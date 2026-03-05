using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class HeijunkaSchedulingService : IHeijunkaSchedulingService
{
    private readonly MesDbContext _db;
    private static readonly HashSet<string> AllowedBreakdownDimensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "TankSize",
        "TankType",
        "Color",
        "FinishedPartNumber"
    };
    private const string Unspecified = "UNSPECIFIED";

    public HeijunkaSchedulingService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IngestErpDemandResultDto> IngestErpDemandAsync(IngestErpDemandRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var result = new IngestErpDemandResultDto();
        if (request.Rows.Count == 0) return result;

        foreach (var row in request.Rows)
        {
            var exists = await _db.ErpSalesOrderDemandRows.AnyAsync(x =>
                x.ErpSalesOrderId == row.ErpSalesOrderId &&
                x.ErpSalesOrderLineId == row.ErpSalesOrderLineId &&
                x.SourceExtractedAtUtc == row.SourceExtractedAtUtc, ct);
            if (exists) continue;

            var raw = new ErpSalesOrderDemandRaw
            {
                Id = Guid.NewGuid(),
                ErpSalesOrderId = row.ErpSalesOrderId,
                ErpSalesOrderLineId = row.ErpSalesOrderLineId,
                ErpSkuCode = row.ErpSkuCode,
                SiteCode = row.SiteCode,
                ErpLoadNumberRaw = row.ErpLoadNumberRaw,
                DispatchDateLocal = row.DispatchDateLocal.Date,
                RequiredQty = row.RequiredQty,
                OrderStatus = row.OrderStatus,
                SourceExtractedAtUtc = row.SourceExtractedAtUtc,
                ErpLastChangedAtUtc = row.ErpLastChangedAtUtc,
                SourceBatchId = row.SourceBatchId,
                IngestedAtUtc = DateTime.UtcNow
            };
            _db.ErpSalesOrderDemandRows.Add(raw);
            result.RawRowsInserted++;

            var mapping = await ResolveMappingAsync(row.ErpSkuCode, row.SiteCode, ct);
            var resolvedPlanningGroupId = string.IsNullOrWhiteSpace(mapping?.MesPlanningGroupId)
                ? null
                : mapping!.MesPlanningGroupId;
            var snapshot = new ErpDemandSnapshot
            {
                Id = Guid.NewGuid(),
                SiteCode = row.SiteCode,
                ErpSalesOrderId = row.ErpSalesOrderId,
                ErpSalesOrderLineId = row.ErpSalesOrderLineId,
                ErpLoadNumberRaw = row.ErpLoadNumberRaw,
                LoadGroupId = NormalizeLoadGroupId(row.ErpLoadNumberRaw),
                LoadLegIndex = ParseLoadLegIndex(row.ErpLoadNumberRaw),
                DispatchDateLocal = row.DispatchDateLocal.Date,
                ProductId = null,
                ErpSkuCode = row.ErpSkuCode,
                MesPlanningGroupId = resolvedPlanningGroupId,
                RequiredQty = row.RequiredQty,
                OrderStatus = row.OrderStatus,
                ErpLastChangedAtUtc = row.ErpLastChangedAtUtc,
                CapturedAtUtc = DateTime.UtcNow
            };
            _db.ErpDemandSnapshots.Add(snapshot);
            result.SnapshotsCreated++;

            if (resolvedPlanningGroupId == null)
            {
                var existingOpen = await _db.UnmappedDemandExceptions.AnyAsync(x =>
                    x.ErpSkuCode == row.ErpSkuCode &&
                    x.SiteCode == row.SiteCode &&
                    x.LoadGroupId == snapshot.LoadGroupId &&
                    x.DispatchDateLocal == snapshot.DispatchDateLocal &&
                    x.ExceptionStatus == "Open", ct);
                if (!existingOpen)
                {
                    _db.UnmappedDemandExceptions.Add(new UnmappedDemandException
                    {
                        Id = Guid.NewGuid(),
                        ErpSkuCode = row.ErpSkuCode,
                        SiteCode = row.SiteCode,
                        LoadGroupId = snapshot.LoadGroupId,
                        DispatchDateLocal = snapshot.DispatchDateLocal,
                        RequiredQty = row.RequiredQty,
                        DetectedAtUtc = DateTime.UtcNow,
                        ExceptionStatus = "Open"
                    });
                    result.UnmappedExceptionsCreated++;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<ErpSkuMappingDto> UpsertSkuMappingAsync(UpsertErpSkuMappingRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var existing = await _db.ErpSkuPlanningGroupMappings
            .OrderByDescending(x => x.EffectiveFromUtc)
            .FirstOrDefaultAsync(x =>
                x.ErpSkuCode == request.ErpSkuCode &&
                x.SiteCode == request.SiteCode &&
                x.EffectiveFromUtc == request.EffectiveFromUtc, ct);

        if (existing == null)
        {
            existing = new ErpSkuPlanningGroupMapping
            {
                Id = Guid.NewGuid(),
                MappingOwnerUserId = actorUserId
            };
            _db.ErpSkuPlanningGroupMappings.Add(existing);
        }

        existing.ErpSkuCode = request.ErpSkuCode;
        existing.MesPlanningGroupId = request.MesPlanningGroupId;
        existing.SiteCode = request.SiteCode;
        existing.EffectiveFromUtc = request.EffectiveFromUtc;
        existing.EffectiveToUtc = request.EffectiveToUtc;
        existing.IsActive = request.IsActive;
        existing.RequiresReview = request.RequiresReview;
        existing.LastReviewedAtUtc = DateTime.UtcNow;
        existing.MappingOwnerUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return Map(existing);
    }

    public async Task<IReadOnlyList<ErpSkuMappingDto>> GetSkuMappingsAsync(string? siteCode, CancellationToken ct = default)
    {
        var query = _db.ErpSkuPlanningGroupMappings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(siteCode))
            query = query.Where(x => x.SiteCode == siteCode || x.SiteCode == null);
        return await query.OrderBy(x => x.ErpSkuCode).Select(x => Map(x)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkCenterBreakdownConfigDto>> GetWorkCenterBreakdownConfigsAsync(string siteCode, Guid productionLineId, CancellationToken ct = default)
    {
        return await _db.HeijunkaWorkCenterBreakdownConfigs
            .Where(x => x.SiteCode == siteCode && x.ProductionLineId == productionLineId)
            .Join(_db.WorkCenters, cfg => cfg.WorkCenterId, wc => wc.Id, (cfg, wc) => new { cfg, wc.Name })
            .OrderBy(x => x.Name)
            .Select(x => new WorkCenterBreakdownConfigDto
            {
                Id = x.cfg.Id,
                SiteCode = x.cfg.SiteCode,
                ProductionLineId = x.cfg.ProductionLineId,
                WorkCenterId = x.cfg.WorkCenterId,
                WorkCenterName = x.Name,
                GroupingDimensions = ParseDimensions(x.cfg.GroupingDimensionsJson)
            })
            .ToListAsync(ct);
    }

    public async Task<WorkCenterBreakdownConfigDto> UpsertWorkCenterBreakdownConfigAsync(UpsertWorkCenterBreakdownConfigRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var normalizedDimensions = NormalizeDimensions(request.GroupingDimensions);
        var workCenter = await _db.WorkCenters.FirstOrDefaultAsync(x => x.Id == request.WorkCenterId, ct)
            ?? throw new InvalidOperationException("Work center was not found.");

        var existing = await _db.HeijunkaWorkCenterBreakdownConfigs.FirstOrDefaultAsync(x =>
            x.SiteCode == request.SiteCode &&
            x.ProductionLineId == request.ProductionLineId &&
            x.WorkCenterId == request.WorkCenterId, ct);

        if (existing == null)
        {
            existing = new HeijunkaWorkCenterBreakdownConfig
            {
                Id = Guid.NewGuid(),
                SiteCode = request.SiteCode,
                ProductionLineId = request.ProductionLineId,
                WorkCenterId = request.WorkCenterId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = actorUserId
            };
            _db.HeijunkaWorkCenterBreakdownConfigs.Add(existing);
        }

        existing.GroupingDimensionsJson = JsonSerializer.Serialize(normalizedDimensions);
        existing.LastModifiedAtUtc = DateTime.UtcNow;
        existing.LastModifiedByUserId = actorUserId;

        await _db.SaveChangesAsync(ct);
        return new WorkCenterBreakdownConfigDto
        {
            Id = existing.Id,
            SiteCode = existing.SiteCode,
            ProductionLineId = existing.ProductionLineId,
            WorkCenterId = existing.WorkCenterId,
            WorkCenterName = workCenter.Name,
            GroupingDimensions = normalizedDimensions
        };
    }

    public async Task<WorkCenterScheduleBreakdownDto> GetWorkCenterScheduleBreakdownAsync(WorkCenterScheduleBreakdownRequestDto request, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.ScheduleId, ct)
            ?? throw new InvalidOperationException("Schedule was not found.");

        var workCenter = await _db.WorkCenters.FirstOrDefaultAsync(x => x.Id == request.WorkCenterId, ct)
            ?? throw new InvalidOperationException("Work center was not found.");

        var config = await _db.HeijunkaWorkCenterBreakdownConfigs.FirstOrDefaultAsync(x =>
            x.SiteCode == schedule.SiteCode &&
            x.ProductionLineId == schedule.ProductionLineId &&
            x.WorkCenterId == request.WorkCenterId, ct);

        var groupingDimensions = config == null
            ? []
            : NormalizeDimensions(ParseDimensions(config.GroupingDimensionsJson));

        var weekStart = schedule.WeekStartDateLocal.Date;
        var weekEnd = weekStart.AddDays(6);
        var lines = schedule.Lines
            .Where(x => x.PlannedDateLocal.Date >= weekStart && x.PlannedDateLocal.Date <= weekEnd)
            .ToList();

        if (lines.Count == 0)
        {
            return new WorkCenterScheduleBreakdownDto
            {
                ScheduleId = schedule.Id,
                SiteCode = schedule.SiteCode,
                ProductionLineId = schedule.ProductionLineId,
                WorkCenterId = request.WorkCenterId,
                WorkCenterName = workCenter.Name,
                WeekStartDateLocal = weekStart,
                GroupingDimensions = groupingDimensions
            };
        }

        var loadGroups = lines
            .Select(x => x.LoadGroupId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        var planningGroups = lines
            .Select(x => x.MesPlanningGroupId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        var snapshots = await _db.ErpDemandSnapshots
            .Where(x => x.SiteCode == schedule.SiteCode &&
                        x.DispatchDateLocal >= weekStart &&
                        x.DispatchDateLocal <= weekEnd &&
                        x.OrderStatus != "Cancelled" &&
                        loadGroups.Contains(x.LoadGroupId) &&
                        x.MesPlanningGroupId != null &&
                        planningGroups.Contains(x.MesPlanningGroupId))
            .ToListAsync(ct);

        var skuCodes = snapshots
            .Select(x => x.ErpSkuCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var products = await _db.Products
            .Where(x =>
                (x.SageItemNumber != null && skuCodes.Contains(x.SageItemNumber)) ||
                skuCodes.Contains(x.ProductNumber))
            .Select(x => new { x.SageItemNumber, x.ProductNumber, x.TankSize, x.TankType })
            .ToListAsync(ct);

        var productBySku = products
            .GroupBy(x => !string.IsNullOrWhiteSpace(x.SageItemNumber) ? x.SageItemNumber! : x.ProductNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var contributions = new List<(DateTime PlannedDate, decimal PlannedQty, string TankSize, string TankType, string Color, string FinishedPartNumber)>();
        foreach (var line in lines)
        {
            var lineSnapshots = snapshots
                .Where(x => string.Equals(x.LoadGroupId, line.LoadGroupId, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.MesPlanningGroupId, line.MesPlanningGroupId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (lineSnapshots.Count == 0)
            {
                contributions.Add((line.PlannedDateLocal.Date, line.PlannedQty, Unspecified, Unspecified, Unspecified, Unspecified));
                continue;
            }

            var totalRequired = lineSnapshots.Sum(x => x.RequiredQty);
            foreach (var snapshot in lineSnapshots)
            {
                var allocationWeight = totalRequired > 0m ? snapshot.RequiredQty / totalRequired : 1m / lineSnapshots.Count;
                var allocatedQty = line.PlannedQty * allocationWeight;
                var finishedPartNumber = string.IsNullOrWhiteSpace(snapshot.ErpSkuCode) ? Unspecified : snapshot.ErpSkuCode;
                var tankSize = Unspecified;
                var tankType = Unspecified;
                if (!string.IsNullOrWhiteSpace(snapshot.ErpSkuCode) && productBySku.TryGetValue(snapshot.ErpSkuCode, out var product))
                {
                    tankSize = product.TankSize.ToString();
                    tankType = string.IsNullOrWhiteSpace(product.TankType) ? Unspecified : product.TankType;
                }

                contributions.Add((line.PlannedDateLocal.Date, allocatedQty, tankSize, tankType, Unspecified, finishedPartNumber));
            }
        }

        var rows = contributions
            .GroupBy(x => new
            {
                x.PlannedDate,
                DimensionKey = BuildDimensionKey(groupingDimensions, x.TankSize, x.TankType, x.Color, x.FinishedPartNumber)
            })
            .Select(group => new WorkCenterScheduleBreakdownRowDto
            {
                PlannedDateLocal = group.Key.PlannedDate,
                PlannedQty = Math.Round(group.Sum(x => x.PlannedQty), 2),
                DimensionValues = BuildDimensionValues(groupingDimensions, group.First().TankSize, group.First().TankType, group.First().Color, group.First().FinishedPartNumber)
            })
            .OrderBy(x => x.PlannedDateLocal)
            .ThenBy(x => BuildDimensionSortKey(groupingDimensions, x.DimensionValues))
            .ToList();

        return new WorkCenterScheduleBreakdownDto
        {
            ScheduleId = schedule.Id,
            SiteCode = schedule.SiteCode,
            ProductionLineId = schedule.ProductionLineId,
            WorkCenterId = request.WorkCenterId,
            WorkCenterName = workCenter.Name,
            WeekStartDateLocal = weekStart,
            GroupingDimensions = groupingDimensions,
            Rows = rows
        };
    }

    public async Task<ScheduleDto> GenerateDraftAsync(GenerateScheduleDraftRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var weekStart = request.WeekStartDateLocal.Date;
        var weekEnd = weekStart.AddDays(6);

        var maxRevision = await _db.Schedules
            .Where(x => x.SiteCode == request.SiteCode &&
                        x.ProductionLineId == request.ProductionLineId &&
                        x.WeekStartDateLocal == weekStart)
            .Select(x => (int?)x.RevisionNumber)
            .MaxAsync(ct) ?? 0;

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            SiteCode = request.SiteCode,
            ProductionLineId = request.ProductionLineId,
            WeekStartDateLocal = weekStart,
            Status = "Draft",
            FreezeHours = request.FreezeHours,
            RevisionNumber = maxRevision + 1,
            CreatedByUserId = actorUserId,
            LastModifiedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        };
        _db.Schedules.Add(schedule);

        var demand = await _db.ErpDemandSnapshots
            .Where(x => x.SiteCode == request.SiteCode &&
                        x.DispatchDateLocal >= weekStart &&
                        x.DispatchDateLocal <= weekEnd &&
                        x.MesPlanningGroupId != null &&
                        x.OrderStatus != "Cancelled")
            .ToListAsync(ct);

        var grouped = demand
            .GroupBy(x => new { x.DispatchDateLocal, x.LoadGroupId, x.MesPlanningGroupId })
            .OrderBy(x => x.Key.DispatchDateLocal)
            .ThenBy(x => x.Key.LoadGroupId);

        var seq = 1;
        foreach (var group in grouped)
        {
            schedule.Lines.Add(new ScheduleLine
            {
                Id = Guid.NewGuid(),
                ScheduleId = schedule.Id,
                PlannedDateLocal = group.Key.DispatchDateLocal.Date,
                SequenceIndex = seq++,
                ProductId = null,
                PlanningClass = "Wheel",
                PlannedQty = group.Sum(x => x.RequiredQty),
                LoadGroupId = group.Key.LoadGroupId,
                DispatchDateLocal = group.Key.DispatchDateLocal.Date,
                MesPlanningGroupId = group.Key.MesPlanningGroupId,
                PlanningResourceId = request.PlanningResourceId,
                PolicySnapshotJson = JsonSerializer.Serialize(new
                {
                    Priority = "DispatchConstrained",
                    TieBreakers = new[] { "DispatchDate", "ShortestRiskWindow", "LowestChangeoverPenalty" }
                }),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = actorUserId
            });
        }

        _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            ScheduleLineId = null,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = actorUserId,
            ChangeReasonCode = "DraftGenerated",
            FieldName = "Status",
            FromValue = null,
            ToValue = "Draft"
        });

        await _db.SaveChangesAsync(ct);
        return await GetScheduleAsync(schedule.Id, ct) ?? throw new InvalidOperationException("Schedule generation failed.");
    }

    public async Task<ScheduleDto?> GetScheduleAsync(Guid scheduleId, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules
            .Include(x => x.Lines.OrderBy(l => l.SequenceIndex))
            .FirstOrDefaultAsync(x => x.Id == scheduleId, ct);
        return schedule == null ? null : Map(schedule);
    }

    public async Task<ScheduleDto?> PublishScheduleAsync(Guid scheduleId, Guid actorUserId, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == scheduleId, ct);
        if (schedule == null) return null;
        var nowUtc = DateTime.UtcNow;

        var openExceptions = await _db.UnmappedDemandExceptions.AnyAsync(x =>
            x.SiteCode == schedule.SiteCode &&
            x.ExceptionStatus == "Open" &&
            x.DispatchDateLocal >= schedule.WeekStartDateLocal &&
            x.DispatchDateLocal <= schedule.WeekStartDateLocal.AddDays(6), ct);
        if (openExceptions)
            throw new InvalidOperationException("Cannot publish schedule while open unmapped demand exceptions exist.");

        var currentlyPublished = await _db.Schedules
            .Where(x => x.Id != schedule.Id &&
                        x.SiteCode == schedule.SiteCode &&
                        x.ProductionLineId == schedule.ProductionLineId &&
                        x.WeekStartDateLocal == schedule.WeekStartDateLocal &&
                        x.Status == "Published")
            .ToListAsync(ct);

        foreach (var previous in currentlyPublished)
        {
            previous.Status = "Closed";
            previous.LastModifiedAtUtc = nowUtc;
            previous.LastModifiedByUserId = actorUserId;

            _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
            {
                Id = Guid.NewGuid(),
                ScheduleId = previous.Id,
                ChangedAtUtc = nowUtc,
                ChangedByUserId = actorUserId,
                ChangeReasonCode = "SupersededByPublish",
                FieldName = "Status",
                FromValue = "Published",
                ToValue = "Closed"
            });
        }

        var fromStatus = schedule.Status;
        schedule.Status = "Published";
        schedule.PublishedAtUtc = nowUtc;
        schedule.PublishedByUserId = actorUserId;
        schedule.LastModifiedAtUtc = nowUtc;
        schedule.LastModifiedByUserId = actorUserId;

        _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            ChangedAtUtc = nowUtc,
            ChangedByUserId = actorUserId,
            ChangeReasonCode = "Publish",
            FieldName = "Status",
            FromValue = fromStatus,
            ToValue = "Published"
        });
        await _db.SaveChangesAsync(ct);
        return Map(schedule);
    }

    public async Task<ScheduleDto?> CloseScheduleAsync(Guid scheduleId, Guid actorUserId, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(x => x.Id == scheduleId, ct);
        if (schedule == null) return null;
        var from = schedule.Status;
        schedule.Status = "Closed";
        schedule.LastModifiedAtUtc = DateTime.UtcNow;
        schedule.LastModifiedByUserId = actorUserId;
        _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = actorUserId,
            ChangeReasonCode = "CloseSchedule",
            FieldName = "Status",
            FromValue = from,
            ToValue = "Closed"
        });
        await _db.SaveChangesAsync(ct);
        return await GetScheduleAsync(scheduleId, ct);
    }

    public async Task<ScheduleDto?> ReopenScheduleAsync(Guid scheduleId, Guid actorUserId, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(x => x.Id == scheduleId, ct);
        if (schedule == null) return null;
        var from = schedule.Status;
        schedule.Status = "Draft";
        schedule.LastModifiedAtUtc = DateTime.UtcNow;
        schedule.LastModifiedByUserId = actorUserId;
        _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = actorUserId,
            ChangeReasonCode = "ReopenSchedule",
            FieldName = "Status",
            FromValue = from,
            ToValue = "Draft"
        });
        await _db.SaveChangesAsync(ct);
        return await GetScheduleAsync(scheduleId, ct);
    }

    public async Task<ScheduleDto?> ApplyFreezeOverrideAsync(FreezeOverrideRequestDto request, Guid actorUserId, decimal actorRoleTier, CancellationToken ct = default)
    {
        if (actorRoleTier > 4.0m)
            throw new InvalidOperationException("Supervisor role or above is required for freeze-window overrides.");

        var schedule = await _db.Schedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.ScheduleId, ct);
        if (schedule == null) return null;
        var line = schedule.Lines.FirstOrDefault(x => x.Id == request.ScheduleLineId);
        if (line == null) return null;
        if (string.IsNullOrWhiteSpace(request.ChangeReasonCode))
            throw new InvalidOperationException("ChangeReasonCode is required for freeze overrides.");

        var freezeBoundaryUtc = line.PlannedDateLocal.AddHours(-schedule.FreezeHours).ToUniversalTime();
        if (DateTime.UtcNow < freezeBoundaryUtc)
            throw new InvalidOperationException("Freeze override is only permitted inside the freeze window.");

        var oldDate = line.PlannedDateLocal;
        line.PlannedDateLocal = request.NewPlannedDateLocal.Date;
        _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            ScheduleLineId = line.Id,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = actorUserId,
            ChangeReasonCode = request.ChangeReasonCode,
            FieldName = "PlannedDateLocal",
            FromValue = oldDate.ToString("O"),
            ToValue = line.PlannedDateLocal.ToString("O")
        });

        if (request.NewPlannedQty.HasValue && request.NewPlannedQty.Value != line.PlannedQty)
        {
            var oldQty = line.PlannedQty;
            line.PlannedQty = request.NewPlannedQty.Value;
            _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
            {
                Id = Guid.NewGuid(),
                ScheduleId = schedule.Id,
                ScheduleLineId = line.Id,
                ChangedAtUtc = DateTime.UtcNow,
                ChangedByUserId = actorUserId,
                ChangeReasonCode = request.ChangeReasonCode,
                FieldName = "PlannedQty",
                FromValue = oldQty.ToString(),
                ToValue = line.PlannedQty.ToString()
            });
        }

        schedule.LastModifiedAtUtc = DateTime.UtcNow;
        schedule.LastModifiedByUserId = actorUserId;
        await _db.SaveChangesAsync(ct);
        return Map(schedule);
    }

    public async Task<ScheduleDto?> ReorderScheduleLineAsync(ReorderScheduleLineRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.ScheduleId, ct);
        if (schedule == null) return null;
        if (schedule.Status != "Draft")
            throw new InvalidOperationException("Only draft schedules can be resequenced.");
        if (string.IsNullOrWhiteSpace(request.ChangeReasonCode))
            throw new InvalidOperationException("ChangeReasonCode is required.");

        var target = schedule.Lines.FirstOrDefault(x => x.Id == request.ScheduleLineId);
        if (target == null) return null;
        var ordered = schedule.Lines
            .OrderBy(x => x.SequenceIndex ?? int.MaxValue)
            .ThenBy(x => x.PlannedDateLocal)
            .ToList();
        ordered.Remove(target);
        var insertIndex = Math.Clamp(request.NewSequenceIndex - 1, 0, ordered.Count);
        ordered.Insert(insertIndex, target);

        for (var i = 0; i < ordered.Count; i++)
        {
            var line = ordered[i];
            var newSeq = i + 1;
            if (line.SequenceIndex != newSeq)
            {
                _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
                {
                    Id = Guid.NewGuid(),
                    ScheduleId = schedule.Id,
                    ScheduleLineId = line.Id,
                    ChangedAtUtc = DateTime.UtcNow,
                    ChangedByUserId = actorUserId,
                    ChangeReasonCode = request.ChangeReasonCode,
                    FieldName = "SequenceIndex",
                    FromValue = line.SequenceIndex?.ToString(),
                    ToValue = newSeq.ToString()
                });
                line.SequenceIndex = newSeq;
            }
        }

        schedule.LastModifiedAtUtc = DateTime.UtcNow;
        schedule.LastModifiedByUserId = actorUserId;
        await _db.SaveChangesAsync(ct);
        return Map(schedule);
    }

    public async Task<ScheduleDto?> MoveScheduleLineAsync(MoveScheduleLineRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.ScheduleId, ct);
        if (schedule == null) return null;
        if (schedule.Status != "Draft")
            throw new InvalidOperationException("Only draft schedules can be moved.");
        if (string.IsNullOrWhiteSpace(request.ChangeReasonCode))
            throw new InvalidOperationException("ChangeReasonCode is required.");

        var target = schedule.Lines.FirstOrDefault(x => x.Id == request.ScheduleLineId);
        if (target == null) return null;

        var newDate = request.NewPlannedDateLocal.Date;
        if (target.PlannedDateLocal.Date != newDate)
        {
            var oldDate = target.PlannedDateLocal;
            target.PlannedDateLocal = newDate;
            _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
            {
                Id = Guid.NewGuid(),
                ScheduleId = schedule.Id,
                ScheduleLineId = target.Id,
                ChangedAtUtc = DateTime.UtcNow,
                ChangedByUserId = actorUserId,
                ChangeReasonCode = request.ChangeReasonCode,
                FieldName = "PlannedDateLocal",
                FromValue = oldDate.ToString("O"),
                ToValue = target.PlannedDateLocal.ToString("O")
            });
        }

        if (request.NewSequenceIndex.HasValue)
        {
            var ordered = schedule.Lines
                .OrderBy(x => x.SequenceIndex ?? int.MaxValue)
                .ThenBy(x => x.PlannedDateLocal)
                .ToList();
            ordered.Remove(target);
            var insertIndex = Math.Clamp(request.NewSequenceIndex.Value - 1, 0, ordered.Count);
            ordered.Insert(insertIndex, target);

            for (var i = 0; i < ordered.Count; i++)
            {
                var line = ordered[i];
                var newSeq = i + 1;
                if (line.SequenceIndex != newSeq)
                {
                    _db.ScheduleChangeLogs.Add(new ScheduleChangeLog
                    {
                        Id = Guid.NewGuid(),
                        ScheduleId = schedule.Id,
                        ScheduleLineId = line.Id,
                        ChangedAtUtc = DateTime.UtcNow,
                        ChangedByUserId = actorUserId,
                        ChangeReasonCode = request.ChangeReasonCode,
                        FieldName = "SequenceIndex",
                        FromValue = line.SequenceIndex?.ToString(),
                        ToValue = newSeq.ToString()
                    });
                    line.SequenceIndex = newSeq;
                }
            }
        }

        schedule.LastModifiedAtUtc = DateTime.UtcNow;
        schedule.LastModifiedByUserId = actorUserId;
        await _db.SaveChangesAsync(ct);
        return Map(schedule);
    }

    public async Task<IReadOnlyList<ScheduleChangeLogDto>> GetChangeHistoryAsync(Guid scheduleId, CancellationToken ct = default)
    {
        return await _db.ScheduleChangeLogs
            .Where(x => x.ScheduleId == scheduleId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .Select(x => new ScheduleChangeLogDto
            {
                Id = x.Id,
                ScheduleLineId = x.ScheduleLineId,
                ChangedAtUtc = x.ChangedAtUtc,
                ChangedByUserId = x.ChangedByUserId,
                ChangeReasonCode = x.ChangeReasonCode,
                FieldName = x.FieldName,
                FromValue = x.FromValue,
                ToValue = x.ToValue
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UnmappedDemandExceptionDto>> GetUnmappedExceptionsAsync(string siteCode, CancellationToken ct = default)
    {
        return await _db.UnmappedDemandExceptions
            .Where(x => x.SiteCode == siteCode)
            .OrderByDescending(x => x.DetectedAtUtc)
            .Select(x => new UnmappedDemandExceptionDto
            {
                Id = x.Id,
                ErpSkuCode = x.ErpSkuCode,
                SiteCode = x.SiteCode,
                LoadGroupId = x.LoadGroupId,
                DispatchDateLocal = x.DispatchDateLocal,
                RequiredQty = x.RequiredQty,
                DetectedAtUtc = x.DetectedAtUtc,
                ExceptionStatus = x.ExceptionStatus,
                ResolutionNotes = x.ResolutionNotes
            })
            .ToListAsync(ct);
    }

    public async Task<UnmappedDemandExceptionDto?> ResolveOrDeferExceptionAsync(ResolveUnmappedDemandExceptionRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await _db.UnmappedDemandExceptions.FirstOrDefaultAsync(x => x.Id == request.ExceptionId, ct);
        if (item == null) return null;
        item.ExceptionStatus = string.Equals(request.Action, "Defer", StringComparison.OrdinalIgnoreCase) ? "Deferred" : "Resolved";
        item.ResolutionNotes = request.ResolutionNotes;
        item.ResolvedByUserId = actorUserId;
        item.ResolvedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new UnmappedDemandExceptionDto
        {
            Id = item.Id,
            ErpSkuCode = item.ErpSkuCode,
            SiteCode = item.SiteCode,
            LoadGroupId = item.LoadGroupId,
            DispatchDateLocal = item.DispatchDateLocal,
            RequiredQty = item.RequiredQty,
            DetectedAtUtc = item.DetectedAtUtc,
            ExceptionStatus = item.ExceptionStatus,
            ResolutionNotes = item.ResolutionNotes
        };
    }

    public async Task<DispatchRiskSummaryDto> GetDispatchRiskSummaryAsync(string siteCode, Guid productionLineId, DateTime weekStartDateLocal, CancellationToken ct = default)
    {
        var start = weekStartDateLocal.Date;
        var end = start.AddDays(6);
        var dueLoadGroups = await _db.ErpDemandSnapshots
            .Where(x => x.SiteCode == siteCode && x.DispatchDateLocal >= start && x.DispatchDateLocal <= end)
            .Select(x => x.LoadGroupId)
            .Distinct()
            .CountAsync(ct);
        var plannedLoadGroups = await _db.ScheduleLines
            .Where(x => x.Schedule.SiteCode == siteCode &&
                        x.Schedule.ProductionLineId == productionLineId &&
                        x.Schedule.WeekStartDateLocal == start &&
                        x.LoadGroupId != null)
            .Select(x => x.LoadGroupId!)
            .Distinct()
            .CountAsync(ct);
        var openExceptions = await _db.UnmappedDemandExceptions.CountAsync(x =>
            x.SiteCode == siteCode &&
            x.ExceptionStatus == "Open" &&
            x.DispatchDateLocal >= start &&
            x.DispatchDateLocal <= end, ct);

        return new DispatchRiskSummaryDto
        {
            SiteCode = siteCode,
            ProductionLineId = productionLineId,
            WeekStartDateLocal = start,
            OpenUnmappedExceptions = openExceptions,
            LoadGroupsDue = dueLoadGroups,
            LoadGroupsPlanned = plannedLoadGroups
        };
    }

    public async Task<IReadOnlyList<DispatchWeekOrderCoverageDto>> GetDispatchWeekOrderCoverageAsync(string siteCode, Guid productionLineId, DateTime weekStartDateLocal, Guid scheduleId, CancellationToken ct = default)
    {
        var start = weekStartDateLocal.Date;
        var end = start.AddDays(6);
        var demand = await _db.ErpDemandSnapshots
            .Where(x => x.SiteCode == siteCode &&
                        x.DispatchDateLocal >= start &&
                        x.DispatchDateLocal <= end &&
                        x.OrderStatus != "Cancelled")
            .OrderBy(x => x.DispatchDateLocal)
            .ThenBy(x => x.LoadGroupId)
            .ThenBy(x => x.ErpSalesOrderId)
            .ThenBy(x => x.ErpSalesOrderLineId)
            .ToListAsync(ct);
        if (demand.Count == 0) return [];

        var requiredByLoadGroup = demand
            .GroupBy(x => x.LoadGroupId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.RequiredQty));

        var plannedByLoadGroup = await _db.ScheduleLines
            .Where(x => x.ScheduleId == scheduleId &&
                        x.Schedule.SiteCode == siteCode &&
                        x.Schedule.ProductionLineId == productionLineId &&
                        x.Schedule.WeekStartDateLocal == start &&
                        x.LoadGroupId != null)
            .GroupBy(x => x.LoadGroupId!)
            .Select(x => new { LoadGroupId = x.Key, PlannedQty = x.Sum(y => y.PlannedQty) })
            .ToDictionaryAsync(x => x.LoadGroupId, x => x.PlannedQty, ct);

        return demand.Select(x => new DispatchWeekOrderCoverageDto
        {
            SiteCode = siteCode,
            ProductionLineId = productionLineId,
            WeekStartDateLocal = start,
            LoadGroupId = x.LoadGroupId,
            DispatchDateLocal = x.DispatchDateLocal,
            ErpSalesOrderId = x.ErpSalesOrderId,
            ErpSalesOrderLineId = x.ErpSalesOrderLineId,
            ErpSkuCode = x.ErpSkuCode,
            MesPlanningGroupId = x.MesPlanningGroupId,
            RequiredQty = x.RequiredQty,
            LoadGroupRequiredQty = requiredByLoadGroup.GetValueOrDefault(x.LoadGroupId, 0m),
            LoadGroupPlannedQty = plannedByLoadGroup.GetValueOrDefault(x.LoadGroupId, 0m),
            IsMapped = !string.IsNullOrWhiteSpace(x.MesPlanningGroupId)
        }).ToList();
    }

    public async Task<IReadOnlyList<SupermarketQuantityStatusDto>> GetSupermarketQuantityStatusAsync(string siteCode, Guid productionLineId, DateTime weekStartDateLocal, CancellationToken ct = default)
    {
        var start = weekStartDateLocal.Date;
        var endExclusive = start.AddDays(7);
        var snapshots = await _db.SupermarketPositionSnapshots
            .Where(x => x.SiteCode == siteCode &&
                        x.ProductionLineId == productionLineId &&
                        x.CapturedAtUtc >= start &&
                        x.CapturedAtUtc < endExclusive)
            .OrderByDescending(x => x.CapturedAtUtc)
            .ToListAsync(ct);
        if (snapshots.Count == 0) return [];

        return snapshots
            .GroupBy(x => x.ProductId)
            .Select(group =>
            {
                var latest = group.OrderByDescending(x => x.CapturedAtUtc).First();
                var stockoutMinutes = group.Sum(x =>
                {
                    if (x.StockoutStartUtc == null || x.StockoutEndUtc == null) return 0m;
                    var minutes = (decimal)(x.StockoutEndUtc.Value - x.StockoutStartUtc.Value).TotalMinutes;
                    return minutes < 0 ? 0m : minutes;
                });

                return new SupermarketQuantityStatusDto
                {
                    SiteCode = siteCode,
                    ProductionLineId = productionLineId,
                    WeekStartDateLocal = start,
                    ProductId = latest.ProductId,
                    OnHandQty = latest.OnHandQty,
                    InTransitQty = latest.InTransitQty,
                    DemandQty = latest.DemandQty,
                    NetAvailableQty = latest.OnHandQty + latest.InTransitQty - latest.DemandQty,
                    StockoutDurationMinutes = stockoutMinutes,
                    HasOpenStockout = latest.StockoutStartUtc != null && latest.StockoutEndUtc == null,
                    LastCapturedAtUtc = latest.CapturedAtUtc
                };
            })
            .OrderBy(x => x.ProductId)
            .ToList();
    }

    public async Task<ScheduleExecutionEventDto> RecordFinalScanExecutionAsync(FinalScanExecutionRequestDto request, Guid actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("IdempotencyKey is required.");

        var existing = await _db.ScheduleExecutionEvents.FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing != null) return Map(existing);

        Guid? scheduleLineId = request.ScheduleLineId;
        if (!scheduleLineId.HasValue)
        {
            scheduleLineId = await _db.ScheduleLines
                .Where(x => x.Schedule.SiteCode == request.SiteCode &&
                            x.Schedule.ProductionLineId == request.ProductionLineId &&
                            x.PlannedDateLocal == request.ExecutionDateLocal.Date &&
                            (request.MesPlanningGroupId == null || x.MesPlanningGroupId == request.MesPlanningGroupId))
                .OrderBy(x => x.SequenceIndex)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);
        }

        var created = new ScheduleExecutionEvent
        {
            Id = Guid.NewGuid(),
            SiteCode = request.SiteCode,
            ProductionLineId = request.ProductionLineId,
            ExecutionResourceId = request.ExecutionResourceId,
            ProductId = request.ProductId,
            MesPlanningGroupId = request.MesPlanningGroupId,
            ExecutionDateLocal = request.ExecutionDateLocal.Date,
            ActualQty = request.ActualQty,
            ScheduleLineId = scheduleLineId,
            ExecutionState = request.ExecutionState,
            ShortfallReasonCode = request.ShortfallReasonCode,
            RecordedAtUtc = DateTime.UtcNow,
            RecordedByUserId = actorUserId,
            IdempotencyKey = request.IdempotencyKey
        };
        _db.ScheduleExecutionEvents.Add(created);
        await _db.SaveChangesAsync(ct);
        return Map(created);
    }

    public async Task<HeijunkaKpiResponseDto> GetPhase1KpisAsync(string siteCode, Guid productionLineId, DateTime fromDateLocal, DateTime toDateLocal, CancellationToken ct = default)
    {
        var from = fromDateLocal.Date;
        var to = toDateLocal.Date;
        var response = new HeijunkaKpiResponseDto
        {
            SiteCode = siteCode,
            ProductionLineId = productionLineId,
            FromDateLocal = from,
            ToDateLocal = to
        };

        var lines = await _db.ScheduleLines
            .Where(x => x.Schedule.SiteCode == siteCode &&
                        x.Schedule.ProductionLineId == productionLineId &&
                        x.PlannedDateLocal >= from &&
                        x.PlannedDateLocal <= to &&
                        x.Schedule.Status != "Draft")
            .ToListAsync(ct);
        if (lines.Count == 0)
        {
            response.IsEligible = false;
            response.EligibilityReason = "NoPublishedSchedule";
            response.ScheduleAdherencePercent.NullReasonCode = "NoPublishedSchedule";
            response.PlanAttainmentPercent.NullReasonCode = "NoPublishedSchedule";
            response.LoadReadinessPercent.NullReasonCode = "NoLoadGroupsDue";
            response.SupermarketStockoutDurationMinutes.NullReasonCode = "NoDemand";
            return response;
        }

        var lineIds = lines.Select(x => x.Id).ToHashSet();
        var execution = await _db.ScheduleExecutionEvents
            .Where(x => x.ScheduleLineId != null && lineIds.Contains(x.ScheduleLineId.Value))
            .ToListAsync(ct);
        var coverage = lines.Count == 0 ? 0 : (decimal)execution.Select(x => x.ScheduleLineId).Distinct().Count() / lines.Count;
        response.IsEligible = coverage >= 0.95m;
        response.EligibilityReason = response.IsEligible ? null : "InsufficientExecutionData";

        if (response.IsEligible)
        {
            var onPlan = execution.Count(x =>
            {
                var line = lines.FirstOrDefault(l => l.Id == x.ScheduleLineId);
                return line != null && x.ExecutionDateLocal == line.PlannedDateLocal;
            });
            response.ScheduleAdherencePercent.Value = lines.Count == 0 ? null : Math.Round(onPlan * 100m / lines.Count, 2);

            var totalPlannedQty = lines.Sum(x => x.PlannedQty);
            if (totalPlannedQty <= 0)
            {
                response.PlanAttainmentPercent.NullReasonCode = "NoPlannedQty";
            }
            else
            {
                response.PlanAttainmentPercent.Value = Math.Round(execution.Sum(x => x.ActualQty) * 100m / totalPlannedQty, 2);
            }
        }
        else
        {
            response.ScheduleAdherencePercent.NullReasonCode = "InsufficientExecutionData";
            response.PlanAttainmentPercent.NullReasonCode = "InsufficientExecutionData";
        }

        var loadGroupLines = lines.Where(x => !string.IsNullOrWhiteSpace(x.LoadGroupId)).ToList();
        if (loadGroupLines.Count == 0)
        {
            response.LoadReadinessPercent.NullReasonCode = "NoLoadGroupsDue";
        }
        else
        {
            var readyGroups = loadGroupLines
                .GroupBy(x => x.LoadGroupId!)
                .Count(g => g.All(line =>
                    execution.Any(e => e.ScheduleLineId == line.Id && e.ExecutionDateLocal <= (line.DispatchDateLocal ?? line.PlannedDateLocal))));
            response.LoadReadinessPercent.Value = Math.Round(readyGroups * 100m / loadGroupLines.Select(x => x.LoadGroupId).Distinct().Count(), 2);
        }

        var stockouts = await _db.SupermarketPositionSnapshots
            .Where(x => x.SiteCode == siteCode &&
                        x.ProductionLineId == productionLineId &&
                        x.CapturedAtUtc >= from &&
                        x.CapturedAtUtc < to.AddDays(1))
            .Where(x => x.StockoutStartUtc != null && x.StockoutEndUtc != null)
            .ToListAsync(ct);
        if (stockouts.Count == 0)
        {
            response.SupermarketStockoutDurationMinutes.Value = 0m;
        }
        else
        {
            response.SupermarketStockoutDurationMinutes.Value = stockouts.Sum(x =>
                (decimal)((x.StockoutEndUtc!.Value - x.StockoutStartUtc!.Value).TotalMinutes < 0
                    ? 0
                    : (x.StockoutEndUtc.Value - x.StockoutStartUtc.Value).TotalMinutes));
        }

        return response;
    }

    private static List<string> ParseDimensions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<string> NormalizeDimensions(IEnumerable<string>? dimensions)
    {
        if (dimensions == null) return [];
        var normalized = new List<string>();
        foreach (var dimension in dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimension)) continue;
            var canonical = AllowedBreakdownDimensions.FirstOrDefault(x => string.Equals(x, dimension.Trim(), StringComparison.OrdinalIgnoreCase));
            if (canonical == null)
                throw new InvalidOperationException($"Unsupported grouping dimension: {dimension}");
            if (!normalized.Contains(canonical, StringComparer.OrdinalIgnoreCase))
                normalized.Add(canonical);
        }
        return normalized;
    }

    private static string BuildDimensionKey(IReadOnlyList<string> groupingDimensions, string tankSize, string tankType, string color, string finishedPartNumber)
        => string.Join("|", groupingDimensions.Select(dim => GetDimensionValue(dim, tankSize, tankType, color, finishedPartNumber)));

    private static Dictionary<string, string> BuildDimensionValues(IReadOnlyList<string> groupingDimensions, string tankSize, string tankType, string color, string finishedPartNumber)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dim in groupingDimensions)
            values[dim] = GetDimensionValue(dim, tankSize, tankType, color, finishedPartNumber);
        return values;
    }

    private static string BuildDimensionSortKey(IReadOnlyList<string> groupingDimensions, IReadOnlyDictionary<string, string> values)
        => string.Join("|", groupingDimensions.Select(dim => values.TryGetValue(dim, out var val) ? val : Unspecified));

    private static string GetDimensionValue(string dimension, string tankSize, string tankType, string color, string finishedPartNumber)
    {
        return dimension switch
        {
            "TankSize" => string.IsNullOrWhiteSpace(tankSize) ? Unspecified : tankSize,
            "TankType" => string.IsNullOrWhiteSpace(tankType) ? Unspecified : tankType,
            "Color" => string.IsNullOrWhiteSpace(color) ? Unspecified : color,
            "FinishedPartNumber" => string.IsNullOrWhiteSpace(finishedPartNumber) ? Unspecified : finishedPartNumber,
            _ => Unspecified
        };
    }

    private async Task<ErpSkuPlanningGroupMapping?> ResolveMappingAsync(string erpSkuCode, string siteCode, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.ErpSkuPlanningGroupMappings
            .Where(x => x.ErpSkuCode == erpSkuCode &&
                        x.IsActive &&
                        (x.SiteCode == siteCode || x.SiteCode == null) &&
                        x.EffectiveFromUtc <= now &&
                        (x.EffectiveToUtc == null || x.EffectiveToUtc >= now))
            .OrderByDescending(x => x.SiteCode == siteCode)
            .ThenByDescending(x => x.EffectiveFromUtc)
            .FirstOrDefaultAsync(ct);
    }

    private static string NormalizeLoadGroupId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "UNKNOWN";
        var idx = raw.LastIndexOf('-');
        return idx <= 0 ? raw.Trim().ToUpperInvariant() : raw[..idx].Trim().ToUpperInvariant();
    }

    private static int ParseLoadLegIndex(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        var idx = raw.LastIndexOf('-');
        if (idx < 0 || idx == raw.Length - 1) return 0;
        return int.TryParse(raw[(idx + 1)..], out var leg) ? leg : 0;
    }

    private static ScheduleExecutionEventDto Map(ScheduleExecutionEvent x) => new()
    {
        Id = x.Id,
        ScheduleLineId = x.ScheduleLineId,
        ExecutionState = x.ExecutionState,
        ActualQty = x.ActualQty,
        ExecutionDateLocal = x.ExecutionDateLocal,
        IdempotencyKey = x.IdempotencyKey
    };

    private static ErpSkuMappingDto Map(ErpSkuPlanningGroupMapping x) => new()
    {
        Id = x.Id,
        ErpSkuCode = x.ErpSkuCode,
        MesPlanningGroupId = x.MesPlanningGroupId,
        SiteCode = x.SiteCode,
        EffectiveFromUtc = x.EffectiveFromUtc,
        EffectiveToUtc = x.EffectiveToUtc,
        IsActive = x.IsActive,
        MappingOwnerUserId = x.MappingOwnerUserId,
        LastReviewedAtUtc = x.LastReviewedAtUtc,
        RequiresReview = x.RequiresReview
    };

    private static ScheduleDto Map(Schedule x) => new()
    {
        Id = x.Id,
        SiteCode = x.SiteCode,
        ProductionLineId = x.ProductionLineId,
        WeekStartDateLocal = x.WeekStartDateLocal,
        Status = x.Status,
        PublishedAtUtc = x.PublishedAtUtc,
        PublishedByUserId = x.PublishedByUserId,
        FreezeHours = x.FreezeHours,
        RevisionNumber = x.RevisionNumber,
        Lines = x.Lines
            .OrderBy(l => l.SequenceIndex)
            .ThenBy(l => l.PlannedDateLocal)
            .Select(l => new ScheduleLineDto
            {
                Id = l.Id,
                PlannedDateLocal = l.PlannedDateLocal,
                SequenceIndex = l.SequenceIndex,
                ProductId = l.ProductId,
                PlanningClass = l.PlanningClass,
                PlannedQty = l.PlannedQty,
                LoadGroupId = l.LoadGroupId,
                DispatchDateLocal = l.DispatchDateLocal,
                MesPlanningGroupId = l.MesPlanningGroupId,
                PlanningResourceId = l.PlanningResourceId,
                ExecutionResourceId = l.ExecutionResourceId
            })
            .ToList()
    };
}
