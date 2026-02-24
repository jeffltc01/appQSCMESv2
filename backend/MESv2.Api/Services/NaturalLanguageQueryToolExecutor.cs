using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NaturalLanguageQueryToolExecutor : INlqToolExecutor
{
    private readonly MesDbContext _db;
    private readonly ISupervisorDashboardService _supervisorDashboardService;
    private readonly IDigitalTwinService _digitalTwinService;

    public NaturalLanguageQueryToolExecutor(
        MesDbContext db,
        ISupervisorDashboardService supervisorDashboardService,
        IDigitalTwinService digitalTwinService)
    {
        _db = db;
        _supervisorDashboardService = supervisorDashboardService;
        _digitalTwinService = digitalTwinService;
    }

    public async Task<NaturalLanguageQueryResponseDto> ExecuteAsync(
        NlqIntent intent,
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        CancellationToken cancellationToken = default)
    {
        var (dateText, dateValue) = ResolveDate(request.Context?.Date);

        return intent switch
        {
            NlqIntent.TanksProducedToday => await BuildTanksProducedTodayAsync(request, plantId, dateText, cancellationToken),
            NlqIntent.WorkCentersBehindTargetToday => await BuildWorkCentersBehindTargetAsync(plantId, dateText, cancellationToken),
            NlqIntent.TopDowntimeDriversToday => await BuildTopDowntimeDriversAsync(plantId, dateValue, cancellationToken),
            NlqIntent.WorkCenterPerformanceSummary => await BuildWorkCenterPerformanceSummaryAsync(request, plantId, dateText, cancellationToken),
            NlqIntent.DefectHotspotsToday => await BuildDefectHotspotsTodayAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.FirstPassYieldTrend => await BuildFirstPassYieldTrendAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.OperatorOutlierPerformance => await BuildOperatorOutlierPerformanceAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.DowntimeByAsset => await BuildDowntimeByAssetAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.DowntimeUncodedEvents => await BuildDowntimeUncodedEventsAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.BottleneckWorkCenterNow => await BuildBottleneckWorkCenterNowAsync(request, plantId, cancellationToken),
            NlqIntent.QueueBacklogRisk => await BuildQueueBacklogRiskAsync(request, plantId, cancellationToken),
            NlqIntent.TargetAtRiskByShift => await BuildTargetAtRiskByShiftAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.CycleTimeAnomalies => await BuildCycleTimeAnomaliesAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.AnnotationFollowUpNeeded => await BuildAnnotationFollowUpNeededAsync(request, plantId, dateValue, cancellationToken),
            NlqIntent.QualityVsThroughputTradeoff => await BuildQualityVsThroughputTradeoffAsync(request, plantId, dateText, cancellationToken),
            NlqIntent.CurrentScreenFilteredRecordCount => BuildCurrentScreenFilteredRecordCount(request),
            _ => BuildUnknownIntentResponse(),
        };
    }

    private static NaturalLanguageQueryResponseDto BuildCurrentScreenFilteredRecordCount(NaturalLanguageQueryRequestDto request)
    {
        var count = request.Context?.ActiveFilterTotalCount;
        var screenKey = request.Context?.ScreenKey ?? "current-screen";

        if (count.HasValue && count.Value >= 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = $"{count.Value} records currently match your active filters on {screenKey}.",
                ScopeUsed = "context",
                Confidence = 0.95m,
                DataPoints =
                [
                    new() { Label = "Screen", Value = screenKey },
                    new() { Label = "Filtered record total", Value = count.Value.ToString(), Unit = "records" },
                    new() { Label = "Filter summary", Value = request.Context?.FilterSummary ?? "not provided" },
                ],
            };
        }

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = "I need list/table filter context to answer that count question on this screen.",
            ScopeUsed = "context",
            Confidence = 0.4m,
            DataPoints =
            [
                new() { Label = "Tip", Value = "Use Ask MES from a list/table screen with active filters." }
            ],
        };
    }

    private NaturalLanguageQueryResponseDto BuildUnknownIntentResponse()
    {
        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = "I could not map that question to a supported metric intent yet.",
            ScopeUsed = "unknown",
            Confidence = 0.35m,
            DataPoints =
            [
                new NaturalLanguageQueryDataPointDto
                {
                    Label = "Supported intents",
                    Value = "tanks produced, behind target, downtime, performance, defect hotspots, FPY trend, operator outliers, downtime by asset, uncoded downtime, bottleneck now, queue backlog risk, target-at-risk by shift, cycle-time anomalies, annotation follow-up, quality vs throughput, current-screen filtered count",
                }
            ],
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildTanksProducedTodayAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        string date,
        CancellationToken ct)
    {
        if (request.Context?.WorkCenterId is Guid wcId)
        {
            var metrics = await _supervisorDashboardService.GetMetricsAsync(
                wcId, plantId, date, request.Context.OperatorId, ct);

            var wcName = await _db.WorkCenters
                .Where(w => w.Id == wcId)
                .Select(w => w.Name)
                .FirstOrDefaultAsync(ct) ?? "Work Center";

            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = $"{wcName} has produced {metrics.DayCount} tanks today.",
                ScopeUsed = "context",
                Confidence = 0.9m,
                DataPoints =
                [
                    new() { Label = "Work center", Value = wcName },
                    new() { Label = "Tanks produced today", Value = metrics.DayCount.ToString() },
                    new() { Label = "Week-to-date", Value = metrics.WeekCount.ToString() },
                ],
            };
        }

        var wcIds = await _db.WorkCenterProductionLines
            .Where(x => x.ProductionLine.PlantId == plantId)
            .Select(x => x.WorkCenterId)
            .Distinct()
            .ToListAsync(ct);

        var rows = new List<(Guid wcId, string wcName, int dayCount)>();
        foreach (var wcIdInPlant in wcIds)
        {
            var metrics = await _supervisorDashboardService.GetMetricsAsync(
                wcIdInPlant, plantId, date, request.Context?.OperatorId, ct);
            if (metrics.DayCount <= 0)
                continue;
            var wcName = await _db.WorkCenters
                .Where(w => w.Id == wcIdInPlant)
                .Select(w => w.Name)
                .FirstOrDefaultAsync(ct) ?? wcIdInPlant.ToString("N")[..8];
            rows.Add((wcIdInPlant, wcName, metrics.DayCount));
        }

        var total = rows.Sum(x => x.dayCount);
        var topWc = rows.OrderByDescending(x => x.dayCount).Take(3).ToList();
        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"This plant has produced {total} tanks today across {rows.Count} active work centers.",
            ScopeUsed = "plant-wide",
            Confidence = 0.88m,
            DataPoints = topWc
                .Select(x => new NaturalLanguageQueryDataPointDto
                {
                    Label = x.wcName,
                    Value = x.dayCount.ToString(),
                    Unit = "tanks"
                })
                .Prepend(new NaturalLanguageQueryDataPointDto
                {
                    Label = "Plant total today",
                    Value = total.ToString(),
                    Unit = "tanks"
                })
                .ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildWorkCentersBehindTargetAsync(
        Guid plantId,
        string date,
        CancellationToken ct)
    {
        var wcIds = await _db.WorkCenterProductionLines
            .Where(x => x.ProductionLine.PlantId == plantId)
            .Select(x => x.WorkCenterId)
            .Distinct()
            .ToListAsync(ct);

        var behind = new List<(string wcName, decimal delta, int actual, decimal planned, decimal downtime)>();
        foreach (var wcId in wcIds)
        {
            var table = await _supervisorDashboardService.GetPerformanceTableAsync(
                wcId, plantId, date, "day", null, ct);
            var total = table.TotalRow;
            if (total?.Planned is null || total.Delta is null || total.Delta >= 0)
                continue;

            var wcName = await _db.WorkCenters
                .Where(w => w.Id == wcId)
                .Select(w => w.Name)
                .FirstOrDefaultAsync(ct) ?? wcId.ToString("N")[..8];

            behind.Add((wcName, total.Delta.Value, total.Actual, total.Planned.Value, total.DowntimeMinutes));
        }

        if (behind.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No work centers are currently behind their planned output today.",
                ScopeUsed = "plant-wide",
                Confidence = 0.85m,
            };
        }

        var ranked = behind.OrderBy(x => x.delta).Take(5).ToList();
        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"{behind.Count} work centers are behind target today. The largest shortfalls are listed below.",
            ScopeUsed = "plant-wide",
            Confidence = 0.86m,
            DataPoints = ranked.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.wcName,
                Value = $"{x.actual}/{x.planned} (delta {x.delta})",
                Unit = $"downtime {x.downtime}m",
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildTopDowntimeDriversAsync(
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var grouped = await _db.DowntimeEvents
            .Where(e => e.WorkCenterProductionLine.ProductionLine.PlantId == plantId
                        && e.StartedAt >= start
                        && e.StartedAt < end
                        && (e.DowntimeReasonId == null || e.DowntimeReason!.CountsAsDowntime))
            .GroupBy(e => e.DowntimeReason != null ? e.DowntimeReason.Name : "Unknown reason")
            .Select(g => new
            {
                Reason = g.Key,
                Minutes = g.Sum(x => x.DurationMinutes),
                Events = g.Count(),
            })
            .OrderByDescending(x => x.Minutes)
            .Take(5)
            .ToListAsync(ct);

        if (grouped.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No downtime events were logged for today in this plant.",
                ScopeUsed = "plant-wide",
                Confidence = 0.8m,
            };
        }

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = "These are the top downtime drivers for today.",
            ScopeUsed = "plant-wide",
            Confidence = 0.84m,
            DataPoints = grouped.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.Reason,
                Value = Math.Round(x.Minutes, 1).ToString(),
                Unit = $"{x.Events} events",
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildWorkCenterPerformanceSummaryAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        string date,
        CancellationToken ct)
    {
        if (request.Context?.WorkCenterId is not Guid wcId)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Select a work center for a performance summary.",
                ScopeUsed = "context",
                Confidence = 0.5m,
            };
        }

        var metrics = await _supervisorDashboardService.GetMetricsAsync(
            wcId, plantId, date, request.Context.OperatorId, ct);
        var table = await _supervisorDashboardService.GetPerformanceTableAsync(
            wcId, plantId, date, request.Context?.View ?? "day", request.Context?.OperatorId, ct);

        var wcName = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(ct) ?? "Work Center";
        var total = table.TotalRow;
        var deltaText = total?.Delta is null ? "n/a" : total.Delta.Value.ToString("0.##");

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"{wcName} today: {metrics.DayCount} units, qty/hour {metrics.DayQtyPerHour}, delta {deltaText}.",
            ScopeUsed = "context",
            Confidence = 0.87m,
            DataPoints =
            [
                new() { Label = "Day count", Value = metrics.DayCount.ToString() },
                new() { Label = "Week count", Value = metrics.WeekCount.ToString() },
                new() { Label = "Qty per hour", Value = metrics.DayQtyPerHour.ToString("0.##") },
                new() { Label = "Total planned", Value = total?.Planned?.ToString("0.##") ?? "--" },
                new() { Label = "Total actual", Value = total?.Actual.ToString() ?? "--" },
                new() { Label = "Total delta", Value = deltaText },
            ],
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildDefectHotspotsTodayAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var wcId = request.Context?.WorkCenterId;

        var query = _db.DefectLogs
            .Where(d => d.Timestamp >= start && d.Timestamp < end
                        && d.ProductionRecord != null
                        && d.ProductionRecord.ProductionLine.PlantId == plantId);
        if (wcId.HasValue)
            query = query.Where(d => d.ProductionRecord!.WorkCenterId == wcId.Value);

        var byCode = await query
            .GroupBy(d => d.DefectCode.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        var byLocation = await query
            .GroupBy(d => d.Location.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .ToListAsync(ct);

        if (byCode.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No defects were logged for the selected scope today.",
                ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
                Confidence = 0.8m,
            };
        }

        var points = byCode.Select(x => new NaturalLanguageQueryDataPointDto
        {
            Label = $"Defect: {x.Name}",
            Value = x.Count.ToString(),
            Unit = "events"
        }).ToList();
        points.AddRange(byLocation.Select(x => new NaturalLanguageQueryDataPointDto
        {
            Label = $"Location: {x.Name}",
            Value = x.Count.ToString(),
            Unit = "events"
        }));

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = "These are the highest defect hotspots for today.",
            ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
            Confidence = 0.87m,
            DataPoints = points,
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildFirstPassYieldTrendAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        if (request.Context?.WorkCenterId is not Guid wcId)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Select a work center to analyze FPY trend.",
                ScopeUsed = "context",
                Confidence = 0.45m,
            };
        }

        var weekStart = localDate.Date.AddDays(-(int)localDate.DayOfWeek + (localDate.DayOfWeek == DayOfWeek.Sunday ? -6 : 1));
        var rows = new List<(string day, decimal? fpy)>();
        for (var i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var table = await _supervisorDashboardService.GetPerformanceTableAsync(
                wcId, plantId, day.ToString("yyyy-MM-dd"), "day", request.Context?.OperatorId, ct);
            rows.Add((day.ToString("ddd"), table.TotalRow?.Fpy));
        }

        var valid = rows.Where(x => x.fpy.HasValue).ToList();
        var trend = valid.Count >= 2 ? valid.Last().fpy!.Value - valid.First().fpy!.Value : 0m;
        var trendText = valid.Count < 2
            ? "insufficient data"
            : trend >= 0m ? $"improving by {Math.Round(trend, 1)} pts" : $"down by {Math.Round(Math.Abs(trend), 1)} pts";

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"FPY this week is {trendText}.",
            ScopeUsed = "context",
            Confidence = 0.86m,
            DataPoints = rows.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.day,
                Value = x.fpy?.ToString("0.0") ?? "--",
                Unit = "%"
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildOperatorOutlierPerformanceAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        if (request.Context?.WorkCenterId is not Guid wcId)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Select a work center to compare operator performance.",
                ScopeUsed = "context",
                Confidence = 0.45m,
            };
        }

        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var byOperator = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= start && r.Timestamp < end)
            .GroupBy(r => new { r.OperatorId, r.Operator.DisplayName })
            .Select(g => new
            {
                g.Key.DisplayName,
                Count = g.Count(),
                First = g.Min(x => x.Timestamp),
                Last = g.Max(x => x.Timestamp),
            })
            .ToListAsync(ct);

        if (byOperator.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No operator production data exists for this scope today.",
                ScopeUsed = "context",
                Confidence = 0.75m,
            };
        }

        var operatorStats = byOperator.Select(x =>
        {
            var hours = Math.Max((decimal)(x.Last - x.First).TotalHours, 1m / 60m);
            var uph = Math.Round(x.Count / hours, 2);
            return new { x.DisplayName, x.Count, Uph = uph };
        }).OrderByDescending(x => x.Uph).ToList();

        var avgUph = operatorStats.Average(x => x.Uph);
        var top = operatorStats.First();
        var bottom = operatorStats.Last();

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"Top operator is {top.DisplayName} ({top.Uph}/hr) and lowest is {bottom.DisplayName} ({bottom.Uph}/hr).",
            ScopeUsed = "context",
            Confidence = 0.84m,
            DataPoints =
            [
                new() { Label = "Average qty/hour", Value = avgUph.ToString("0.##") },
                new() { Label = $"Top: {top.DisplayName}", Value = top.Uph.ToString("0.##"), Unit = "units/hr" },
                new() { Label = $"Bottom: {bottom.DisplayName}", Value = bottom.Uph.ToString("0.##"), Unit = "units/hr" },
            ],
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildDowntimeByAssetAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var wcId = request.Context?.WorkCenterId;

        var query = _db.DowntimeEvents
            .Where(e => e.WorkCenterProductionLine.ProductionLine.PlantId == plantId
                        && e.StartedAt >= start && e.StartedAt < end);
        if (wcId.HasValue)
            query = query.Where(e => e.WorkCenterProductionLine.WorkCenterId == wcId.Value);

        var grouped = await query
            .GroupBy(e => new
            {
                WorkCenterName = e.WorkCenterProductionLine.WorkCenter.Name,
                LineName = e.WorkCenterProductionLine.ProductionLine.Name
            })
            .Select(g => new
            {
                Label = $"{g.Key.WorkCenterName} / {g.Key.LineName}",
                Minutes = g.Sum(x => x.DurationMinutes),
                Events = g.Count()
            })
            .OrderByDescending(x => x.Minutes)
            .Take(5)
            .ToListAsync(ct);

        if (grouped.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No downtime events were logged for assets/stations in this scope today.",
                ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
                Confidence = 0.78m,
            };
        }

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = "These assets/stations have the highest downtime today.",
            ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
            Confidence = 0.84m,
            DataPoints = grouped.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.Label,
                Value = Math.Round(x.Minutes, 1).ToString(),
                Unit = $"{x.Events} events"
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildDowntimeUncodedEventsAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var wcId = request.Context?.WorkCenterId;
        var query = _db.DowntimeEvents
            .Where(e => e.WorkCenterProductionLine.ProductionLine.PlantId == plantId
                        && e.StartedAt >= start && e.StartedAt < end
                        && e.DowntimeReasonId == null);
        if (wcId.HasValue)
            query = query.Where(e => e.WorkCenterProductionLine.WorkCenterId == wcId.Value);

        var grouped = await query
            .GroupBy(e => e.WorkCenterProductionLine.WorkCenter.Name)
            .Select(g => new { WorkCenter = g.Key, Events = g.Count(), Minutes = g.Sum(x => x.DurationMinutes) })
            .OrderByDescending(x => x.Minutes)
            .Take(5)
            .ToListAsync(ct);

        if (grouped.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No uncoded downtime events were found for this scope today.",
                ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
                Confidence = 0.84m,
            };
        }

        var totalEvents = grouped.Sum(x => x.Events);
        var totalMinutes = grouped.Sum(x => x.Minutes);
        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"There are {totalEvents} uncoded downtime events totaling {Math.Round(totalMinutes, 1)} minutes.",
            ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
            Confidence = 0.88m,
            DataPoints = grouped.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.WorkCenter,
                Value = Math.Round(x.Minutes, 1).ToString(),
                Unit = $"{x.Events} events"
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildBottleneckWorkCenterNowAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        CancellationToken ct)
    {
        if (request.Context?.ProductionLineId is not Guid lineId)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Select a production line (Digital Twin context) to detect bottleneck now.",
                ScopeUsed = "context",
                Confidence = 0.45m,
            };
        }

        var snapshot = await _digitalTwinService.GetSnapshotAsync(plantId, lineId, ct);
        var bottleneck = snapshot.Stations.FirstOrDefault(s => s.IsBottleneck);
        if (bottleneck == null)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No active bottleneck is currently detected for this production line.",
                ScopeUsed = "context",
                Confidence = 0.82m,
            };
        }

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"{bottleneck.Name} is the current bottleneck based on the Digital Twin WIP rule.",
            ScopeUsed = "context",
            Confidence = 0.9m,
            DataPoints =
            [
                new() { Label = "Station", Value = bottleneck.Name },
                new() { Label = "WIP", Value = bottleneck.WipCount.ToString(), Unit = "units" },
                new() { Label = "Status", Value = bottleneck.Status },
            ],
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildQueueBacklogRiskAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        CancellationToken ct)
    {
        var lineId = request.Context?.ProductionLineId;
        var queued = _db.MaterialQueueItems
            .Where(m => m.Status == "queued" || m.Status == "active");
        if (lineId.HasValue)
            queued = queued.Where(m => m.ProductionLineId == lineId.Value);
        else
            queued = queued.Where(m => m.ProductionLine != null && m.ProductionLine.PlantId == plantId);

        var grouped = await queued
            .GroupBy(m => m.WorkCenter.Name)
            .Select(g => new
            {
                WorkCenter = g.Key,
                Lots = g.Count(),
                Qty = g.Sum(x => x.Quantity - x.QuantityCompleted),
            })
            .OrderByDescending(x => x.Qty)
            .Take(5)
            .ToListAsync(ct);

        if (grouped.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No queue backlog risk was detected in this scope.",
                ScopeUsed = lineId.HasValue ? "context" : "plant-wide",
                Confidence = 0.8m,
            };
        }

        var highest = grouped.First();
        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"{highest.WorkCenter} has the largest queue backlog right now.",
            ScopeUsed = lineId.HasValue ? "context" : "plant-wide",
            Confidence = 0.86m,
            DataPoints = grouped.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.WorkCenter,
                Value = x.Qty.ToString(),
                Unit = $"{x.Lots} lots"
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildTargetAtRiskByShiftAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        if (request.Context?.WorkCenterId is not Guid wcId || request.Context?.ProductionLineId is not Guid lineId)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Select both work center and production line to compute target-at-risk by shift.",
                ScopeUsed = "context",
                Confidence = 0.45m,
            };
        }

        var tz = await GetPlantTimeZoneAsync(plantId, ct);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var schedule = await _db.ShiftSchedules
            .Where(s => s.PlantId == plantId && s.EffectiveDate <= DateOnly.FromDateTime(localDate))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);
        if (schedule == null)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No shift schedule found to compute shift target risk.",
                ScopeUsed = "context",
                Confidence = 0.5m,
            };
        }

        var targetUph = await _db.WorkCenterCapacityTargets
            .Where(t => t.WorkCenterProductionLine.WorkCenterId == wcId
                        && t.WorkCenterProductionLine.ProductionLineId == lineId)
            .Select(t => t.TargetUnitsPerHour)
            .DefaultIfEmpty(0m)
            .AverageAsync(ct);

        if (targetUph <= 0m)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No capacity target is configured for this work center/line.",
                ScopeUsed = "context",
                Confidence = 0.52m,
            };
        }

        var plannedMinutes = schedule.GetPlannedMinutes(localNow.DayOfWeek);
        if (plannedMinutes <= 0m)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No planned shift minutes are configured for today.",
                ScopeUsed = "context",
                Confidence = 0.55m,
            };
        }

        var elapsedMinutes = Math.Clamp((decimal)localNow.TimeOfDay.TotalMinutes, 0m, plannedMinutes);
        var elapsedRatio = Math.Clamp(elapsedMinutes / plannedMinutes, 0.01m, 1m);
        var plannedUnits = Math.Floor(targetUph * (plannedMinutes / 60m));
        var expectedByNow = Math.Floor(plannedUnits * elapsedRatio);

        var metrics = await _supervisorDashboardService.GetMetricsAsync(
            wcId, plantId, localDate.ToString("yyyy-MM-dd"), request.Context.OperatorId, ct);
        var projectedEnd = Math.Floor(metrics.DayCount / elapsedRatio);
        var projectedDelta = projectedEnd - plannedUnits;

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = projectedDelta >= 0
                ? $"Current pace projects +{projectedDelta} units versus today's planned target."
                : $"Current pace projects {Math.Abs(projectedDelta)} units below today's planned target.",
            ScopeUsed = "context",
            Confidence = 0.86m,
            DataPoints =
            [
                new() { Label = "Target units (planned)", Value = plannedUnits.ToString() },
                new() { Label = "Expected by now", Value = expectedByNow.ToString() },
                new() { Label = "Actual by now", Value = metrics.DayCount.ToString() },
                new() { Label = "Projected end-of-shift", Value = projectedEnd.ToString() },
                new() { Label = "Projected delta", Value = projectedDelta.ToString() },
            ],
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildCycleTimeAnomaliesAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        if (request.Context?.WorkCenterId is not Guid wcId)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Select a work center to analyze scan-gap anomalies.",
                ScopeUsed = "context",
                Confidence = 0.45m,
            };
        }

        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var timestamps = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= start && r.Timestamp < end)
            .OrderBy(r => r.Timestamp)
            .Select(r => r.Timestamp)
            .ToListAsync(ct);

        if (timestamps.Count < 2)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "Not enough scans to compute cycle-time anomalies.",
                ScopeUsed = "context",
                Confidence = 0.7m,
            };
        }

        var gaps = new List<(DateTime at, double min)>();
        for (var i = 1; i < timestamps.Count; i++)
            gaps.Add((timestamps[i], (timestamps[i] - timestamps[i - 1]).TotalMinutes));
        var avg = gaps.Average(x => x.min);
        var threshold = Math.Max(avg * 1.8, 15d);
        var anomalies = gaps.Where(x => x.min >= threshold).OrderByDescending(x => x.min).Take(5).ToList();

        if (anomalies.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = $"No unusual scan-gap anomalies found. Average gap is {Math.Round(avg, 1)} minutes.",
                ScopeUsed = "context",
                Confidence = 0.83m,
            };
        }

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"Detected {anomalies.Count} cycle-time anomalies above {Math.Round(threshold, 1)} minutes.",
            ScopeUsed = "context",
            Confidence = 0.84m,
            DataPoints = anomalies.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = $"Gap ending {x.at:HH:mm}",
                Value = Math.Round(x.min, 1).ToString(),
                Unit = "minutes"
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildAnnotationFollowUpNeededAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        DateTime localDate,
        CancellationToken ct)
    {
        var (start, end) = await ResolveDateRangeAsync(plantId, localDate, ct);
        var wcId = request.Context?.WorkCenterId;
        var query = _db.Annotations
            .Where(a => a.CreatedAt >= start && a.CreatedAt < end
                        && a.Status == AnnotationStatus.Open
                        && (a.ProductionRecord == null || a.ProductionRecord.ProductionLine.PlantId == plantId));
        if (wcId.HasValue)
            query = query.Where(a => a.ProductionRecord != null && a.ProductionRecord.WorkCenterId == wcId.Value);

        var grouped = await query
            .GroupBy(a => a.AnnotationType.Name)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        if (grouped.Count == 0)
        {
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = "No open annotation follow-up items were found for this scope today.",
                ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
                Confidence = 0.82m,
            };
        }

        var total = grouped.Sum(x => x.Count);
        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = $"{total} open annotation items need follow-up.",
            ScopeUsed = wcId.HasValue ? "context" : "plant-wide",
            Confidence = 0.86m,
            DataPoints = grouped.Select(x => new NaturalLanguageQueryDataPointDto
            {
                Label = x.Type,
                Value = x.Count.ToString(),
                Unit = "open items"
            }).ToList(),
        };
    }

    private async Task<NaturalLanguageQueryResponseDto> BuildQualityVsThroughputTradeoffAsync(
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        string date,
        CancellationToken ct)
    {
        var wcId = request.Context?.WorkCenterId;
        if (wcId.HasValue)
        {
            var metrics = await _supervisorDashboardService.GetMetricsAsync(
                wcId.Value, plantId, date, request.Context?.OperatorId, ct);
            if (metrics.DayFPY is null)
            {
                return new NaturalLanguageQueryResponseDto
                {
                    AnswerText = "Quality/throughput tradeoff is unavailable because FPY is not measured for this work center.",
                    ScopeUsed = "context",
                    Confidence = 0.6m,
                };
            }

            var tradeoff = metrics.DayQtyPerHour >= metrics.WeekQtyPerHour && metrics.DayFPY < metrics.WeekFPY;
            return new NaturalLanguageQueryResponseDto
            {
                AnswerText = tradeoff
                    ? "This work center shows a potential quality-vs-throughput tradeoff today."
                    : "No strong quality-vs-throughput tradeoff signal was detected for this work center today.",
                ScopeUsed = "context",
                Confidence = 0.82m,
                DataPoints =
                [
                    new() { Label = "Day qty/hour", Value = metrics.DayQtyPerHour.ToString("0.##") },
                    new() { Label = "Week qty/hour", Value = metrics.WeekQtyPerHour.ToString("0.##") },
                    new() { Label = "Day FPY", Value = metrics.DayFPY?.ToString("0.0") ?? "--", Unit = "%" },
                    new() { Label = "Week FPY", Value = metrics.WeekFPY?.ToString("0.0") ?? "--", Unit = "%" },
                ],
            };
        }

        var wcIds = await _db.WorkCenterProductionLines
            .Where(x => x.ProductionLine.PlantId == plantId)
            .Select(x => x.WorkCenterId)
            .Distinct()
            .ToListAsync(ct);

        var rows = new List<NaturalLanguageQueryDataPointDto>();
        foreach (var id in wcIds)
        {
            var metrics = await _supervisorDashboardService.GetMetricsAsync(id, plantId, date, null, ct);
            if (metrics.DayFPY is null || metrics.WeekFPY is null)
                continue;
            var hasTradeoff = metrics.DayQtyPerHour >= metrics.WeekQtyPerHour && metrics.DayFPY < metrics.WeekFPY;
            if (!hasTradeoff)
                continue;

            var name = await _db.WorkCenters.Where(w => w.Id == id).Select(w => w.Name).FirstOrDefaultAsync(ct) ?? id.ToString("N")[..8];
            rows.Add(new NaturalLanguageQueryDataPointDto
            {
                Label = name,
                Value = $"qty/hr {metrics.DayQtyPerHour:0.##}, FPY {metrics.DayFPY:0.0}% vs week {metrics.WeekFPY:0.0}%"
            });
        }

        return new NaturalLanguageQueryResponseDto
        {
            AnswerText = rows.Count == 0
                ? "No strong quality-vs-throughput tradeoff signals were detected plant-wide today."
                : $"Detected {rows.Count} work centers with high-throughput / lower-quality tradeoff signals.",
            ScopeUsed = "plant-wide",
            Confidence = 0.8m,
            DataPoints = rows.Take(5).ToList(),
        };
    }

    private static (string dateText, DateTime dateValue) ResolveDate(string? dateText)
    {
        if (!string.IsNullOrWhiteSpace(dateText) && DateTime.TryParse(dateText, out var parsed))
            return (parsed.ToString("yyyy-MM-dd"), parsed.Date);

        var today = DateTime.UtcNow.Date;
        return (today.ToString("yyyy-MM-dd"), today);
    }

    private async Task<(DateTime startUtc, DateTime endUtc)> ResolveDateRangeAsync(Guid plantId, DateTime localDate, CancellationToken cancellationToken)
    {
        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var start = TimeZoneInfo.ConvertTimeToUtc(localDate.Date, tz);
        var end = TimeZoneInfo.ConvertTimeToUtc(localDate.Date.AddDays(1), tz);
        return (start, end);
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(Guid plantId, CancellationToken cancellationToken)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
