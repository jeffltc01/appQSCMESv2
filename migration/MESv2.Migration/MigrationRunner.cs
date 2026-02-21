using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.Models;
using MESv2.Migration.Mappers;
using MESv2.Migration.Readers;

namespace MESv2.Migration;

public class MigrationRunner
{
    private readonly V1Reader _v1;
    private readonly Func<MesDbContext> _dbFactory;
    private readonly MigrationLogger _log;
    private readonly bool _skipTestRows;
    private const int BatchSize = 2000;

    public MigrationRunner(V1Reader v1, Func<MesDbContext> dbFactory, MigrationLogger log, bool skipTestRows = true)
    {
        _v1 = v1;
        _dbFactory = dbFactory;
        _log = log;
        _skipTestRows = skipTestRows;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Starting V1 -> V2 full data migration...");
        var total = Stopwatch.StartNew();

        // Phase 2: Reference data
        await MigratePlantsFirstPassAsync();
        await MigratePlantGearsAsync();
        await MigratePlantsSecondPassAsync();
        await MigrateProductionLinesAsync();
        await MigrateWorkCentersAsync();
        await MigrateAssetsAsync();
        await MigrateTableAsync("mesProductType", ProductTypeMapper.Map);
        await MigrateTableAsync("mesProduct", ProductMapper.Map);
        await MigrateUsersAsync();
        await MigrateTableAsync("mesVendor", VendorMapper.Map);
        await MigrateTableAsync("mesAnnotationType", AnnotationTypeMapper.Map);
        await MigrateTableAsync("mesCharacteristic", CharacteristicMapper.Map);
        await MigrateTableAsync("mesCharacteristicWorkCenter", CharacteristicWorkCenterMapper.Map);
        await MigrateControlPlansAsync();
        await MigrateTableAsync("mesDefectMaster", DefectCodeMapper.Map);
        await MigrateTableAsync("mesDefectLocation", DefectLocationMapper.Map);
        await MigrateTableAsync("mesDefectWorkCenter", DefectWorkCenterMapper.Map);
        await MigrateTableAsync("mesKanbanCards", BarcodeCardMapper.Map);

        // Phase 3: Transactional data
        await MigrateSerialNumbersAsync();
        await MigrateProductionRecordsAsync();
        await MigrateTableAsync("mesManufacturingLogWelder", WelderLogMapper.Map, "IsTest = 0");
        await MigrateTraceabilityLogsAsync();
        await MigrateInspectionRecordsAsync();
        await MigrateDefectLogsAsync();
        await MigrateAnnotationsAsync();
        await MigrateMaterialQueueAsync();
        await MigrateTableAsync("mesChangeLog", ChangeLogMapper.Map);
        await MigrateSpotXrayIncrementsAsync();
        await MigrateSiteSchedulesAsync();

        total.Stop();
        Console.WriteLine($"\nTotal migration time: {total.Elapsed.TotalMinutes:F1} minutes");
        _log.PrintSummary();
    }

    private async Task MigrateTableAsync<TEntity>(
        string v1Table,
        Func<dynamic, TEntity?> mapper,
        string? extraWhere = null) where TEntity : class
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable(v1Table);

        var where = _skipTestRows && await HasIsTestColumn(v1Table)
            ? CombineWhere("IsTest = 0", extraWhere)
            : extraWhere;

        var sourceCount = await _v1.CountAsync(v1Table, where);
        _log.SetSourceCount(sourceCount);

        if (sourceCount == 0) { _log.EndTable(sw.Elapsed); return; }

        var rows = await _v1.ReadTableAsync(v1Table, where);
        var batch = new List<TEntity>(BatchSize);

        foreach (var row in rows)
        {
            try
            {
                var entity = mapper(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);

                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Row {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }

        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task UpsertBatchAsync<TEntity>(List<TEntity> entities) where TEntity : class
    {
        using var db = _dbFactory();
        foreach (var entity in entities)
        {
            var entry = db.Entry(entity);
            var pk = entry.Property("Id").CurrentValue;
            var existing = await db.Set<TEntity>().FindAsync(pk);
            if (existing != null)
            {
                db.Entry(existing).CurrentValues.SetValues(entity);
            }
            else
            {
                db.Set<TEntity>().Add(entity);
            }
        }
        await db.SaveChangesAsync();
        _log.IncrementMigrated(entities.Count);
    }

    private async Task<bool> HasIsTestColumn(string tableName)
    {
        var result = await _v1.ScalarAsync<int>(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t AND COLUMN_NAME = 'IsTest'",
            new { t = tableName });
        return result > 0;
    }

    private static string? CombineWhere(string a, string? b)
        => string.IsNullOrEmpty(b) ? a : $"{a} AND {b}";

    // ---------- Custom migration methods for tables needing special logic ----------

    /// <summary>
    /// First pass: insert Plants without CurrentPlantGearId to break the circular dependency
    /// (Plant needs PlantGear, PlantGear needs Plant).
    /// </summary>
    private async Task MigratePlantsFirstPassAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesSite");
        var rows = await _v1.ReadTableAsync("mesSite");
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        foreach (var row in list)
        {
            try
            {
                var entity = PlantMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                entity.CurrentPlantGearId = null; // set in second pass
                var existing = await db.Plants.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.Plants.Add(entity);
                _log.IncrementMigrated();
            }
            catch (Exception ex)
            {
                _log.Warn($"Site {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    /// <summary>
    /// V1 PlantGears are global. V2 requires PlantId. Duplicate each v1 gear across all plants,
    /// using a deterministic GUID derived from the original gear ID + plant ID.
    /// </summary>
    private async Task MigratePlantGearsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesPlantGears");
        var rows = await _v1.ReadTableAsync("mesPlantGears");
        var gears = rows.ToList();

        using var db = _dbFactory();
        var plants = await db.Plants.ToListAsync();

        _log.SetSourceCount(gears.Count);

        foreach (var gear in gears)
        {
            foreach (var plant in plants)
            {
                try
                {
                    // Deterministic ID: XOR plant ID bytes into the gear ID
                    var deterministicId = CombineGuids((Guid)gear.Id, plant.Id);
                    var entity = PlantGearMapper.Map(gear, plant.Id, deterministicId);
                    var existing = await db.PlantGears.FindAsync(entity.Id);
                    if (existing != null)
                        db.Entry(existing).CurrentValues.SetValues(entity);
                    else
                        db.PlantGears.Add(entity);
                }
                catch (Exception ex)
                {
                    _log.Warn($"PlantGear {gear.Id} for plant {plant.Code}: {ex.Message}");
                    _log.IncrementSkipped();
                }
            }
        }
        await db.SaveChangesAsync();
        _log.IncrementMigrated(gears.Count * plants.Count);
        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    /// <summary>
    /// Second pass: now that PlantGears exist, update Plant.CurrentPlantGearId.
    /// Maps the v1 mesSite.CurrentPlantGearId (global gear) to the plant-specific copy.
    /// </summary>
    private async Task MigratePlantsSecondPassAsync()
    {
        var rows = await _v1.ReadTableAsync("mesSite");
        using var db = _dbFactory();

        foreach (var row in rows)
        {
            Guid? v1GearId = (Guid?)row.CurrentPlantGearId;
            if (v1GearId == null || v1GearId == Guid.Empty) continue;

            var plantId = (Guid)row.Id;
            var mappedGearId = CombineGuids(v1GearId.Value, plantId);
            var plant = await db.Plants.FindAsync(plantId);
            if (plant != null)
            {
                plant.CurrentPlantGearId = mappedGearId;
            }
        }
        await db.SaveChangesAsync();
    }

    private async Task MigrateProductionLinesAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesProductionLine");
        var rows = await _v1.ReadTableAsync("mesProductionLine");
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var plants = await db.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);

        foreach (var row in list)
        {
            try
            {
                var entity = ProductionLineMapper.Map(row, plants);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                var existing = await db.ProductionLines.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.ProductionLines.Add(entity);
                _log.IncrementMigrated();
            }
            catch (Exception ex)
            {
                _log.Warn($"ProductionLine {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateWorkCentersAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesWorkCenter");
        var rows = await _v1.ReadTableAsync("mesWorkCenter");
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var wcTypes = await db.WorkCenterTypes.ToDictionaryAsync(t => t.Name, t => t.Id);
        var plants = await db.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);
        var lines = await db.ProductionLines.ToListAsync();
        int migrated = 0;
        var wcplRecords = new List<WorkCenterProductionLine>();

        foreach (var row in list)
        {
            try
            {
                var result = WorkCenterMapper.Map(row, wcTypes, plants, lines);
                if (result == null) { _log.IncrementSkipped(); continue; }

                var entity = result.WorkCenter;
                var existing = await db.WorkCenters.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.WorkCenters.Add(entity);

                if (result.ProductionLineId.HasValue)
                {
                    wcplRecords.Add(new WorkCenterProductionLine
                    {
                        Id = Guid.NewGuid(),
                        WorkCenterId = entity.Id,
                        ProductionLineId = result.ProductionLineId.Value,
                        DisplayName = entity.Name,
                        NumberOfWelders = entity.NumberOfWelders,
                    });
                }

                migrated++;
            }
            catch (Exception ex)
            {
                _log.Warn($"WorkCenter {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        _log.IncrementMigrated(migrated);

        // Create WorkCenterProductionLine records
        int wcplMigrated = 0;
        foreach (var wcpl in wcplRecords)
        {
            var exists = await db.WorkCenterProductionLines
                .AnyAsync(x => x.WorkCenterId == wcpl.WorkCenterId && x.ProductionLineId == wcpl.ProductionLineId);
            if (!exists)
            {
                db.WorkCenterProductionLines.Add(wcpl);
                wcplMigrated++;
            }
        }
        if (wcplMigrated > 0)
            await db.SaveChangesAsync();
        Console.WriteLine($"  Created {wcplMigrated} WorkCenterProductionLine records.");

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateAssetsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesAsset");
        var rows = await _v1.ReadTableAsync("mesAsset");
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var plants = await db.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);

        // Derive PlantId for each work center via WorkCenterProductionLine junction
        var wcPlantLookup = await db.WorkCenterProductionLines
            .Include(wcpl => wcpl.ProductionLine)
            .GroupBy(wcpl => wcpl.WorkCenterId)
            .Select(g => new { WorkCenterId = g.Key, PlantId = g.First().ProductionLine.PlantId })
            .ToDictionaryAsync(x => x.WorkCenterId, x => x.PlantId);

        var wcNames = await db.WorkCenters.ToDictionaryAsync(w => w.Id, w => w.Name);
        var wcTuples = wcNames.Select(kvp =>
            (kvp.Key, kvp.Value, wcPlantLookup.GetValueOrDefault(kvp.Key, Guid.Empty))
        ).ToList();

        foreach (var row in list)
        {
            try
            {
                var entity = AssetMapper.Map(row, plants, wcTuples, _log);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                var existing = await db.Assets.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.Assets.Add(entity);
                _log.IncrementMigrated();
            }
            catch (Exception ex)
            {
                _log.Warn($"Asset {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateUsersAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesUser");
        var rows = await _v1.ReadTableAsync("mesUser");
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var plants = await db.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);
        var plantIds = await db.Plants.ToDictionaryAsync(p => p.Id, p => p.Code);

        foreach (var row in list)
        {
            try
            {
                var entity = UserMapper.Map(row, plants, plantIds);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                var existing = await db.Users.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.Users.Add(entity);
                _log.IncrementMigrated();
            }
            catch (Exception ex)
            {
                _log.Warn($"User {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateControlPlansAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesControlPlan");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var rows = await _v1.ReadTableAsync("mesControlPlan", where);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        foreach (var row in list)
        {
            try
            {
                var entity = ControlPlanMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                var existing = await db.ControlPlans.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.ControlPlans.Add(entity);
                _log.IncrementMigrated();
            }
            catch (Exception ex)
            {
                _log.Warn($"ControlPlan {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateSerialNumbersAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesSerialNumberMaster");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var count = await _v1.CountAsync("mesSerialNumberMaster", where);
        _log.SetSourceCount(count);

        // Two-pass: first insert with ReplaceBySNId = null, then update self-references
        var rows = await _v1.ReadTableAsync("mesSerialNumberMaster", where);
        var list = rows.ToList();
        var selfRefs = new List<(Guid Id, Guid ReplaceBySNId)>();
        var batch = new List<SerialNumber>(BatchSize);

        foreach (var row in list)
        {
            try
            {
                var entity = SerialNumberMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }

                Guid? replaceId = (Guid?)row.ReplaceBySNId;
                if (replaceId.HasValue && replaceId.Value != Guid.Empty)
                {
                    selfRefs.Add((entity.Id, replaceId.Value));
                    entity.ReplaceBySNId = null;
                }

                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"SerialNumber {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        // Second pass: update self-references
        if (selfRefs.Count > 0)
        {
            using var db = _dbFactory();
            foreach (var (id, replaceId) in selfRefs)
            {
                var sn = await db.SerialNumbers.FindAsync(id);
                if (sn != null)
                {
                    sn.ReplaceBySNId = replaceId;
                }
            }
            await db.SaveChangesAsync();
        }

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateProductionRecordsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesManufacturingLog");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var count = await _v1.CountAsync("mesManufacturingLog", where);
        _log.SetSourceCount(count);

        using var db = _dbFactory();
        var users = await db.Users.ToDictionaryAsync(u => u.EmployeeNumber, u => u.Id);

        var rows = await _v1.ReadTableAsync("mesManufacturingLog", where);
        var batch = new List<ProductionRecord>(BatchSize);

        foreach (var row in rows)
        {
            try
            {
                var entity = ProductionRecordMapper.Map(row, users, _log);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"ManufacturingLog {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateTraceabilityLogsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesManufacturingTraceLog");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var rows = await _v1.ReadTableAsync("mesManufacturingTraceLog", where);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        var batch = new List<TraceabilityLog>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = TraceabilityLogMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"TraceLog {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateInspectionRecordsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesManufacturingInspectionsLog");
        var where = _skipTestRows ? "IsTest = 0" : null;

        // Join with ManufacturingLog to get WorkCenterId and operator
        var sql = $@"
            SELECT i.*, m.WorkCenterId, m.CreatedByUserId AS OperatorUserId
            FROM [dbo].[mesManufacturingInspectionsLog] i
            INNER JOIN [dbo].[mesManufacturingLog] m ON i.ManufacturingLogId = m.Id
            {(where != null ? $"WHERE i.{where}" : "")}";

        var rows = await _v1.QueryAsync(sql);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var snLookup = await db.SerialNumbers.ToDictionaryAsync(s => s.Id, s => s.Serial);

        var batch = new List<InspectionRecord>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = InspectionRecordMapper.Map(row, snLookup);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"InspectionRecord {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateDefectLogsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesDefectLog");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var rows = await _v1.ReadTableAsync("mesDefectLog", where);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var snLookup = await db.SerialNumbers.ToDictionaryAsync(s => s.Id, s => s.Serial);

        var batch = new List<DefectLog>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = DefectLogMapper.Map(row, snLookup);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"DefectLog {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateAnnotationsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesAnnotation");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var rows = await _v1.ReadTableAsync("mesAnnotation", where);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        using var db = _dbFactory();
        var productionRecordIds = (await db.ProductionRecords.Select(p => p.Id).ToListAsync()).ToHashSet();

        var batch = new List<Annotation>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = AnnotationMapper.Map(row, productionRecordIds, _log);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Annotation {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateMaterialQueueAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesWorkCenterMaterialQueue");
        var where = _skipTestRows ? "IsTest = 0" : null;

        var sql = $@"
            SELECT q.*, sn.SerialNumber, sn.ProductId, sn.CoilNumber, sn.HeatNumber, sn.LotNumber,
                   sn.MillVendorId, sn.ProcessorVendorId, sn.HeadsVendorId,
                   p.ProductName AS ProductDescription, p.TankSize
            FROM [dbo].[mesWorkCenterMaterialQueue] q
            LEFT JOIN [dbo].[mesSerialNumberMaster] sn ON q.SerialNumberMasterId = sn.Id
            LEFT JOIN [dbo].[mesProduct] p ON sn.ProductId = p.Id
            {(where != null ? $"WHERE q.{where}" : "")}";

        var rows = await _v1.QueryAsync(sql);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        var batch = new List<MaterialQueueItem>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = MaterialQueueMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"MaterialQueue {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateSpotXrayIncrementsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesSpotXrayIncrement");
        var rows = await _v1.ReadTableAsync("mesSpotXrayIncrement");
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        var batch = new List<SpotXrayIncrement>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = SpotXrayIncrementMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"SpotXrayIncrement {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task MigrateSiteSchedulesAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesSiteSchedule");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var rows = await _v1.ReadTableAsync("mesSiteSchedule", where);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        var batch = new List<SiteSchedule>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = SiteScheduleMapper.Map(row);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    await UpsertBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"SiteSchedule {row.Id}: {ex.Message}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
            await UpsertBatchAsync(batch);

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    /// <summary>
    /// Produces a deterministic GUID by XOR-ing two GUIDs' byte arrays.
    /// Used to create plant-specific copies of global v1 records.
    /// </summary>
    private static Guid CombineGuids(Guid a, Guid b)
    {
        var bytesA = a.ToByteArray();
        var bytesB = b.ToByteArray();
        var result = new byte[16];
        for (int i = 0; i < 16; i++)
            result[i] = (byte)(bytesA[i] ^ bytesB[i]);
        return new Guid(result);
    }
}
