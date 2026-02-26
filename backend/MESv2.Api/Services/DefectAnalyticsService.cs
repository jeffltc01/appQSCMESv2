using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class DefectAnalyticsService : IDefectAnalyticsService
{
    private readonly MesDbContext _db;

    public DefectAnalyticsService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<DefectParetoResponseDto> GetDefectParetoAsync(
        Guid wcId, Guid plantId, string date, string view,
        Guid? operatorId = null, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var (windowStartUtc, windowEndUtc) = ResolveViewWindowUtc(localDate, tz, view);

        var serialsFromProductionQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= windowStartUtc
                        && r.Timestamp < windowEndUtc);
        if (operatorId.HasValue)
            serialsFromProductionQuery = serialsFromProductionQuery.Where(r => r.OperatorId == operatorId.Value);

        var serialsFromInspectionQuery = _db.InspectionRecords
            .Where(i => i.WorkCenterId == wcId
                        && i.ProductionRecord.ProductionLine.PlantId == plantId
                        && i.Timestamp >= windowStartUtc
                        && i.Timestamp < windowEndUtc);
        if (operatorId.HasValue)
            serialsFromInspectionQuery = serialsFromInspectionQuery.Where(i => i.OperatorId == operatorId.Value);

        var serialsFromWorkCenter = serialsFromProductionQuery
            .Select(r => r.SerialNumberId)
            .Concat(serialsFromInspectionQuery.Select(i => i.SerialNumberId))
            .Distinct();

        var defectsQuery = _db.DefectLogs
            .Where(d => d.Timestamp >= windowStartUtc
                        && d.Timestamp < windowEndUtc
                        && (
                            (d.ProductionRecordId != null
                             && d.ProductionRecord!.WorkCenterId == wcId
                             && d.ProductionRecord.ProductionLine.PlantId == plantId
                             && (!operatorId.HasValue || d.ProductionRecord.OperatorId == operatorId.Value))
                            || serialsFromWorkCenter.Contains(d.SerialNumberId)
                        ));

        var totalDefects = await defectsQuery.CountAsync(cancellationToken);
        if (totalDefects == 0)
            return new DefectParetoResponseDto();

        var grouped = await defectsQuery
            .Select(d => new
            {
                Code = d.DefectCode != null ? d.DefectCode.Code : "UNKNOWN",
                Name = d.DefectCode != null ? d.DefectCode.Name : "Unknown Defect",
            })
            .GroupBy(d => new { d.Code, d.Name })
            .Select(g => new
            {
                g.Key.Code,
                g.Key.Name,
                Count = g.Count(),
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var runningCount = 0;
        var items = grouped.Select(item =>
        {
            runningCount += item.Count;
            return new DefectParetoItemDto
            {
                DefectCode = item.Code,
                DefectName = item.Name,
                Count = item.Count,
                CumulativePercent = Math.Round((decimal)runningCount / totalDefects * 100m, 1),
            };
        }).ToList();

        return new DefectParetoResponseDto
        {
            TotalDefects = totalDefects,
            Items = items,
        };
    }

    public async Task<DowntimeParetoResponseDto> GetDowntimeParetoAsync(
        Guid wcId, Guid plantId, string date, string view,
        Guid? operatorId = null, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var (windowStartUtc, windowEndUtc) = ResolveViewWindowUtc(localDate, tz, view);

        var eventsQuery = _db.DowntimeEvents
            .Where(e => e.WorkCenterProductionLine.WorkCenterId == wcId
                        && e.WorkCenterProductionLine.ProductionLine.PlantId == plantId
                        && e.StartedAt < windowEndUtc
                        && e.EndedAt > windowStartUtc
                        && (e.DowntimeReasonId == null || e.DowntimeReason!.CountsAsDowntime));
        if (operatorId.HasValue)
            eventsQuery = eventsQuery.Where(e => e.OperatorUserId == operatorId.Value);

        var events = await eventsQuery
            .Select(e => new
            {
                e.StartedAt,
                e.EndedAt,
                ReasonName = e.DowntimeReasonId == null
                    ? "Unspecified"
                    : e.DowntimeReason!.Name,
            })
            .ToListAsync(cancellationToken);

        var grouped = events
            .Select(e =>
            {
                var clampedStart = e.StartedAt < windowStartUtc ? windowStartUtc : e.StartedAt;
                var clampedEnd = e.EndedAt > windowEndUtc ? windowEndUtc : e.EndedAt;
                var minutes = (decimal)(clampedEnd - clampedStart).TotalMinutes;
                return new { e.ReasonName, Minutes = minutes > 0m ? minutes : 0m };
            })
            .Where(e => e.Minutes > 0m)
            .GroupBy(e => e.ReasonName)
            .Select(g => new
            {
                ReasonName = g.Key,
                Minutes = Math.Round(g.Sum(x => x.Minutes), 1),
            })
            .OrderByDescending(x => x.Minutes)
            .ThenBy(x => x.ReasonName)
            .ToList();

        var totalMinutes = grouped.Sum(x => x.Minutes);
        if (totalMinutes <= 0m)
            return new DowntimeParetoResponseDto();

        decimal runningMinutes = 0m;
        var items = grouped.Select(item =>
        {
            runningMinutes += item.Minutes;
            return new DowntimeParetoItemDto
            {
                ReasonName = item.ReasonName,
                Minutes = item.Minutes,
                CumulativePercent = Math.Round(runningMinutes / totalMinutes * 100m, 1),
            };
        }).ToList();

        return new DowntimeParetoResponseDto
        {
            TotalDowntimeMinutes = Math.Round(totalMinutes, 1),
            Items = items,
        };
    }

    private static (DateTime windowStartUtc, DateTime windowEndUtc) ResolveViewWindowUtc(
        DateTime localDate,
        TimeZoneInfo tz,
        string view)
    {
        var normalizedView = view.ToLowerInvariant();
        return normalizedView switch
        {
            "week" => ResolveWeekWindow(localDate, tz),
            "month" => ResolveMonthWindow(localDate, tz),
            _ => ResolveDayWindow(localDate, tz),
        };
    }

    private static (DateTime windowStartUtc, DateTime windowEndUtc) ResolveDayWindow(
        DateTime localDate,
        TimeZoneInfo tz)
    {
        var dayStart = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var dayEnd = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);
        return (dayStart, dayEnd);
    }

    private static (DateTime windowStartUtc, DateTime windowEndUtc) ResolveWeekWindow(
        DateTime localDate,
        TimeZoneInfo tz)
    {
        var dayOfWeek = localDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)localDate.DayOfWeek - 1;
        var weekStart = localDate.AddDays(-dayOfWeek);
        var start = TimeZoneInfo.ConvertTimeToUtc(weekStart, tz);
        var end = TimeZoneInfo.ConvertTimeToUtc(weekStart.AddDays(7), tz);
        return (start, end);
    }

    private static (DateTime windowStartUtc, DateTime windowEndUtc) ResolveMonthWindow(
        DateTime localDate,
        TimeZoneInfo tz)
    {
        var monthStart = new DateTime(localDate.Year, localDate.Month, 1);
        var start = TimeZoneInfo.ConvertTimeToUtc(monthStart, tz);
        var end = TimeZoneInfo.ConvertTimeToUtc(monthStart.AddMonths(1), tz);
        return (start, end);
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(
        Guid plantId, CancellationToken cancellationToken)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }

        return TimeZoneInfo.Utc;
    }

}
