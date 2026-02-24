using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class DemoDataAdminService : IDemoDataAdminService
{
    private readonly MesDbContext _db;
    private readonly DemoDataAdminOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DemoDataAdminService> _logger;

    public DemoDataAdminService(
        MesDbContext db,
        IOptions<DemoDataAdminOptions> options,
        IHostEnvironment environment,
        ILogger<DemoDataAdminService> logger)
    {
        _db = db;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<DemoDataResetSeedResultDto> ResetAndSeedAsync(CancellationToken ct = default)
    {
        EnsureDemoOperationsAllowed();

        var deleted = new List<DemoDataTableCountDto>();
        var inserted = new List<DemoDataTableCountDto>();
        var useTransaction = _db.Database.IsRelational();
        using var tx = useTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            await DeleteAllAsync<ChecklistEntryItemResponse>("ChecklistEntryItemResponses", deleted, ct);
            await DeleteAllAsync<ChecklistEntry>("ChecklistEntries", deleted, ct);
            await DeleteAllAsync<ChecklistTemplateItem>("ChecklistTemplateItems", deleted, ct);
            await DeleteAllAsync<ChecklistTemplate>("ChecklistTemplates", deleted, ct);
            await DeleteAllAsync<AuditLog>("AuditLogs", deleted, ct);
            await DeleteAllAsync<FrontendTelemetryEvent>("FrontendTelemetryEvents", deleted, ct);
            await DeleteAllAsync<ChangeLog>("ChangeLogs", deleted, ct);
            await DeleteAllAsync<Annotation>("Annotations", deleted, ct);
            await DeleteAllAsync<DefectLog>("DefectLogs", deleted, ct);
            await DeleteAllAsync<InspectionRecord>("InspectionRecords", deleted, ct);
            await DeleteAllAsync<WelderLog>("WelderLogs", deleted, ct);
            await DeleteAllAsync<TraceabilityLog>("TraceabilityLogs", deleted, ct);
            // Spot X-ray increments can reference production logs via ManufacturingLogId.
            await DeleteAllAsync<SpotXrayIncrementTank>("SpotXrayIncrementTanks", deleted, ct);
            await DeleteAllAsync<SpotXrayIncrement>("SpotXrayIncrements", deleted, ct);
            await DeleteAllAsync<ProductionRecord>("ProductionRecords", deleted, ct);
            await DeleteAllAsync<PrintLog>("PrintLogs", deleted, ct);
            await DeleteAllAsync<IssueRequest>("IssueRequests", deleted, ct);
            await DeleteAllAsync<DowntimeEvent>("DowntimeEvents", deleted, ct);
            await DeleteAllAsync<WorkCenterProductionLineDowntimeReason>("WorkCenterProductionLineDowntimeReasons", deleted, ct);
            await DeleteAllAsync<QueueTransaction>("QueueTransactions", deleted, ct);
            await DeleteAllAsync<MaterialQueueItem>("MaterialQueueItems", deleted, ct);
            await DeleteAllAsync<XrayQueueItem>("XrayQueueItems", deleted, ct);
            await DeleteAllAsync<RoundSeamSetup>("RoundSeamSetups", deleted, ct);
            await DeleteAllAsync<DemoShellFlow>("DemoShellFlows", deleted, ct);
            await DeleteAllAsync<ActiveSession>("ActiveSessions", deleted, ct);
            await DeleteAllAsync<SiteSchedule>("SiteSchedules", deleted, ct);
            await DeleteAllAsync<ShiftSchedule>("ShiftSchedules", deleted, ct);
            await DeleteAllAsync<WorkCenterCapacityTarget>("WorkCenterCapacityTargets", deleted, ct);
            await DeleteAllAsync<XrayShotCounter>("XrayShotCounters", deleted, ct);
            await DeleteAllAsync<PlantPrinter>("PlantPrinters", deleted, ct);
            await DeleteAllAsync<Asset>("Assets", deleted, ct);
            await DeleteAllAsync<ControlPlan>("ControlPlans", deleted, ct);
            await DeleteAllAsync<DefectWorkCenter>("DefectWorkCenters", deleted, ct);
            await DeleteAllAsync<CharacteristicWorkCenter>("CharacteristicWorkCenters", deleted, ct);
            await DeleteAllAsync<DefectLocation>("DefectLocations", deleted, ct);
            await DeleteAllAsync<DefectCode>("DefectCodes", deleted, ct);
            await DeleteAllAsync<Characteristic>("Characteristics", deleted, ct);
            await DeleteAllAsync<BarcodeCard>("BarcodeCards", deleted, ct);
            await DeleteAllAsync<VendorPlant>("VendorPlants", deleted, ct);
            await DeleteAllAsync<ProductPlant>("ProductPlants", deleted, ct);
            await DeleteAllAsync<SerialNumber>("SerialNumbers", deleted, ct);
            await DeleteAllAsync<Vendor>("Vendors", deleted, ct);
            await DeleteAllAsync<Product>("Products", deleted, ct);
            await DeleteAllAsync<User>("Users", deleted, ct);
            await DeleteAllAsync<DowntimeReason>("DowntimeReasons", deleted, ct);
            await DeleteAllAsync<DowntimeReasonCategory>("DowntimeReasonCategories", deleted, ct);
            // Break Plant -> PlantGear FK.
            // Use bulk update for relational providers; fallback for InMemory test provider.
            if (_db.Database.IsRelational())
            {
                await _db.Plants.ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.CurrentPlantGearId, (Guid?)null),
                    ct);
            }
            else
            {
                var plants = await _db.Plants.ToListAsync(ct);
                foreach (var plant in plants)
                    plant.CurrentPlantGearId = null;
                await _db.SaveChangesAsync(ct);
            }
            await DeleteAllAsync<PlantGear>("PlantGears", deleted, ct);
            await DeleteAllAsync<WorkCenterProductionLine>("WorkCenterProductionLines", deleted, ct);
            await DeleteAllAsync<WorkCenter>("WorkCenters", deleted, ct);
            await DeleteAllAsync<ProductionLine>("ProductionLines", deleted, ct);
            await DeleteAllAsync<Plant>("Plants", deleted, ct);
            await DeleteAllAsync<ProductType>("ProductTypes", deleted, ct);
            await DeleteAllAsync<WorkCenterType>("WorkCenterTypes", deleted, ct);
            await DeleteAllAsync<AnnotationType>("AnnotationTypes", deleted, ct);

            DbInitializer.Seed(_db);
            DbInitializer.SyncJoinTables(_db);
            DbInitializer.EnsureAssembledProducts(_db);
            DbInitializer.BackfillInspectionProductionRecords(_db);
            await SeedDeterministicDemoOperationsAsync(ct);

            inserted.Add(new DemoDataTableCountDto { Table = "Plants", Count = await _db.Plants.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "ProductionLines", Count = await _db.ProductionLines.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "WorkCenters", Count = await _db.WorkCenters.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "Users", Count = await _db.Users.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "Products", Count = await _db.Products.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "SerialNumbers", Count = await _db.SerialNumbers.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "ProductionRecords", Count = await _db.ProductionRecords.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "InspectionRecords", Count = await _db.InspectionRecords.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "DefectLogs", Count = await _db.DefectLogs.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "DowntimeEvents", Count = await _db.DowntimeEvents.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "ShiftSchedules", Count = await _db.ShiftSchedules.CountAsync(ct) });
            inserted.Add(new DemoDataTableCountDto { Table = "WorkCenterCapacityTargets", Count = await _db.WorkCenterCapacityTargets.CountAsync(ct) });

            if (tx is not null)
                await tx.CommitAsync(ct);

            return new DemoDataResetSeedResultDto
            {
                ExecutedAtUtc = DateTime.UtcNow,
                Deleted = deleted,
                Inserted = inserted,
            };
        }
        catch
        {
            if (tx is not null)
                await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<DemoDataRefreshDatesResultDto> RefreshDatesAsync(CancellationToken ct = default)
    {
        EnsureDemoOperationsAllowed();

        var latestProduction = await _db.ProductionRecords
            .Select(r => (DateTime?)r.Timestamp)
            .MaxAsync(ct);

        if (!latestProduction.HasValue)
            throw new InvalidOperationException("No production records found. Run reset + seed first.");

        var delta = DateTime.UtcNow - latestProduction.Value;
        if (Math.Abs(delta.TotalSeconds) < 1)
        {
            return new DemoDataRefreshDatesResultDto
            {
                ExecutedAtUtc = DateTime.UtcNow,
                AppliedDeltaHours = 0,
            };
        }

        var updated = new List<DemoDataTableCountDto>();
        var useTransaction = _db.Database.IsRelational();
        using var tx = useTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            var productionRecords = await _db.ProductionRecords.ToListAsync(ct);
            foreach (var row in productionRecords) row.Timestamp = row.Timestamp.Add(delta);
            AddCount("ProductionRecords", productionRecords.Count, updated);

            var inspectionRecords = await _db.InspectionRecords.ToListAsync(ct);
            foreach (var row in inspectionRecords) row.Timestamp = row.Timestamp.Add(delta);
            AddCount("InspectionRecords", inspectionRecords.Count, updated);

            var defectLogs = await _db.DefectLogs.ToListAsync(ct);
            foreach (var row in defectLogs)
            {
                row.Timestamp = row.Timestamp.Add(delta);
                row.CreatedAt = row.CreatedAt.Add(delta);
                if (row.RepairedDateTime.HasValue) row.RepairedDateTime = row.RepairedDateTime.Value.Add(delta);
            }
            AddCount("DefectLogs", defectLogs.Count, updated);

            var annotations = await _db.Annotations.ToListAsync(ct);
            foreach (var row in annotations) row.CreatedAt = row.CreatedAt.Add(delta);
            AddCount("Annotations", annotations.Count, updated);

            var downtimeEvents = await _db.DowntimeEvents.ToListAsync(ct);
            foreach (var row in downtimeEvents)
            {
                row.StartedAt = row.StartedAt.Add(delta);
                row.EndedAt = row.EndedAt.Add(delta);
                row.CreatedAt = row.CreatedAt.Add(delta);
            }
            AddCount("DowntimeEvents", downtimeEvents.Count, updated);

            var activeSessions = await _db.ActiveSessions.ToListAsync(ct);
            foreach (var row in activeSessions)
            {
                row.LoginDateTime = row.LoginDateTime.Add(delta);
                row.LastHeartbeatDateTime = row.LastHeartbeatDateTime.Add(delta);
            }
            AddCount("ActiveSessions", activeSessions.Count, updated);

            var shiftSchedules = await _db.ShiftSchedules.ToListAsync(ct);
            foreach (var row in shiftSchedules)
            {
                row.EffectiveDate = row.EffectiveDate.AddDays(delta.Days);
                row.CreatedAt = row.CreatedAt.Add(delta);
            }
            AddCount("ShiftSchedules", shiftSchedules.Count, updated);

            var serialNumbers = await _db.SerialNumbers.ToListAsync(ct);
            foreach (var row in serialNumbers)
            {
                row.CreatedAt = row.CreatedAt.Add(delta);
                if (row.ModifiedDateTime.HasValue) row.ModifiedDateTime = row.ModifiedDateTime.Value.Add(delta);
            }
            AddCount("SerialNumbers", serialNumbers.Count, updated);

            var traceabilityLogs = await _db.TraceabilityLogs.ToListAsync(ct);
            foreach (var row in traceabilityLogs) row.Timestamp = row.Timestamp.Add(delta);
            AddCount("TraceabilityLogs", traceabilityLogs.Count, updated);

            var queueItems = await _db.MaterialQueueItems.ToListAsync(ct);
            foreach (var row in queueItems) row.CreatedAt = row.CreatedAt.Add(delta);
            AddCount("MaterialQueueItems", queueItems.Count, updated);

            var queueTransactions = await _db.QueueTransactions.ToListAsync(ct);
            foreach (var row in queueTransactions) row.Timestamp = row.Timestamp.Add(delta);
            AddCount("QueueTransactions", queueTransactions.Count, updated);

            var issues = await _db.IssueRequests.ToListAsync(ct);
            foreach (var row in issues)
            {
                row.SubmittedAt = row.SubmittedAt.Add(delta);
                if (row.ReviewedAt.HasValue) row.ReviewedAt = row.ReviewedAt.Value.Add(delta);
            }
            AddCount("IssueRequests", issues.Count, updated);

            var printLogs = await _db.PrintLogs.ToListAsync(ct);
            foreach (var row in printLogs) row.RequestedAt = row.RequestedAt.Add(delta);
            AddCount("PrintLogs", printLogs.Count, updated);

            var changeLogs = await _db.ChangeLogs.ToListAsync(ct);
            foreach (var row in changeLogs) row.ChangeDateTime = row.ChangeDateTime.Add(delta);
            AddCount("ChangeLogs", changeLogs.Count, updated);

            var telemetryEvents = await _db.FrontendTelemetryEvents.ToListAsync(ct);
            foreach (var row in telemetryEvents)
            {
                row.OccurredAtUtc = row.OccurredAtUtc.Add(delta);
                row.ReceivedAtUtc = row.ReceivedAtUtc.Add(delta);
            }
            AddCount("FrontendTelemetryEvents", telemetryEvents.Count, updated);

            var auditLogs = await _db.AuditLogs.ToListAsync(ct);
            foreach (var row in auditLogs) row.ChangedAtUtc = row.ChangedAtUtc.Add(delta);
            AddCount("AuditLogs", auditLogs.Count, updated);

            var demoShellFlows = await _db.DemoShellFlows.ToListAsync(ct);
            foreach (var row in demoShellFlows)
            {
                row.CreatedAtUtc = row.CreatedAtUtc.Add(delta);
                row.StageEnteredAtUtc = row.StageEnteredAtUtc.Add(delta);
                if (row.CompletedAtUtc.HasValue) row.CompletedAtUtc = row.CompletedAtUtc.Value.Add(delta);
            }
            AddCount("DemoShellFlows", demoShellFlows.Count, updated);

            var checklistTemplates = await _db.ChecklistTemplates.ToListAsync(ct);
            foreach (var row in checklistTemplates)
            {
                row.EffectiveFromUtc = row.EffectiveFromUtc.Add(delta);
                if (row.EffectiveToUtc.HasValue) row.EffectiveToUtc = row.EffectiveToUtc.Value.Add(delta);
                row.CreatedAtUtc = row.CreatedAtUtc.Add(delta);
            }
            AddCount("ChecklistTemplates", checklistTemplates.Count, updated);

            var checklistEntries = await _db.ChecklistEntries.ToListAsync(ct);
            foreach (var row in checklistEntries)
            {
                row.StartedAtUtc = row.StartedAtUtc.Add(delta);
                if (row.CompletedAtUtc.HasValue) row.CompletedAtUtc = row.CompletedAtUtc.Value.Add(delta);
            }
            AddCount("ChecklistEntries", checklistEntries.Count, updated);

            var checklistResponses = await _db.ChecklistEntryItemResponses.ToListAsync(ct);
            foreach (var row in checklistResponses) row.RespondedAtUtc = row.RespondedAtUtc.Add(delta);
            AddCount("ChecklistEntryItemResponses", checklistResponses.Count, updated);

            await _db.SaveChangesAsync(ct);
            if (tx is not null)
                await tx.CommitAsync(ct);

            return new DemoDataRefreshDatesResultDto
            {
                ExecutedAtUtc = DateTime.UtcNow,
                AppliedDeltaHours = Math.Round(delta.TotalHours, 2),
                Updated = updated,
            };
        }
        catch
        {
            if (tx is not null)
                await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task SeedDeterministicDemoOperationsAsync(CancellationToken ct)
    {
        var baseUtc = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        var plant = await _db.Plants.FirstAsync(p => p.Code == "000", ct);
        var line = await _db.ProductionLines.FirstAsync(pl => pl.PlantId == plant.Id && pl.Name == "Line 1", ct);
        var admin = await _db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001", ct);
        var op = await _db.Users.FirstAsync(u => u.EmployeeNumber == "EMP002", ct);
        var welder = await _db.Users.FirstAsync(u => u.EmployeeNumber == "EMP003", ct);

        var workCenters = await _db.WorkCenters
            .Where(w => w.DataEntryType != null)
            .ToDictionaryAsync(w => w.DataEntryType!, w => w, ct);

        var wcplByWcId = await _db.WorkCenterProductionLines
            .Where(x => x.ProductionLineId == line.Id)
            .ToDictionaryAsync(x => x.WorkCenterId, x => x, ct);

        var shellProduct = await _db.Products
            .Include(p => p.ProductType)
            .FirstAsync(p => p.ProductType.SystemTypeName == "shell" && p.TankSize == 120, ct);
        var assembledProduct = await _db.Products
            .Include(p => p.ProductType)
            .FirstAsync(p => p.ProductType.SystemTypeName == "assembled" && p.TankSize == 120, ct);
        var sellableProduct = await _db.Products
            .Include(p => p.ProductType)
            .FirstAsync(p => p.ProductType.SystemTypeName == "sellable" && p.TankSize == 120, ct);
        var plateProduct = await _db.Products
            .Include(p => p.ProductType)
            .FirstAsync(p => p.ProductType.SystemTypeName == "plate" && p.TankSize == 120, ct);

        var longSeamChar = await _db.Characteristics.FirstAsync(c => c.Name == "Long Seam", ct);
        var rs1Char = await _db.Characteristics.FirstAsync(c => c.Name == "RS1", ct);
        var defectCode = await _db.DefectCodes.FirstAsync(dc => dc.Code == "101", ct);
        var defectLocation = await _db.DefectLocations.FirstAsync(dl => dl.Code == "1", ct);
        var annotationTypeDefect = await _db.AnnotationTypes.FirstAsync(a => a.Name == "Defect", ct);
        var annotationTypeCorrection = await _db.AnnotationTypes.FirstAsync(a => a.Name == "Correction Needed", ct);
        var plantGear = await _db.PlantGears.FirstAsync(pg => pg.PlantId == plant.Id && pg.Level == 2, ct);

        var longSeamInspWcpl = wcplByWcId[workCenters["Barcode-LongSeamInsp"].Id];
        var roundSeamInspWcpl = wcplByWcId[workCenters["Barcode-RoundSeamInsp"].Id];
        var hydroWcpl = wcplByWcId[workCenters["Hydro"].Id];

        var controlPlans = new[]
        {
            new ControlPlan
            {
                Id = Guid.Parse("7b430001-0000-0000-0000-000000000001"),
                CharacteristicId = longSeamChar.Id,
                WorkCenterProductionLineId = longSeamInspWcpl.Id,
                IsEnabled = true,
                ResultType = "PassFail",
                IsGateCheck = false,
                CodeRequired = true,
            },
            new ControlPlan
            {
                Id = Guid.Parse("7b430001-0000-0000-0000-000000000002"),
                CharacteristicId = rs1Char.Id,
                WorkCenterProductionLineId = roundSeamInspWcpl.Id,
                IsEnabled = true,
                ResultType = "PassFail",
                IsGateCheck = true,
                CodeRequired = true,
            },
            new ControlPlan
            {
                Id = Guid.Parse("7b430001-0000-0000-0000-000000000003"),
                CharacteristicId = rs1Char.Id,
                WorkCenterProductionLineId = hydroWcpl.Id,
                IsEnabled = true,
                ResultType = "PassFail",
                IsGateCheck = true,
                CodeRequired = false,
            },
        };
        _db.ControlPlans.AddRange(controlPlans);

        var category = new DowntimeReasonCategory
        {
            Id = Guid.Parse("8c440001-0000-0000-0000-000000000001"),
            PlantId = plant.Id,
            Name = "Equipment",
            SortOrder = 1,
            IsActive = true,
        };
        var reason = new DowntimeReason
        {
            Id = Guid.Parse("8c440001-0000-0000-0000-000000000002"),
            DowntimeReasonCategoryId = category.Id,
            Name = "Welder Setup Delay",
            IsActive = true,
            CountsAsDowntime = true,
            SortOrder = 1,
        };
        _db.DowntimeReasonCategories.Add(category);
        _db.DowntimeReasons.Add(reason);
        _db.WorkCenterProductionLineDowntimeReasons.Add(new WorkCenterProductionLineDowntimeReason
        {
            Id = Guid.Parse("8c440001-0000-0000-0000-000000000003"),
            WorkCenterProductionLineId = wcplByWcId[workCenters["Barcode-RoundSeam"].Id].Id,
            DowntimeReasonId = reason.Id,
        });

        _db.ShiftSchedules.Add(new ShiftSchedule
        {
            Id = Guid.Parse("9d550001-0000-0000-0000-000000000001"),
            PlantId = plant.Id,
            EffectiveDate = DateOnly.FromDateTime(baseUtc),
            MondayHours = 9, MondayBreakMinutes = 30,
            TuesdayHours = 9, TuesdayBreakMinutes = 30,
            WednesdayHours = 9, WednesdayBreakMinutes = 30,
            ThursdayHours = 9, ThursdayBreakMinutes = 30,
            FridayHours = 8, FridayBreakMinutes = 30,
            SaturdayHours = 5, SaturdayBreakMinutes = 15,
            SundayHours = 0, SundayBreakMinutes = 0,
            CreatedAt = baseUtc,
            CreatedByUserId = admin.Id,
        });

        var targetDataEntryTypes = new[]
        {
            "Rolls",
            "Barcode-LongSeam",
            "Fitup",
            "Barcode-RoundSeam",
            "Hydro",
        };
        var targetWorkCenterIds = targetDataEntryTypes
            .Where(workCenters.ContainsKey)
            .Select(key => workCenters[key].Id)
            .ToHashSet();
        var capacityTargets = new List<WorkCenterCapacityTarget>();
        foreach (var entry in wcplByWcId.Values.Where(v => targetWorkCenterIds.Contains(v.WorkCenterId)))
        {
            capacityTargets.Add(new WorkCenterCapacityTarget
            {
                Id = Guid.NewGuid(),
                WorkCenterProductionLineId = entry.Id,
                TankSize = 120,
                PlantGearId = plantGear.Id,
                TargetUnitsPerHour = 6,
            });
        }
        _db.WorkCenterCapacityTargets.AddRange(capacityTargets);

        var rollsWc = workCenters["Rolls"];
        var lsWc = workCenters["Barcode-LongSeam"];
        var lsInspWc = workCenters["Barcode-LongSeamInsp"];
        var fitupWc = workCenters["Fitup"];
        var rsWc = workCenters["Barcode-RoundSeam"];
        var rsInspWc = workCenters["Barcode-RoundSeamInsp"];
        var hydroWc = workCenters["Hydro"];

        for (var i = 1; i <= 12; i++)
        {
            var shellStamp = baseUtc.AddHours(i);
            var shellSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"9{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                CreatedAt = shellStamp,
                CreatedByUserId = admin.Id,
            };
            _db.SerialNumbers.Add(shellSn);

            var rollsRecord = CreateProductionRecord(shellSn.Id, rollsWc.Id, line.Id, op.Id, plantGear.Id, shellStamp);
            var longSeamRecord = CreateProductionRecord(shellSn.Id, lsWc.Id, line.Id, welder.Id, plantGear.Id, shellStamp.AddMinutes(20));
            var longSeamInspRecord = CreateProductionRecord(shellSn.Id, lsInspWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(35));
            _db.ProductionRecords.AddRange(rollsRecord, longSeamRecord, longSeamInspRecord);

            _db.InspectionRecords.Add(new InspectionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = shellSn.Id,
                ProductionRecordId = longSeamInspRecord.Id,
                WorkCenterId = lsInspWc.Id,
                OperatorId = op.Id,
                Timestamp = shellStamp.AddMinutes(36),
                ControlPlanId = controlPlans[0].Id,
                ResultText = i % 4 == 0 ? "Fail" : "Pass",
            });

            if (i % 3 == 0)
            {
                _db.DefectLogs.Add(new DefectLog
                {
                    Id = Guid.NewGuid(),
                    ProductionRecordId = longSeamInspRecord.Id,
                    SerialNumberId = shellSn.Id,
                    DefectCodeId = defectCode.Id,
                    CharacteristicId = longSeamChar.Id,
                    LocationId = defectLocation.Id,
                    IsRepaired = i % 6 == 0,
                    RepairedByUserId = i % 6 == 0 ? op.Id : null,
                    RepairedDateTime = i % 6 == 0 ? shellStamp.AddMinutes(80) : null,
                    CreatedByUserId = op.Id,
                    CreatedAt = shellStamp.AddMinutes(38),
                    Timestamp = shellStamp.AddMinutes(38),
                });
            }

            if (i % 4 == 0)
            {
                _db.Annotations.Add(new Annotation
                {
                    Id = Guid.NewGuid(),
                    ProductionRecordId = longSeamInspRecord.Id,
                    SerialNumberId = shellSn.Id,
                    AnnotationTypeId = annotationTypeDefect.Id,
                    Status = AnnotationStatus.Open,
                    Notes = "Demo quality hold for supervisor follow-up.",
                    InitiatedByUserId = op.Id,
                    CreatedAt = shellStamp.AddMinutes(42),
                });
            }

            if (i <= 10)
            {
                var assemblySn = new SerialNumber
                {
                    Id = Guid.NewGuid(),
                    Serial = $"A{i:00}",
                    PlantId = plant.Id,
                    ProductId = assembledProduct.Id,
                    CreatedAt = shellStamp.AddMinutes(55),
                    CreatedByUserId = op.Id,
                };
                _db.SerialNumbers.Add(assemblySn);

                var fitupRecord = CreateProductionRecord(assemblySn.Id, fitupWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(60));
                var rsRecord = CreateProductionRecord(assemblySn.Id, rsWc.Id, line.Id, welder.Id, plantGear.Id, shellStamp.AddMinutes(80));
                _db.ProductionRecords.AddRange(fitupRecord, rsRecord);

                _db.TraceabilityLogs.Add(new TraceabilityLog
                {
                    Id = Guid.NewGuid(),
                    FromSerialNumberId = shellSn.Id,
                    ToSerialNumberId = assemblySn.Id,
                    ProductionRecordId = fitupRecord.Id,
                    Relationship = "ShellToAssembly",
                    Quantity = 1,
                    Timestamp = shellStamp.AddMinutes(60),
                });

                if (i <= 8)
                {
                    var rsInspRecord = CreateProductionRecord(assemblySn.Id, rsInspWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(95));
                    _db.ProductionRecords.Add(rsInspRecord);
                    _db.InspectionRecords.Add(new InspectionRecord
                    {
                        Id = Guid.NewGuid(),
                        SerialNumberId = assemblySn.Id,
                        ProductionRecordId = rsInspRecord.Id,
                        WorkCenterId = rsInspWc.Id,
                        OperatorId = op.Id,
                        Timestamp = shellStamp.AddMinutes(96),
                        ControlPlanId = controlPlans[1].Id,
                        ResultText = i % 5 == 0 ? "Fail" : "Pass",
                    });
                }

                if (i <= 6)
                {
                    var sellableSn = new SerialNumber
                    {
                        Id = Guid.NewGuid(),
                        Serial = $"W0099{i:000}",
                        PlantId = plant.Id,
                        ProductId = sellableProduct.Id,
                        CreatedAt = shellStamp.AddMinutes(125),
                        CreatedByUserId = op.Id,
                    };
                    _db.SerialNumbers.Add(sellableSn);

                    var hydroRecord = CreateProductionRecord(sellableSn.Id, hydroWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(130));
                    _db.ProductionRecords.Add(hydroRecord);
                    _db.InspectionRecords.Add(new InspectionRecord
                    {
                        Id = Guid.NewGuid(),
                        SerialNumberId = sellableSn.Id,
                        ProductionRecordId = hydroRecord.Id,
                        WorkCenterId = hydroWc.Id,
                        OperatorId = op.Id,
                        Timestamp = shellStamp.AddMinutes(131),
                        ControlPlanId = controlPlans[2].Id,
                        ResultText = "Pass",
                    });

                    _db.TraceabilityLogs.Add(new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = assemblySn.Id,
                        ToSerialNumberId = sellableSn.Id,
                        ProductionRecordId = hydroRecord.Id,
                        Relationship = "AssemblyToSellable",
                        Quantity = 1,
                        Timestamp = shellStamp.AddMinutes(130),
                    });

                    _db.PrintLogs.Add(new PrintLog
                    {
                        Id = Guid.NewGuid(),
                        SerialNumberId = sellableSn.Id,
                        RequestedByUserId = op.Id,
                        PrinterName = "Demo Printer 1",
                        RequestedAt = shellStamp.AddMinutes(132),
                        Succeeded = true,
                    });
                }
            }
        }

        var queueWc = workCenters["MatQueue-Material"];
        for (var i = 1; i <= 3; i++)
        {
            var queuedAt = baseUtc.AddHours(2).AddMinutes(i * 15);
            var plateSerial = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"Heat DMO{i:000} Coil C{i:000}",
                PlantId = plant.Id,
                ProductId = plateProduct.Id,
                HeatNumber = $"DMO{i:000}",
                CoilNumber = $"C{i:000}",
                CreatedAt = queuedAt,
                CreatedByUserId = op.Id,
            };
            _db.SerialNumbers.Add(plateSerial);
            _db.MaterialQueueItems.Add(new MaterialQueueItem
            {
                Id = Guid.NewGuid(),
                WorkCenterId = queueWc.Id,
                ProductionLineId = line.Id,
                Position = i,
                Status = i == 1 ? "active" : "queued",
                Quantity = 2,
                QuantityCompleted = i == 1 ? 1 : 0,
                QueueType = "rolls",
                SerialNumberId = plateSerial.Id,
                CreatedAt = queuedAt,
                OperatorId = op.Id,
            });
            _db.QueueTransactions.Add(new QueueTransaction
            {
                Id = Guid.NewGuid(),
                WorkCenterId = queueWc.Id,
                Action = "added",
                ItemSummary = $"Demo plate lot {i}",
                OperatorName = op.DisplayName,
                Timestamp = queuedAt,
            });
        }

        _db.ActiveSessions.Add(new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = op.Id,
            PlantId = plant.Id,
            ProductionLineId = line.Id,
            WorkCenterId = rollsWc.Id,
            LoginDateTime = baseUtc.AddHours(7),
            LastHeartbeatDateTime = baseUtc.AddHours(7).AddMinutes(5),
        });

        var downtimeEvent = new DowntimeEvent
        {
            Id = Guid.NewGuid(),
            WorkCenterProductionLineId = wcplByWcId[rsWc.Id].Id,
            OperatorUserId = op.Id,
            DowntimeReasonId = reason.Id,
            StartedAt = baseUtc.AddHours(8),
            EndedAt = baseUtc.AddHours(8).AddMinutes(22),
            DurationMinutes = 22,
            IsAutoGenerated = false,
            CreatedAt = baseUtc.AddHours(8).AddMinutes(22),
        };
        _db.DowntimeEvents.Add(downtimeEvent);

        _db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            DowntimeEventId = downtimeEvent.Id,
            AnnotationTypeId = annotationTypeCorrection.Id,
            Status = AnnotationStatus.Open,
            Notes = "Demo downtime event requires review.",
            InitiatedByUserId = admin.Id,
            CreatedAt = baseUtc.AddHours(8).AddMinutes(25),
        });

        _db.IssueRequests.Add(new IssueRequest
        {
            Id = Guid.NewGuid(),
            Type = IssueRequestType.FeatureRequest,
            Status = IssueRequestStatus.Pending,
            Title = "Demo request: additional dashboard tile",
            Area = "Supervisor Dashboard",
            BodyJson = "{\"description\":\"Need extra KPI card for demo.\"}",
            SubmittedByUserId = admin.Id,
            SubmittedAt = baseUtc.AddDays(1),
        });

        _db.DemoShellFlows.Add(new DemoShellFlow
        {
            Id = Guid.NewGuid(),
            PlantId = plant.Id,
            CreatedByUserId = admin.Id,
            CurrentStage = DemoShellStage.LongSeam,
            ShellNumber = 5001,
            SerialNumber = "005001",
            CreatedAtUtc = baseUtc.AddHours(1),
            StageEnteredAtUtc = baseUtc.AddHours(2),
        });

        await _db.SaveChangesAsync(ct);
    }

    private static ProductionRecord CreateProductionRecord(
        Guid serialId,
        Guid workCenterId,
        Guid productionLineId,
        Guid operatorId,
        Guid plantGearId,
        DateTime tsUtc)
    {
        return new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serialId,
            WorkCenterId = workCenterId,
            ProductionLineId = productionLineId,
            OperatorId = operatorId,
            PlantGearId = plantGearId,
            Timestamp = tsUtc,
        };
    }

    private static void AddCount(string table, int count, ICollection<DemoDataTableCountDto> list)
    {
        if (count > 0)
            list.Add(new DemoDataTableCountDto { Table = table, Count = count });
    }

    private async Task DeleteAllAsync<TEntity>(
        string tableName,
        ICollection<DemoDataTableCountDto> deleted,
        CancellationToken ct) where TEntity : class
    {
        var set = _db.Set<TEntity>();
        var count = await set.CountAsync(ct);
        if (count == 0)
            return;

        if (_db.Database.IsRelational())
        {
            await set.ExecuteDeleteAsync(ct);
        }
        else
        {
            var rows = await set.ToListAsync(ct);
            set.RemoveRange(rows);
            await _db.SaveChangesAsync(ct);
        }

        deleted.Add(new DemoDataTableCountDto { Table = tableName, Count = count });
    }

    private void EnsureDemoOperationsAllowed()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("Demo data admin tools are disabled.");

        if (_options.AllowedEnvironments.Count > 0 &&
            !_options.AllowedEnvironments.Any(env =>
                string.Equals(env, _environment.EnvironmentName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Demo data admin tools are blocked in environment '{_environment.EnvironmentName}'.");
        }

        if (!string.IsNullOrWhiteSpace(_options.RequiredConnectionStringContains))
        {
            var marker = _options.RequiredConnectionStringContains.Trim();
            var connectionString = _db.Database.GetConnectionString() ?? string.Empty;
            if (!connectionString.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Demo data operation blocked because connection string did not include required marker '{Marker}'.", marker);
                throw new InvalidOperationException("Demo data admin tools are blocked for this database.");
            }
        }
    }
}
