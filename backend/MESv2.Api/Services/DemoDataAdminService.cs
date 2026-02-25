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

        var referenceSnapshot = await CaptureReferenceSnapshotAsync(ct);
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
            await ApplyReferenceSnapshotAsync(referenceSnapshot, ct);
            await ApplyDemoPlantGearDefaultsAsync(ct);
            _db.ChangeTracker.Clear();
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
        const string targetPlantCode = "700";
        const string targetLineName = "Main Line";
        const int shellCount = 120;
        const int assemblyCount = 120;
        const int roundSeamInspectionCount = 120;
        const int sellableCount = 120;
        const int queueSeedCount = 20;

        var plant = await _db.Plants
            .FirstOrDefaultAsync(p => p.Code == targetPlantCode, ct)
            ?? await _db.Plants.FirstAsync(ct);
        var line = await _db.ProductionLines
            .FirstOrDefaultAsync(pl => pl.PlantId == plant.Id && pl.Name == targetLineName, ct)
            ?? await _db.ProductionLines.FirstAsync(pl => pl.PlantId == plant.Id, ct);

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeNumber == "EMP001", ct)
            ?? await _db.Users.OrderBy(u => u.RoleTier).FirstAsync(ct);
        var op = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeNumber == "EMP002", ct)
            ?? await _db.Users.Where(u => u.Id != admin.Id).OrderByDescending(u => u.RoleTier).FirstAsync(ct);
        var welder = await _db.Users.FirstOrDefaultAsync(u => u.EmployeeNumber == "EMP003", ct)
            ?? op;

        var workCenters = await _db.WorkCenters
            .Where(w => w.DataEntryType != null)
            .ToDictionaryAsync(w => w.DataEntryType!, w => w, ct);

        var wcplByWcId = await _db.WorkCenterProductionLines
            .Where(x => x.ProductionLineId == line.Id)
            .ToDictionaryAsync(x => x.WorkCenterId, x => x, ct);
        var plantWcpls = await _db.WorkCenterProductionLines
            .Where(x => x.ProductionLine.PlantId == plant.Id)
            .ToListAsync(ct);
        var plantWcplsByWcId = plantWcpls
            .GroupBy(x => x.WorkCenterId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ProductionLine.Name).First());

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

        var longSeamChar = await _db.Characteristics
            .FirstOrDefaultAsync(c => c.Name == "Long Seam", ct)
            ?? await _db.Characteristics.FirstAsync(ct);
        var rs1Char = await _db.Characteristics
            .FirstOrDefaultAsync(c => c.Name == "RS1", ct)
            ?? longSeamChar;
        var defectCode = await _db.DefectCodes.FirstAsync(dc => dc.Code == "101", ct);
        var defectLocation = await _db.DefectLocations
            .FirstOrDefaultAsync(dl => dl.Code == "1", ct)
            ?? await _db.DefectLocations.FirstAsync(ct);
        var annotationTypeDefect = await _db.AnnotationTypes.FirstAsync(a => a.Name == "Defect", ct);
        var annotationTypeCorrection = await _db.AnnotationTypes.FirstAsync(a => a.Name == "Correction Needed", ct);
        var plantGear = await _db.PlantGears
            .FirstOrDefaultAsync(pg => pg.PlantId == plant.Id && pg.Level == 4, ct)
            ?? await _db.PlantGears.FirstAsync(pg => pg.PlantId == plant.Id, ct);

        WorkCenter ResolveWorkCenter(string dataEntryType) =>
            workCenters.TryGetValue(dataEntryType, out var wc)
                ? wc
                : workCenters.Values.First();
        WorkCenterProductionLine ResolveWcpl(string dataEntryType)
        {
            var workCenter = ResolveWorkCenter(dataEntryType);
            return wcplByWcId.TryGetValue(workCenter.Id, out var wcpl)
                ? wcpl
                : plantWcplsByWcId.TryGetValue(workCenter.Id, out var plantWcpl)
                    ? plantWcpl
                    : wcplByWcId.Values.First();
        }

        var longSeamInspWcpl = ResolveWcpl("Barcode-LongSeamInsp");
        var roundSeamInspWcpl = ResolveWcpl("Barcode-RoundSeamInsp");
        var hydroWcpl = ResolveWcpl("Hydro");

        var controlPlanLongSeam = await _db.ControlPlans
            .FirstOrDefaultAsync(cp =>
                cp.CharacteristicId == longSeamChar.Id &&
                cp.WorkCenterProductionLineId == longSeamInspWcpl.Id, ct);
        if (controlPlanLongSeam is null)
        {
            controlPlanLongSeam = new ControlPlan
            {
                Id = Guid.NewGuid(),
                CharacteristicId = longSeamChar.Id,
                WorkCenterProductionLineId = longSeamInspWcpl.Id,
                IsEnabled = true,
                ResultType = "PassFail",
                IsGateCheck = false,
                CodeRequired = true,
            };
            _db.ControlPlans.Add(controlPlanLongSeam);
        }

        var controlPlanRoundSeam = await _db.ControlPlans
            .FirstOrDefaultAsync(cp =>
                cp.CharacteristicId == rs1Char.Id &&
                cp.WorkCenterProductionLineId == roundSeamInspWcpl.Id, ct);
        if (controlPlanRoundSeam is null)
        {
            controlPlanRoundSeam = new ControlPlan
            {
                Id = Guid.NewGuid(),
                CharacteristicId = rs1Char.Id,
                WorkCenterProductionLineId = roundSeamInspWcpl.Id,
                IsEnabled = true,
                ResultType = "PassFail",
                IsGateCheck = true,
                CodeRequired = true,
            };
            _db.ControlPlans.Add(controlPlanRoundSeam);
        }

        var controlPlanHydro = await _db.ControlPlans
            .FirstOrDefaultAsync(cp =>
                cp.CharacteristicId == rs1Char.Id &&
                cp.WorkCenterProductionLineId == hydroWcpl.Id, ct);
        if (controlPlanHydro is null)
        {
            controlPlanHydro = new ControlPlan
            {
                Id = Guid.NewGuid(),
                CharacteristicId = rs1Char.Id,
                WorkCenterProductionLineId = hydroWcpl.Id,
                IsEnabled = true,
                ResultType = "PassFail",
                IsGateCheck = true,
                CodeRequired = false,
            };
            _db.ControlPlans.Add(controlPlanHydro);
        }

        var controlPlans = new[] { controlPlanLongSeam, controlPlanRoundSeam, controlPlanHydro };

        // Clone the current Cleveland downtime library to every plant in demo seed data.
        var downtimeTemplate = new[]
        {
            new
            {
                CategoryName = "Other",
                CategorySortOrder = 1,
                IsCategoryActive = true,
                Reasons = new[]
                {
                    new { Name = "Not Downtime", SortOrder = 0, IsActive = true, CountsAsDowntime = false },
                },
            },
            new
            {
                CategoryName = "Machine",
                CategorySortOrder = 2,
                IsCategoryActive = true,
                Reasons = new[]
                {
                    new { Name = "Equipment Down", SortOrder = 1, IsActive = true, CountsAsDowntime = true },
                    new { Name = "Welder Setup Delay", SortOrder = 1, IsActive = true, CountsAsDowntime = true },
                },
            },
            new
            {
                CategoryName = "Man",
                CategorySortOrder = 2,
                IsCategoryActive = true,
                Reasons = new[]
                {
                    new { Name = "Training", SortOrder = 0, IsActive = true, CountsAsDowntime = true },
                    new { Name = "Break", SortOrder = 1, IsActive = true, CountsAsDowntime = false },
                },
            },
            new
            {
                CategoryName = "Method",
                CategorySortOrder = 4,
                IsCategoryActive = true,
                Reasons = new[]
                {
                    new { Name = "Bad Work Instruction", SortOrder = 0, IsActive = true, CountsAsDowntime = true },
                },
            },
            new
            {
                CategoryName = "Material",
                CategorySortOrder = 5,
                IsCategoryActive = true,
                Reasons = new[]
                {
                    new { Name = "Defective Material", SortOrder = 0, IsActive = true, CountsAsDowntime = true },
                    new { Name = "No Material", SortOrder = 1, IsActive = true, CountsAsDowntime = true },
                },
            },
            new
            {
                CategoryName = "Environment",
                CategorySortOrder = 6,
                IsCategoryActive = true,
                Reasons = new[]
                {
                    new { Name = "Plant Emergency", SortOrder = 0, IsActive = true, CountsAsDowntime = true },
                    new { Name = "Plant Tour", SortOrder = 1, IsActive = true, CountsAsDowntime = true },
                },
            },
        };

        var reasonIdsByPlant = new Dictionary<Guid, Dictionary<string, Guid>>();
        var plants = await _db.Plants.ToListAsync(ct);
        foreach (var p in plants)
        {
            var reasonIdsByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var categoryTemplate in downtimeTemplate)
            {
                var category = new DowntimeReasonCategory
                {
                    Id = Guid.NewGuid(),
                    PlantId = p.Id,
                    Name = categoryTemplate.CategoryName,
                    SortOrder = categoryTemplate.CategorySortOrder,
                    IsActive = categoryTemplate.IsCategoryActive,
                };
                _db.DowntimeReasonCategories.Add(category);

                foreach (var reasonTemplate in categoryTemplate.Reasons)
                {
                    var reason = new DowntimeReason
                    {
                        Id = Guid.NewGuid(),
                        DowntimeReasonCategoryId = category.Id,
                        Name = reasonTemplate.Name,
                        IsActive = reasonTemplate.IsActive,
                        CountsAsDowntime = reasonTemplate.CountsAsDowntime,
                        SortOrder = reasonTemplate.SortOrder,
                    };
                    _db.DowntimeReasons.Add(reason);
                    reasonIdsByName[reason.Name] = reason.Id;
                }
            }

            reasonIdsByPlant[p.Id] = reasonIdsByName;
        }

        var selectedReasonId = reasonIdsByPlant[plant.Id].TryGetValue("Welder Setup Delay", out var mappedReasonId)
            ? mappedReasonId
            : reasonIdsByPlant[plant.Id].Values.First();
        _db.WorkCenterProductionLineDowntimeReasons.Add(new WorkCenterProductionLineDowntimeReason
        {
            Id = Guid.Parse("8c440001-0000-0000-0000-000000000003"),
            WorkCenterProductionLineId = ResolveWcpl("Barcode-RoundSeam").Id,
            DowntimeReasonId = selectedReasonId,
        });

        var clevelandPlantId = await _db.Plants
            .Where(p => p.Code == "000")
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);
        var westJordanPlantId = await _db.Plants
            .Where(p => p.Code == "700")
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);
        if (westJordanPlantId == Guid.Empty)
        {
            westJordanPlantId = plant.Id;
        }
        if (clevelandPlantId == Guid.Empty)
        {
            clevelandPlantId = plant.Id;
        }

        _db.ShiftSchedules.AddRange(
            new ShiftSchedule
            {
                Id = Guid.Parse("9d550001-0000-0000-0000-000000000001"),
                PlantId = clevelandPlantId, // 000 - Cleveland
                EffectiveDate = new DateOnly(2026, 2, 23),
                MondayHours = 9, MondayBreakMinutes = 30,
                TuesdayHours = 9, TuesdayBreakMinutes = 30,
                WednesdayHours = 9, WednesdayBreakMinutes = 30,
                ThursdayHours = 9, ThursdayBreakMinutes = 30,
                FridayHours = 8, FridayBreakMinutes = 30,
                SaturdayHours = 5, SaturdayBreakMinutes = 15,
                SundayHours = 0, SundayBreakMinutes = 0,
                CreatedAt = baseUtc,
                CreatedByUserId = admin.Id,
            },
            new ShiftSchedule
            {
                Id = Guid.Parse("9d550001-0000-0000-0000-000000000002"),
                PlantId = westJordanPlantId, // 700 - West Jordan
                EffectiveDate = new DateOnly(2026, 2, 22),
                MondayHours = 10, MondayBreakMinutes = 30,
                TuesdayHours = 10, TuesdayBreakMinutes = 30,
                WednesdayHours = 10, WednesdayBreakMinutes = 30,
                ThursdayHours = 10, ThursdayBreakMinutes = 30,
                FridayHours = 0, FridayBreakMinutes = 0,
                SaturdayHours = 0, SaturdayBreakMinutes = 0,
                SundayHours = 0, SundayBreakMinutes = 0,
                CreatedAt = baseUtc,
                CreatedByUserId = admin.Id,
            },
            new ShiftSchedule
            {
                Id = Guid.Parse("9d550001-0000-0000-0000-000000000003"),
                PlantId = westJordanPlantId, // 700 - West Jordan
                EffectiveDate = new DateOnly(2026, 3, 1),
                MondayHours = 10, MondayBreakMinutes = 30,
                TuesdayHours = 10, TuesdayBreakMinutes = 30,
                WednesdayHours = 10, WednesdayBreakMinutes = 30,
                ThursdayHours = 10, ThursdayBreakMinutes = 30,
                FridayHours = 6, FridayBreakMinutes = 0,
                SaturdayHours = 0, SaturdayBreakMinutes = 0,
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
        var candidateWcplIds = wcplByWcId.Values
            .Where(v => targetWorkCenterIds.Contains(v.WorkCenterId))
            .Select(v => v.Id)
            .ToHashSet();
        var hasCapacityTargetsForLine = await _db.WorkCenterCapacityTargets
            .AnyAsync(t => candidateWcplIds.Contains(t.WorkCenterProductionLineId), ct);
        var existingCapacityTargetKeys = await _db.WorkCenterCapacityTargets
            .Where(t => candidateWcplIds.Contains(t.WorkCenterProductionLineId) && t.TankSize == 120 && t.PlantGearId == plantGear.Id)
            .Select(t => t.WorkCenterProductionLineId)
            .ToHashSetAsync(ct);
        var capacityTargets = new List<WorkCenterCapacityTarget>();
        if (!hasCapacityTargetsForLine)
        {
            foreach (var entry in wcplByWcId.Values.Where(v => targetWorkCenterIds.Contains(v.WorkCenterId)))
            {
                if (existingCapacityTargetKeys.Contains(entry.Id))
                    continue;
                capacityTargets.Add(new WorkCenterCapacityTarget
                {
                    Id = Guid.NewGuid(),
                    WorkCenterProductionLineId = entry.Id,
                    TankSize = 120,
                    PlantGearId = plantGear.Id,
                    TargetUnitsPerHour = 6,
                });
            }
        }
        _db.WorkCenterCapacityTargets.AddRange(capacityTargets);

        var rollsWc = ResolveWorkCenter("Rolls");
        var lsWc = ResolveWorkCenter("Barcode-LongSeam");
        var lsInspWc = ResolveWorkCenter("Barcode-LongSeamInsp");
        var fitupWc = ResolveWorkCenter("Fitup");
        var rsWc = ResolveWorkCenter("Barcode-RoundSeam");
        var rsInspWc = ResolveWorkCenter("Barcode-RoundSeamInsp");
        var hydroWc = ResolveWorkCenter("Hydro");
        workCenters.TryGetValue("MatQueue-Shell", out var rtXrayWc);
        workCenters.TryGetValue("Spot", out var spotWc);

        for (var i = 1; i <= shellCount; i++)
        {
            var shellStamp = baseUtc.AddHours(i);
            var shellSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"9{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                HeatNumber = $"H{i:00000}",
                CoilNumber = $"C{i:00000}",
                LotNumber = $"L{i:00000}",
                CreatedAt = shellStamp,
                CreatedByUserId = admin.Id,
            };
            _db.SerialNumbers.Add(shellSn);

            var rollsRecord = CreateProductionRecord(shellSn.Id, rollsWc.Id, line.Id, op.Id, plantGear.Id, shellStamp);
            var longSeamRecord = CreateProductionRecord(shellSn.Id, lsWc.Id, line.Id, welder.Id, plantGear.Id, shellStamp.AddMinutes(20));
            var longSeamInspRecord = CreateProductionRecord(shellSn.Id, lsInspWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(35));
            _db.ProductionRecords.AddRange(rollsRecord, longSeamRecord, longSeamInspRecord);
            if (rtXrayWc is not null)
            {
                var rtXrayRecord = CreateProductionRecord(shellSn.Id, rtXrayWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(50));
                _db.ProductionRecords.Add(rtXrayRecord);
            }

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

            if (i <= assemblyCount)
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

                var leftHeadSn = new SerialNumber
                {
                    Id = Guid.NewGuid(),
                    Serial = $"HL{i:00000}",
                    PlantId = plant.Id,
                    HeatNumber = $"HHL{i:00000}",
                    CoilNumber = $"CHL{i:00000}",
                    LotNumber = $"LHL{i:00000}",
                    CreatedAt = shellStamp.AddMinutes(58),
                    CreatedByUserId = op.Id,
                };
                var rightHeadSn = new SerialNumber
                {
                    Id = Guid.NewGuid(),
                    Serial = $"HR{i:00000}",
                    PlantId = plant.Id,
                    HeatNumber = $"HHR{i:00000}",
                    CoilNumber = $"CHR{i:00000}",
                    LotNumber = $"LHR{i:00000}",
                    CreatedAt = shellStamp.AddMinutes(59),
                    CreatedByUserId = op.Id,
                };
                _db.SerialNumbers.AddRange(leftHeadSn, rightHeadSn);

                _db.TraceabilityLogs.AddRange(
                    new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = leftHeadSn.Id,
                        ToSerialNumberId = assemblySn.Id,
                        ProductionRecordId = fitupRecord.Id,
                        Relationship = "leftHead",
                        TankLocation = "Head 1",
                        Quantity = 1,
                        Timestamp = shellStamp.AddMinutes(60),
                    },
                    new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = rightHeadSn.Id,
                        ToSerialNumberId = assemblySn.Id,
                        ProductionRecordId = fitupRecord.Id,
                        Relationship = "rightHead",
                        TankLocation = "Head 2",
                        Quantity = 1,
                        Timestamp = shellStamp.AddMinutes(61),
                    });

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

                if (i <= roundSeamInspectionCount)
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

                    if (spotWc is not null)
                    {
                        var spotRecord = CreateProductionRecord(assemblySn.Id, spotWc.Id, line.Id, op.Id, plantGear.Id, shellStamp.AddMinutes(110));
                        _db.ProductionRecords.Add(spotRecord);
                    }
                }

                if (i <= sellableCount)
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
                        Relationship = "hydro-marriage",
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

        // Add staged in-progress units to keep upstream Digital Twin stations non-zero WIP
        // while preserving completed hydro flow for sellable/status views.
        var upstreamBase = baseUtc.AddHours(shellCount - 2);
        for (var i = 1; i <= 24; i++)
        {
            var stageStamp = upstreamBase.AddMinutes(i * 2);
            var shellSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"U9{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                HeatNumber = $"UH{i:00000}",
                CoilNumber = $"UC{i:00000}",
                LotNumber = $"UL{i:00000}",
                CreatedAt = stageStamp,
                CreatedByUserId = op.Id,
            };
            _db.SerialNumbers.Add(shellSn);

            var rollsRecord = CreateProductionRecord(shellSn.Id, rollsWc.Id, line.Id, op.Id, plantGear.Id, stageStamp);
            _db.ProductionRecords.Add(rollsRecord);

            if (i > 6)
            {
                var longSeamRecord = CreateProductionRecord(shellSn.Id, lsWc.Id, line.Id, welder.Id, plantGear.Id, stageStamp.AddMinutes(8));
                _db.ProductionRecords.Add(longSeamRecord);
            }
            if (i > 12)
            {
                var lsInspRecord = CreateProductionRecord(shellSn.Id, lsInspWc.Id, line.Id, op.Id, plantGear.Id, stageStamp.AddMinutes(14));
                _db.ProductionRecords.Add(lsInspRecord);
            }
            if (rtXrayWc is not null && i > 18)
            {
                var rtRecord = CreateProductionRecord(shellSn.Id, rtXrayWc.Id, line.Id, op.Id, plantGear.Id, stageStamp.AddMinutes(18));
                _db.ProductionRecords.Add(rtRecord);
            }
        }

        for (var i = 1; i <= 8; i++)
        {
            var fitupStamp = upstreamBase.AddMinutes(140 + i * 3);
            var shellSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"UF{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                HeatNumber = $"FH{i:00000}",
                CoilNumber = $"FC{i:00000}",
                LotNumber = $"FL{i:00000}",
                CreatedAt = fitupStamp.AddMinutes(-4),
                CreatedByUserId = op.Id,
            };
            var assemblySn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"UA{i:000}",
                PlantId = plant.Id,
                ProductId = assembledProduct.Id,
                CreatedAt = fitupStamp,
                CreatedByUserId = op.Id,
            };
            _db.SerialNumbers.AddRange(shellSn, assemblySn);
            var fitupRecord = CreateProductionRecord(assemblySn.Id, fitupWc.Id, line.Id, op.Id, plantGear.Id, fitupStamp);
            _db.ProductionRecords.Add(fitupRecord);
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shellSn.Id,
                ToSerialNumberId = assemblySn.Id,
                ProductionRecordId = fitupRecord.Id,
                Relationship = "ShellToAssembly",
                Quantity = 1,
                Timestamp = fitupStamp,
            });
        }

        for (var i = 1; i <= 6; i++)
        {
            var seamStamp = upstreamBase.AddMinutes(180 + i * 3);
            var shellSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"UR{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                HeatNumber = $"RH{i:00000}",
                CoilNumber = $"RC{i:00000}",
                LotNumber = $"RL{i:00000}",
                CreatedAt = seamStamp.AddMinutes(-6),
                CreatedByUserId = op.Id,
            };
            var assemblySn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"URS{i:000}",
                PlantId = plant.Id,
                ProductId = assembledProduct.Id,
                CreatedAt = seamStamp.AddMinutes(-2),
                CreatedByUserId = op.Id,
            };
            _db.SerialNumbers.AddRange(shellSn, assemblySn);
            var fitupRecord = CreateProductionRecord(assemblySn.Id, fitupWc.Id, line.Id, op.Id, plantGear.Id, seamStamp.AddMinutes(-1));
            var rsRecord = CreateProductionRecord(assemblySn.Id, rsWc.Id, line.Id, welder.Id, plantGear.Id, seamStamp);
            _db.ProductionRecords.AddRange(fitupRecord, rsRecord);
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shellSn.Id,
                ToSerialNumberId = assemblySn.Id,
                ProductionRecordId = fitupRecord.Id,
                Relationship = "ShellToAssembly",
                Quantity = 1,
                Timestamp = seamStamp.AddMinutes(-1),
            });
        }

        for (var i = 1; i <= 6; i++)
        {
            var inspStamp = upstreamBase.AddMinutes(220 + i * 3);
            var shellSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"UI{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                HeatNumber = $"IH{i:00000}",
                CoilNumber = $"IC{i:00000}",
                LotNumber = $"IL{i:00000}",
                CreatedAt = inspStamp.AddMinutes(-8),
                CreatedByUserId = op.Id,
            };
            var assemblySn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"UIS{i:000}",
                PlantId = plant.Id,
                ProductId = assembledProduct.Id,
                CreatedAt = inspStamp.AddMinutes(-4),
                CreatedByUserId = op.Id,
            };
            _db.SerialNumbers.AddRange(shellSn, assemblySn);
            var fitupRecord = CreateProductionRecord(assemblySn.Id, fitupWc.Id, line.Id, op.Id, plantGear.Id, inspStamp.AddMinutes(-3));
            var rsRecord = CreateProductionRecord(assemblySn.Id, rsWc.Id, line.Id, welder.Id, plantGear.Id, inspStamp.AddMinutes(-2));
            var rsInspRecord = CreateProductionRecord(assemblySn.Id, rsInspWc.Id, line.Id, op.Id, plantGear.Id, inspStamp);
            _db.ProductionRecords.AddRange(fitupRecord, rsRecord, rsInspRecord);
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shellSn.Id,
                ToSerialNumberId = assemblySn.Id,
                ProductionRecordId = fitupRecord.Id,
                Relationship = "ShellToAssembly",
                Quantity = 1,
                Timestamp = inspStamp.AddMinutes(-3),
            });
        }

        // Seed a small "live pulse" at upstream stations so Digital Twin status
        // reflects active/slow behavior (based on recent scan timestamps).
        var livePulseBase = baseUtc.AddHours(shellCount).AddMinutes(122);
        for (var i = 1; i <= 8; i++)
        {
            var pulseStamp = livePulseBase.AddMinutes(i);
            var pulseShell = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"LP{i:00000}",
                PlantId = plant.Id,
                ProductId = shellProduct.Id,
                HeatNumber = $"LH{i:00000}",
                CoilNumber = $"LC{i:00000}",
                LotNumber = $"LL{i:00000}",
                CreatedAt = pulseStamp.AddMinutes(-2),
                CreatedByUserId = op.Id,
            };
            _db.SerialNumbers.Add(pulseShell);

            _db.ProductionRecords.Add(CreateProductionRecord(
                pulseShell.Id, rollsWc.Id, line.Id, op.Id, plantGear.Id, pulseStamp));
            _db.ProductionRecords.Add(CreateProductionRecord(
                pulseShell.Id, lsWc.Id, line.Id, welder.Id, plantGear.Id, pulseStamp.AddMinutes(1)));
            _db.ProductionRecords.Add(CreateProductionRecord(
                pulseShell.Id, lsInspWc.Id, line.Id, op.Id, plantGear.Id, pulseStamp.AddMinutes(2)));
            if (rtXrayWc is not null)
            {
                _db.ProductionRecords.Add(CreateProductionRecord(
                    pulseShell.Id, rtXrayWc.Id, line.Id, op.Id, plantGear.Id, pulseStamp.AddMinutes(3)));
            }
        }

        if (!workCenters.TryGetValue("MatQueue-Material", out var queueWc))
            queueWc = workCenters.Values.First();
        var hasSourceQueueData = await _db.MaterialQueueItems.AnyAsync(i => i.WorkCenterId == queueWc.Id, ct);
        if (!hasSourceQueueData)
        {
            for (var i = 1; i <= queueSeedCount; i++)
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
            DowntimeReasonId = selectedReasonId,
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

    private async Task ApplyDemoPlantGearDefaultsAsync(CancellationToken ct)
    {
        var desiredGearByPlantCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["000"] = 4,
            ["600"] = 2,
            ["700"] = 1,
        };

        var plants = await _db.Plants.ToListAsync(ct);
        foreach (var plant in plants)
        {
            if (!desiredGearByPlantCode.TryGetValue(plant.Code, out var desiredLevel))
                continue;

            var desiredGearId = await _db.PlantGears
                .Where(pg => pg.PlantId == plant.Id && pg.Level == desiredLevel)
                .Select(pg => (Guid?)pg.Id)
                .FirstOrDefaultAsync(ct);

            if (desiredGearId.HasValue)
                plant.CurrentPlantGearId = desiredGearId.Value;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<ReferenceSnapshot> CaptureReferenceSnapshotAsync(CancellationToken ct)
    {
        var queueWorkCenterIds = await _db.WorkCenters
            .Where(w => w.DataEntryType == "MatQueue-Material" || w.DataEntryType == "MatQueue-Fitup")
            .Select(w => w.Id)
            .ToListAsync(ct);
        var materialQueueItems = await _db.MaterialQueueItems
            .AsNoTracking()
            .Where(i => queueWorkCenterIds.Contains(i.WorkCenterId))
            .ToListAsync(ct);
        var queueSerialIds = materialQueueItems
            .Where(i => i.SerialNumberId.HasValue)
            .Select(i => i.SerialNumberId!.Value)
            .Distinct()
            .ToList();
        var queueSerialNumbers = await _db.SerialNumbers
            .AsNoTracking()
            .Where(sn => queueSerialIds.Contains(sn.Id))
            .ToListAsync(ct);

        return new ReferenceSnapshot
        {
            Users = await _db.Users.AsNoTracking().ToListAsync(ct),
            WorkCenterProductionLines = await _db.WorkCenterProductionLines.AsNoTracking().ToListAsync(ct),
            WorkCenterCapacityTargets = await _db.WorkCenterCapacityTargets.AsNoTracking().ToListAsync(ct),
            Characteristics = await _db.Characteristics.AsNoTracking().ToListAsync(ct),
            CharacteristicWorkCenters = await _db.CharacteristicWorkCenters.AsNoTracking().ToListAsync(ct),
            DefectLocations = await _db.DefectLocations.AsNoTracking().ToListAsync(ct),
            ControlPlans = await _db.ControlPlans.AsNoTracking().ToListAsync(ct),
            Products = await _db.Products.AsNoTracking().ToListAsync(ct),
            ProductPlants = await _db.ProductPlants.AsNoTracking().ToListAsync(ct),
            Vendors = await _db.Vendors.AsNoTracking().ToListAsync(ct),
            VendorPlants = await _db.VendorPlants.AsNoTracking().ToListAsync(ct),
            Assets = await _db.Assets.AsNoTracking().ToListAsync(ct),
            QueueSerialNumbers = queueSerialNumbers,
            MaterialQueueItems = materialQueueItems,
        };
    }

    private async Task ApplyReferenceSnapshotAsync(ReferenceSnapshot snapshot, CancellationToken ct)
    {
        // DbInitializer.Seed tracks many entities in this DbContext instance.
        // Clear tracked state before re-attaching snapshot rows with same primary keys.
        _db.ChangeTracker.Clear();

        if (snapshot.Users.Count > 0)
        {
            await ClearSetAsync(_db.Users, ct);
            await _db.Users.AddRangeAsync(snapshot.Users, ct);
        }

        if (snapshot.Products.Count > 0)
        {
            await ClearSetAsync(_db.ProductPlants, ct);
            await ClearSetAsync(_db.Products, ct);
            await _db.Products.AddRangeAsync(snapshot.Products, ct);
            if (snapshot.ProductPlants.Count > 0)
                await _db.ProductPlants.AddRangeAsync(snapshot.ProductPlants, ct);
        }

        if (snapshot.Vendors.Count > 0)
        {
            await ClearSetAsync(_db.VendorPlants, ct);
            await ClearSetAsync(_db.Vendors, ct);
            await _db.Vendors.AddRangeAsync(snapshot.Vendors, ct);
            if (snapshot.VendorPlants.Count > 0)
                await _db.VendorPlants.AddRangeAsync(snapshot.VendorPlants, ct);
        }

        if (snapshot.WorkCenterProductionLines.Count > 0)
        {
            await ClearSetAsync(_db.WorkCenterCapacityTargets, ct);
            await ClearSetAsync(_db.WorkCenterProductionLines, ct);
            await _db.WorkCenterProductionLines.AddRangeAsync(snapshot.WorkCenterProductionLines, ct);
            if (snapshot.WorkCenterCapacityTargets.Count > 0)
                await _db.WorkCenterCapacityTargets.AddRangeAsync(snapshot.WorkCenterCapacityTargets, ct);
        }

        await ClearSetAsync(_db.ControlPlans, ct);
        await ClearSetAsync(_db.CharacteristicWorkCenters, ct);
        await ClearSetAsync(_db.DefectLocations, ct);
        await ClearSetAsync(_db.Characteristics, ct);

        if (snapshot.Characteristics.Count > 0)
            await _db.Characteristics.AddRangeAsync(snapshot.Characteristics, ct);
        if (snapshot.CharacteristicWorkCenters.Count > 0)
            await _db.CharacteristicWorkCenters.AddRangeAsync(snapshot.CharacteristicWorkCenters, ct);
        if (snapshot.DefectLocations.Count > 0)
            await _db.DefectLocations.AddRangeAsync(snapshot.DefectLocations, ct);
        if (snapshot.ControlPlans.Count > 0)
            await _db.ControlPlans.AddRangeAsync(snapshot.ControlPlans, ct);

        if (snapshot.Assets.Count > 0)
        {
            await ClearSetAsync(_db.Assets, ct);
            await _db.Assets.AddRangeAsync(snapshot.Assets, ct);
        }

        if (snapshot.MaterialQueueItems.Count > 0 || snapshot.QueueSerialNumbers.Count > 0)
        {
            var queueWorkCenterIds = await _db.WorkCenters
                .Where(w => w.DataEntryType == "MatQueue-Material" || w.DataEntryType == "MatQueue-Fitup")
                .Select(w => w.Id)
                .ToListAsync(ct);

            var existingQueueSerialIds = await _db.MaterialQueueItems
                .Where(i => queueWorkCenterIds.Contains(i.WorkCenterId) && i.SerialNumberId.HasValue)
                .Select(i => i.SerialNumberId!.Value)
                .Distinct()
                .ToListAsync(ct);

            if (_db.Database.IsRelational())
            {
                await _db.MaterialQueueItems
                    .Where(i => queueWorkCenterIds.Contains(i.WorkCenterId))
                    .ExecuteDeleteAsync(ct);
                if (existingQueueSerialIds.Count > 0)
                {
                    await _db.SerialNumbers
                        .Where(sn => existingQueueSerialIds.Contains(sn.Id))
                        .ExecuteDeleteAsync(ct);
                }
            }
            else
            {
                var existingQueueItems = await _db.MaterialQueueItems
                    .Where(i => queueWorkCenterIds.Contains(i.WorkCenterId))
                    .ToListAsync(ct);
                if (existingQueueItems.Count > 0)
                {
                    _db.MaterialQueueItems.RemoveRange(existingQueueItems);
                    await _db.SaveChangesAsync(ct);
                }

                if (existingQueueSerialIds.Count > 0)
                {
                    var serialsToRemove = await _db.SerialNumbers
                        .Where(sn => existingQueueSerialIds.Contains(sn.Id))
                        .ToListAsync(ct);
                    if (serialsToRemove.Count > 0)
                    {
                        _db.SerialNumbers.RemoveRange(serialsToRemove);
                        await _db.SaveChangesAsync(ct);
                    }
                }
            }

            if (snapshot.QueueSerialNumbers.Count > 0)
                await _db.SerialNumbers.AddRangeAsync(snapshot.QueueSerialNumbers, ct);
            if (snapshot.MaterialQueueItems.Count > 0)
                await _db.MaterialQueueItems.AddRangeAsync(snapshot.MaterialQueueItems, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task ClearSetAsync<TEntity>(DbSet<TEntity> set, CancellationToken ct)
        where TEntity : class
    {
        if (_db.Database.IsRelational())
        {
            await set.ExecuteDeleteAsync(ct);
            return;
        }

        var existing = await set.ToListAsync(ct);
        if (existing.Count > 0)
        {
            set.RemoveRange(existing);
            await _db.SaveChangesAsync(ct);
        }
    }

    private sealed class ReferenceSnapshot
    {
        public List<User> Users { get; init; } = new();
        public List<WorkCenterProductionLine> WorkCenterProductionLines { get; init; } = new();
        public List<WorkCenterCapacityTarget> WorkCenterCapacityTargets { get; init; } = new();
        public List<Characteristic> Characteristics { get; init; } = new();
        public List<CharacteristicWorkCenter> CharacteristicWorkCenters { get; init; } = new();
        public List<DefectLocation> DefectLocations { get; init; } = new();
        public List<ControlPlan> ControlPlans { get; init; } = new();
        public List<Product> Products { get; init; } = new();
        public List<ProductPlant> ProductPlants { get; init; } = new();
        public List<Vendor> Vendors { get; init; } = new();
        public List<VendorPlant> VendorPlants { get; init; } = new();
        public List<Asset> Assets { get; init; } = new();
        public List<SerialNumber> QueueSerialNumbers { get; init; } = new();
        public List<MaterialQueueItem> MaterialQueueItems { get; init; } = new();
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
