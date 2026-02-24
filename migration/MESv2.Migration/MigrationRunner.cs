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
    private readonly HashSet<string> _onlyTables;
    private const int BatchSize = 2000;
    private HashSet<Guid> _skipPlantIds = new();
    private HashSet<string> _skipPlantCodes = new(StringComparer.OrdinalIgnoreCase) { "600" };

    public MigrationRunner(
        V1Reader v1,
        Func<MesDbContext> dbFactory,
        MigrationLogger log,
        bool skipTestRows = true,
        IEnumerable<string>? onlyTables = null)
    {
        _v1 = v1;
        _dbFactory = dbFactory;
        _log = log;
        _skipTestRows = skipTestRows;
        _onlyTables = onlyTables != null
            ? new HashSet<string>(onlyTables.Where(t => !string.IsNullOrWhiteSpace(t)), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task BuildPlantFiltersAsync()
    {
        using var db = _dbFactory();
        var plant600 = await db.Plants.FirstOrDefaultAsync(p => p.Code == "600");
        if (plant600 != null)
        {
            _skipPlantIds.Add(plant600.Id);
            Console.WriteLine($"  Skipping Plant 600 (Id: {plant600.Id})");
        }

        Console.WriteLine($"  Skipping Plant 600 data for SerialNumbers.");
    }


    public async Task RunAsync()
    {
        if (_onlyTables.Count > 0)
        {
            await RunSelectedTablesAsync(_onlyTables);
            return;
        }

        Console.WriteLine("Starting V1 -> V2 full data migration...");
        var total = Stopwatch.StartNew();

        await BuildPlantFiltersAsync();

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

    private async Task RunSelectedTablesAsync(IEnumerable<string> tables)
    {
        var list = tables.ToList();
        Console.WriteLine($"Starting targeted migration for table(s): {string.Join(", ", list)}");
        var total = Stopwatch.StartNew();

        await BuildPlantFiltersAsync();

        foreach (var table in list)
        {
            switch (table)
            {
                case "mesSerialNumberMaster":
                    await MigrateSerialNumbersAsync();
                    break;
                case "mesManufacturingLog":
                    await MigrateProductionRecordsAsync();
                    break;
                case "mesManufacturingLogWelder":
                    await MigrateTableAsync("mesManufacturingLogWelder", WelderLogMapper.Map, "IsTest = 0");
                    break;
                case "mesManufacturingTraceLog":
                    await MigrateTraceabilityLogsAsync();
                    break;
                case "mesManufacturingInspectionsLog":
                    await MigrateInspectionRecordsAsync();
                    break;
                case "mesDefectLog":
                    await MigrateDefectLogsAsync();
                    break;
                case "mesAnnotation":
                    await MigrateAnnotationsAsync();
                    break;
                case "mesWorkCenterMaterialQueue":
                    await MigrateMaterialQueueAsync();
                    break;
                case "mesChangeLog":
                    await MigrateTableAsync("mesChangeLog", ChangeLogMapper.Map);
                    break;
                case "mesSpotXrayIncrement":
                    await MigrateSpotXrayIncrementsAsync();
                    break;
                case "mesSiteSchedule":
                    await MigrateSiteSchedulesAsync();
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported table '{table}' for targeted mode.");
            }
        }

        total.Stop();
        Console.WriteLine($"\nTargeted migration time: {total.Elapsed.TotalMinutes:F1} minutes");
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
                    try
                    {
                        await UpsertBatchAsync(batch);
                    }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} {v1Table} failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try
            {
                await UpsertBatchAsync(batch);
            }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} {v1Table} failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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
            var d = (IDictionary<string, object>)row;
            var gearRaw = d.TryGetValue("CurrentPlantGearId", out var gv) ? gv : null;
            if (gearRaw is not Guid v1GearId || v1GearId == Guid.Empty) continue;

            var idRaw = d.TryGetValue("Id", out var iv) ? iv : null;
            if (idRaw is not Guid plantId) continue;
            var mappedGearId = CombineGuids(v1GearId, plantId);
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
        var allLines = await db.ProductionLines.ToListAsync();
        int migrated = 0;
        var migratedWcIds = new List<Guid>();

        foreach (var row in list)
        {
            try
            {
                var result = WorkCenterMapper.Map((object)row, wcTypes);
                if (result == null) { _log.IncrementSkipped(); continue; }

                var entity = result.WorkCenter;
                var existing = await db.WorkCenters.FindAsync(entity.Id);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.WorkCenters.Add(entity);

                migratedWcIds.Add(entity.Id);
                migrated++;
            }
            catch (Exception ex)
            {
                var d = (IDictionary<string, object>)row;
                var id = d.TryGetValue("Id", out var idv) ? idv : "??";
                _log.Warn($"WorkCenter {id}: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    _log.Warn($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                _log.IncrementSkipped();
            }
        }
        await db.SaveChangesAsync();
        _log.IncrementMigrated(migrated);

        // All WorkCenters are global: create a WCPL record for every WC x every ProductionLine
        int wcplMigrated = 0;
        foreach (var wcId in migratedWcIds)
        {
            var wcEntity = await db.WorkCenters.FindAsync(wcId);
            foreach (var line in allLines)
            {
                var exists = await db.WorkCenterProductionLines
                    .AnyAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == line.Id);
                if (!exists)
                {
                    db.WorkCenterProductionLines.Add(new WorkCenterProductionLine
                    {
                        Id = Guid.NewGuid(),
                        WorkCenterId = wcId,
                        ProductionLineId = line.Id,
                        DisplayName = wcEntity?.Name ?? "",
                        NumberOfWelders = wcEntity?.NumberOfWelders ?? 0,
                    });
                    wcplMigrated++;
                }
            }
        }
        if (wcplMigrated > 0)
            await db.SaveChangesAsync();
        Console.WriteLine($"  Created {wcplMigrated} WorkCenterProductionLine records (all WCs x all Lines).");

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

        var allWcs = await db.WorkCenters
            .Select(w => ValueTuple.Create(w.Id, w.Name))
            .ToListAsync();

        // Plant -> first ProductionLine for that plant
        var plantToLine = await db.ProductionLines
            .GroupBy(l => l.PlantId)
            .Select(g => new { PlantId = g.Key, LineId = g.First().Id })
            .ToDictionaryAsync(x => x.PlantId, x => x.LineId);

        foreach (var row in list)
        {
            try
            {
                var entity = AssetMapper.Map((object)row, plants, allWcs, plantToLine, _log);
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
        var wcplLookup = await db.WorkCenterProductionLines
            .GroupBy(wcpl => wcpl.WorkCenterId)
            .Select(g => new { WorkCenterId = g.Key, WcplId = g.First().Id })
            .ToDictionaryAsync(x => x.WorkCenterId, x => x.WcplId);

        foreach (var row in list)
        {
            try
            {
                Guid collectionWcId = (Guid)row.CollectionWorkCenterId;
                if (!wcplLookup.TryGetValue(collectionWcId, out var wcplId))
                {
                    _log.Warn($"ControlPlan {row.Id}: No WorkCenterProductionLine found for WorkCenter {collectionWcId}");
                    _log.IncrementSkipped();
                    continue;
                }

                var entity = ControlPlanMapper.Map(row, wcplId);
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

        using var lookupDb = _dbFactory();
        var plantsByCode = await lookupDb.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);

        var rows = await _v1.ReadTableAsync("mesSerialNumberMaster", where);
        var list = rows.ToList();
        var selfRefs = new List<(Guid Id, Guid ReplaceBySNId)>();
        var batch = new List<SerialNumber>(BatchSize);

        foreach (var row in list)
        {
            try
            {
                // Skip serial numbers from plant 600
                var dSn = (IDictionary<string, object>)row;
                var snSc = dSn.TryGetValue("SiteCode", out var snScv) ? snScv : null;
                bool skipSn = false;
                if (snSc is Guid snScGuid && _skipPlantIds.Contains(snScGuid)) skipSn = true;
                else if (snSc is string snScStr && _skipPlantCodes.Contains(snScStr.Trim())) skipSn = true;
                if (skipSn)
                {
                    _log.IncrementSkipped();
                    continue;
                }

                var entity = SerialNumberMapper.Map(row, plantsByCode);
                if (entity == null) { _log.IncrementSkipped(); continue; }

                var d = dSn;
                var replaceRaw = d.TryGetValue("ReplaceBySNId", out var rv) ? rv : null;
                if (replaceRaw is Guid rGuid && rGuid != Guid.Empty)
                {
                    selfRefs.Add((entity.Id, rGuid));
                    entity.ReplaceBySNId = null;
                }

                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} SerialNumbers failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} SerialNumbers failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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

        // Pre-load valid FK targets to avoid FK violations in batches
        using var lookupDb = _dbFactory();
        var validSnIds = (await lookupDb.SerialNumbers.Select(s => s.Id).ToListAsync()).ToHashSet();
        var validWcIds = (await lookupDb.WorkCenters.Select(w => w.Id).ToListAsync()).ToHashSet();
        var validLineIds = (await lookupDb.ProductionLines.Select(l => l.Id).ToListAsync()).ToHashSet();
        var validUserIds = (await lookupDb.Users.Select(u => u.Id).ToListAsync()).ToHashSet();
        Console.WriteLine($"  FK validation: {validSnIds.Count} SNs, {validWcIds.Count} WCs, {validLineIds.Count} Lines, {validUserIds.Count} Users");

        var rows = await _v1.ReadTableAsync("mesManufacturingLog", where);
        var batch = new List<ProductionRecord>(BatchSize);
        int skippedFk = 0;

        foreach (var row in rows)
        {
            try
            {
                var entity = ProductionRecordMapper.Map((object)row, _log);
                if (entity == null) { _log.IncrementSkipped(); continue; }

                if (!validSnIds.Contains(entity.SerialNumberId) ||
                    !validWcIds.Contains(entity.WorkCenterId) ||
                    !validLineIds.Contains(entity.ProductionLineId))
                {
                    skippedFk++;
                    _log.IncrementSkipped();
                    continue;
                }
                if (!validUserIds.Contains(entity.OperatorId))
                    entity.OperatorId = validUserIds.First();

                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try
                    {
                        await UpsertBatchAsync(batch);
                    }
                    catch
                    {
                        _log.Warn($"Batch failed, retrying row-by-row ({batch.Count} records)...");
                        await UpsertRowByRowAsync(batch);
                    }
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                var d2 = (IDictionary<string, object>)row;
                var rid = d2.TryGetValue("Id", out var rv) ? rv : "??";
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                _log.Warn($"ManufacturingLog {rid}: {innerMsg}");
                _log.IncrementSkipped();
            }
        }
        if (batch.Count > 0)
        {
            try
            {
                await UpsertBatchAsync(batch);
            }
            catch
            {
                _log.Warn($"Final batch failed, retrying row-by-row ({batch.Count} records)...");
                await UpsertRowByRowAsync(batch);
            }
            batch.Clear();
        }

        if (skippedFk > 0)
            Console.WriteLine($"  Skipped {skippedFk} records with missing FK references (likely plant 600 data).");

        sw.Stop();
        _log.EndTable(sw.Elapsed);
    }

    private async Task UpsertRowByRowAsync<TEntity>(List<TEntity> entities) where TEntity : class
    {
        int ok = 0, fail = 0;
        foreach (var entity in entities)
        {
            try
            {
                using var db = _dbFactory();
                var entry = db.Entry(entity);
                var pk = entry.Property("Id").CurrentValue;
                var existing = await db.Set<TEntity>().FindAsync(pk);
                if (existing != null)
                    db.Entry(existing).CurrentValues.SetValues(entity);
                else
                    db.Set<TEntity>().Add(entity);
                await db.SaveChangesAsync();
                ok++;
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                _log.Warn($"Row-by-row fail: {innerMsg}");
                fail++;
            }
        }
        _log.IncrementMigrated(ok);
        _log.IncrementSkipped(fail);
        if (fail > 0)
            Console.WriteLine($"    Row-by-row: {ok} OK, {fail} failed.");
    }

    private async Task MigrateTraceabilityLogsAsync()
    {
        var sw = Stopwatch.StartNew();
        _log.BeginTable("mesManufacturingTraceLog");
        var where = _skipTestRows ? "IsTest = 0" : null;
        var rows = await _v1.ReadTableAsync("mesManufacturingTraceLog", where);
        var list = rows.ToList();
        _log.SetSourceCount(list.Count);

        // Pre-load FK targets so bad rows are handled deterministically instead of failing whole batches.
        using var lookupDb = _dbFactory();
        var validSnIds = (await lookupDb.SerialNumbers.Select(s => s.Id).ToListAsync()).ToHashSet();
        var validPrIds = (await lookupDb.ProductionRecords.Select(p => p.Id).ToListAsync()).ToHashSet();

        var batch = new List<TraceabilityLog>(BatchSize);
        int skippedFk = 0;
        int nulledProductionRecordFk = 0;
        foreach (var row in list)
        {
            try
            {
                var entity = TraceabilityLogMapper.Map((object)row);
                if (entity == null) { _log.IncrementSkipped(); continue; }

                if (entity.FromSerialNumberId is Guid fromSnId && !validSnIds.Contains(fromSnId))
                {
                    skippedFk++;
                    _log.IncrementSkipped();
                    continue;
                }

                if (entity.ToSerialNumberId is Guid toSnId && !validSnIds.Contains(toSnId))
                {
                    skippedFk++;
                    _log.IncrementSkipped();
                    continue;
                }

                // ProductionRecordId is optional in v2. Preserve trace links when the record no longer exists.
                if (entity.ProductionRecordId is Guid productionRecordId && !validPrIds.Contains(productionRecordId))
                {
                    entity.ProductionRecordId = null;
                    nulledProductionRecordFk++;
                }

                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} TraceabilityLogs failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} TraceabilityLogs failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

        if (skippedFk > 0)
            Console.WriteLine($"  Skipped {skippedFk} trace rows with missing SerialNumber FK(s).");
        if (nulledProductionRecordFk > 0)
            Console.WriteLine($"  Cleared ProductionRecordId on {nulledProductionRecordFk} trace rows with missing ProductionRecord FK.");

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
                var entity = InspectionRecordMapper.Map((object)row, snLookup);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} InspectionRecords failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} InspectionRecords failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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
                var entity = DefectLogMapper.Map((object)row, snLookup);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} DefectLogs failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} DefectLogs failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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
                var entity = AnnotationMapper.Map((object)row, productionRecordIds, _log);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} Annotations failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} Annotations failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} MaterialQueueItems failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} MaterialQueueItems failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} SpotXrayIncrements failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} SpotXrayIncrements failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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

        using var lookupDb = _dbFactory();
        var plantsByCode = await lookupDb.Plants.ToDictionaryAsync(p => p.Code, p => p.Id);

        var batch = new List<SiteSchedule>(BatchSize);
        foreach (var row in list)
        {
            try
            {
                var entity = SiteScheduleMapper.Map(row, plantsByCode);
                if (entity == null) { _log.IncrementSkipped(); continue; }
                batch.Add(entity);
                if (batch.Count >= BatchSize)
                {
                    try { await UpsertBatchAsync(batch); }
                    catch
                    {
                        _log.Warn($"Batch of {batch.Count} SiteSchedules failed, retrying row-by-row...");
                        await UpsertRowByRowAsync(batch);
                    }
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
        {
            try { await UpsertBatchAsync(batch); }
            catch
            {
                _log.Warn($"Final batch of {batch.Count} SiteSchedules failed, retrying row-by-row...");
                await UpsertRowByRowAsync(batch);
            }
        }

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
