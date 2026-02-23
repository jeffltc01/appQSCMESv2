using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class NaturalLanguageQueryToolExecutor : INlqToolExecutor
{
    private readonly MesDbContext _db;
    private readonly ISupervisorDashboardService _supervisorDashboardService;

    public NaturalLanguageQueryToolExecutor(
        MesDbContext db,
        ISupervisorDashboardService supervisorDashboardService)
    {
        _db = db;
        _supervisorDashboardService = supervisorDashboardService;
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
            NlqIntent.TanksProducedToday => await BuildTanksProducedTodayAsync(
                request, plantId, dateText, cancellationToken),
            NlqIntent.WorkCentersBehindTargetToday => await BuildWorkCentersBehindTargetAsync(
                plantId, dateText, cancellationToken),
            NlqIntent.TopDowntimeDriversToday => await BuildTopDowntimeDriversAsync(
                plantId, dateValue, cancellationToken),
            NlqIntent.WorkCenterPerformanceSummary => await BuildWorkCenterPerformanceSummaryAsync(
                request, plantId, dateText, cancellationToken),
            _ => new NaturalLanguageQueryResponseDto
            {
                AnswerText = "I could not map that question to a supported metric intent yet.",
                ScopeUsed = "unknown",
                Confidence = 0.35m,
                DataPoints =
                [
                    new NaturalLanguageQueryDataPointDto
                    {
                        Label = "Supported intents",
                        Value = "tanks produced today, work centers behind target, downtime drivers, work center performance",
                    }
                ],
            },
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
        var tz = await GetPlantTimeZoneAsync(plantId, ct);
        var start = TimeZoneInfo.ConvertTimeToUtc(localDate.Date, tz);
        var end = TimeZoneInfo.ConvertTimeToUtc(localDate.Date.AddDays(1), tz);

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

    private static (string dateText, DateTime dateValue) ResolveDate(string? dateText)
    {
        if (!string.IsNullOrWhiteSpace(dateText) && DateTime.TryParse(dateText, out var parsed))
            return (parsed.ToString("yyyy-MM-dd"), parsed.Date);

        var today = DateTime.UtcNow.Date;
        return (today.ToString("yyyy-MM-dd"), today);
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
