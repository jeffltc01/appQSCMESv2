using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class SupervisorDashboardService : ISupervisorDashboardService
{
    private static readonly Guid CharLongSeamId =
        Guid.Parse("c1000001-0000-0000-0000-000000000001");

    private static readonly Guid[] CharRoundSeamIds =
    {
        Guid.Parse("c2000001-0000-0000-0000-000000000001"),
        Guid.Parse("c2000002-0000-0000-0000-000000000002"),
        Guid.Parse("c2000003-0000-0000-0000-000000000003"),
        Guid.Parse("c2000004-0000-0000-0000-000000000004"),
    };

    private static readonly HashSet<string> LongSeamDataEntryTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Rolls", "Barcode-LongSeam" };

    private static readonly HashSet<string> RoundSeamDataEntryTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Fitup", "Barcode-RoundSeam" };

    private readonly MesDbContext _db;
    private readonly ILogger<SupervisorDashboardService> _logger;
    private readonly IOeeService _oeeService;

    public SupervisorDashboardService(MesDbContext db, ILogger<SupervisorDashboardService> logger, IOeeService oeeService)
    {
        _db = db;
        _logger = logger;
        _oeeService = oeeService;
    }

    public async Task<SupervisorDashboardMetricsDto> GetMetricsAsync(
        Guid wcId, Guid plantId, string date, Guid? operatorId = null,
        CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var dayOfWeek = localDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)localDate.DayOfWeek - 1;
        var weekStart = localDate.AddDays(-dayOfWeek);
        var startOfWeek = TimeZoneInfo.ConvertTimeToUtc(weekStart, tz);
        var endOfWeek = TimeZoneInfo.ConvertTimeToUtc(weekStart.AddDays(7), tz);

        var dataEntryType = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.DataEntryType)
            .FirstOrDefaultAsync(cancellationToken);

        var baseQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId);

        if (operatorId.HasValue)
            baseQuery = baseQuery.Where(r => r.OperatorId == operatorId.Value);

        var weekRecords = await baseQuery
            .Where(r => r.Timestamp >= startOfWeek && r.Timestamp < endOfWeek)
            .Select(r => new { r.Id, r.Timestamp, r.SerialNumberId, r.OperatorId })
            .ToListAsync(cancellationToken);

        var dayRecords = weekRecords
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .ToList();

        var dto = new SupervisorDashboardMetricsDto
        {
            DayCount = dayRecords.Count,
            WeekCount = weekRecords.Count,
        };

        // ---- Hourly counts (local time) ----
        var hourlyGroups = dayRecords
            .GroupBy(r => TimeZoneInfo.ConvertTimeFromUtc(r.Timestamp, tz).Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        for (var h = 0; h < 24; h++)
            dto.HourlyCounts.Add(new HourlyCountDto { Hour = h, Count = hourlyGroups.GetValueOrDefault(h) });

        // ---- Weekly daily counts ----
        var dailyGroups = weekRecords
            .GroupBy(r => TimeZoneInfo.ConvertTimeFromUtc(r.Timestamp, tz).Date)
            .ToDictionary(g => g.Key, g => g.Count());

        for (var d = 0; d < 7; d++)
        {
            var day = weekStart.AddDays(d);
            dto.WeekDailyCounts.Add(new DailyCountDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Count = dailyGroups.GetValueOrDefault(day),
            });
        }

        // ---- Avg time between scans ----
        dto.DayAvgTimeBetweenScans = ComputeAvgTimeBetweenScans(dayRecords.Select(r => r.Timestamp).ToList());
        dto.WeekAvgTimeBetweenScans = ComputeAvgTimeBetweenScans(weekRecords.Select(r => r.Timestamp).ToList());

        // ---- Qty per hour ----
        dto.DayQtyPerHour = ComputeQtyPerHour(dayRecords.Select(r => r.Timestamp).ToList(), startOfDay, endOfDay);
        dto.WeekQtyPerHour = ComputeQtyPerHour(weekRecords.Select(r => r.Timestamp).ToList(), startOfWeek, endOfWeek);

        // ---- FPY & defect count ----
        var charIds = GetApplicableCharacteristicIds(dataEntryType);
        dto.SupportsFirstPassYield = charIds is not null;
        if (charIds is not null)
        {
            var (dayFpy, dayDefects) = await ComputeFpyAsync(
                wcId, plantId, startOfDay, endOfDay, charIds, operatorId, cancellationToken);
            var (weekFpy, weekDefects) = await ComputeFpyAsync(
                wcId, plantId, startOfWeek, endOfWeek, charIds, operatorId, cancellationToken);

            dto.DayFPY = dayFpy;
            dto.WeekFPY = weekFpy;
            dto.DayDefects = dayDefects;
            dto.WeekDefects = weekDefects;
        }

        // ---- Operators for the week (unfiltered) ----
        var weekOperatorRecords = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= startOfWeek
                        && r.Timestamp < endOfWeek);

        dto.Operators = await weekOperatorRecords
            .GroupBy(r => new { r.OperatorId, r.Operator.DisplayName })
            .Select(g => new OperatorSummaryDto
            {
                Id = g.Key.OperatorId,
                DisplayName = g.Key.DisplayName,
                RecordCount = g.Count(),
            })
            .OrderByDescending(o => o.RecordCount)
            .ToListAsync(cancellationToken);

        // ---- OEE (unfiltered by operator) ----
        try
        {
            var oee = await _oeeService.CalculateOeeAsync(wcId, plantId, date, cancellationToken);
            dto.OeeAvailability = oee.Availability;
            dto.OeePerformance = oee.Performance;
            dto.OeePlannedMinutes = oee.PlannedMinutes;
            dto.OeeDowntimeMinutes = oee.DowntimeMinutes;
            dto.OeeRunTimeMinutes = oee.RunTimeMinutes;

            // Use FPY as the Quality component when available
            if (dto.DayFPY.HasValue)
            {
                dto.OeeQuality = dto.DayFPY;
                if (oee.Availability.HasValue && oee.Performance.HasValue)
                    dto.OeeOverall = Math.Round(
                        oee.Availability.Value / 100m * oee.Performance.Value / 100m * dto.DayFPY.Value / 100m * 100m, 1);
            }
            else if (oee.Availability.HasValue && oee.Performance.HasValue)
            {
                dto.OeeQuality = 100m;
                dto.OeeOverall = Math.Round(oee.Availability.Value / 100m * oee.Performance.Value / 100m * 100m, 1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OEE calculation failed for WC {WcId}", wcId);
        }

        return dto;
    }

    public async Task<IReadOnlyList<SupervisorRecordDto>> GetRecordsAsync(
        Guid wcId, Guid plantId, string date, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber)
            .Include(r => r.SerialNumber!.Product)
            .Include(r => r.Operator)
            .Include(r => r.ProductionLine)
            .Where(r => r.WorkCenterId == wcId
                        && r.Timestamp >= startOfDay
                        && r.Timestamp < endOfDay
                        && r.ProductionLine.PlantId == plantId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
            return Array.Empty<SupervisorRecordDto>();

        var recordIds = records.Select(r => r.Id).ToList();

        var annotations = await _db.Annotations
            .Include(a => a.AnnotationType)
            .Where(a => a.ProductionRecordId != null && recordIds.Contains(a.ProductionRecordId.Value))
            .ToListAsync(cancellationToken);

        var annotationsByRecord = annotations
            .GroupBy(a => a.ProductionRecordId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(a => new ExistingAnnotationDto
            {
                AnnotationTypeId = a.AnnotationTypeId,
                TypeName = a.AnnotationType?.Name ?? string.Empty,
                Abbreviation = a.AnnotationType?.Abbreviation,
                DisplayColor = a.AnnotationType?.DisplayColor,
            }).ToList());

        return records.Select(r => new SupervisorRecordDto
        {
            Id = r.Id,
            Timestamp = r.Timestamp,
            SerialOrIdentifier = r.SerialNumber?.Serial ?? r.Id.ToString("N")[..8],
            TankSize = r.SerialNumber?.Product?.TankSize.ToString(),
            OperatorName = r.Operator?.DisplayName ?? string.Empty,
            Annotations = annotationsByRecord.GetValueOrDefault(r.Id) ?? new List<ExistingAnnotationDto>(),
        }).ToList();
    }

    public async Task<SupervisorAnnotationResultDto> SubmitAnnotationAsync(
        Guid userId, CreateSupervisorAnnotationRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingPairs = await _db.Annotations
            .Where(a => a.ProductionRecordId != null
                        && request.RecordIds.Contains(a.ProductionRecordId.Value)
                        && a.AnnotationTypeId == request.AnnotationTypeId)
            .Select(a => a.ProductionRecordId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var alreadySet = existingPairs.ToHashSet();
        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var recordId in request.RecordIds)
        {
            if (alreadySet.Contains(recordId))
                continue;

            _db.Annotations.Add(new Annotation
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recordId,
                AnnotationTypeId = request.AnnotationTypeId,
                Flag = true,
                Notes = request.Comment,
                InitiatedByUserId = userId,
                CreatedAt = now,
            });
            created++;
        }

        if (created > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SupervisorDashboard: User {UserId} created {Count} annotations of type {TypeId}",
            userId, created, request.AnnotationTypeId);

        return new SupervisorAnnotationResultDto { AnnotationsCreated = created };
    }

    // ---- Private helpers ----

    private async Task<(decimal? fpy, int defectCount)> ComputeFpyAsync(
        Guid wcId, Guid plantId, DateTime utcStart, DateTime utcEnd,
        Guid[] characteristicIds, Guid? operatorId,
        CancellationToken cancellationToken)
    {
        var query = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= utcStart
                        && r.Timestamp < utcEnd);

        if (operatorId.HasValue)
            query = query.Where(r => r.OperatorId == operatorId.Value);

        // First-pass serial numbers: SNs whose earliest record at this WC falls in the window
        var snFirstPass = await query
            .GroupBy(r => r.SerialNumberId)
            .Select(g => new
            {
                SerialNumberId = g.Key,
                FirstTimestamp = g.Min(r => r.Timestamp),
            })
            .ToListAsync(cancellationToken);

        var firstPassSns = snFirstPass
            .Where(s => s.FirstTimestamp >= utcStart && s.FirstTimestamp < utcEnd)
            .Select(s => s.SerialNumberId)
            .ToList();

        // Also need to exclude SNs that had an earlier record at this WC before the window
        var snsWithEarlierRecords = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp < utcStart
                        && firstPassSns.Contains(r.SerialNumberId))
            .Select(r => r.SerialNumberId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var trueFirstPassSns = firstPassSns.Except(snsWithEarlierRecords).ToList();
        var opportunities = trueFirstPassSns.Count;

        if (opportunities == 0)
            return (null, 0);

        var defectCount = await _db.DefectLogs
            .Where(d => trueFirstPassSns.Contains(d.SerialNumberId)
                        && characteristicIds.Contains(d.CharacteristicId)
                        && d.Timestamp >= utcStart
                        && d.Timestamp < utcEnd)
            .Select(d => d.SerialNumberId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalDefects = await _db.DefectLogs
            .Where(d => trueFirstPassSns.Contains(d.SerialNumberId)
                        && characteristicIds.Contains(d.CharacteristicId)
                        && d.Timestamp >= utcStart
                        && d.Timestamp < utcEnd)
            .CountAsync(cancellationToken);

        var fpy = Math.Round((decimal)(opportunities - defectCount) / opportunities * 100, 1);
        return (fpy, totalDefects);
    }

    private static Guid[]? GetApplicableCharacteristicIds(string? dataEntryType)
    {
        if (string.IsNullOrEmpty(dataEntryType))
            return null;

        if (LongSeamDataEntryTypes.Contains(dataEntryType))
            return new[] { CharLongSeamId };

        if (RoundSeamDataEntryTypes.Contains(dataEntryType))
            return CharRoundSeamIds;

        return null;
    }

    private static double ComputeAvgTimeBetweenScans(List<DateTime> timestamps)
    {
        if (timestamps.Count < 2)
            return 0;

        var sorted = timestamps.OrderBy(t => t).ToList();
        var totalSeconds = 0.0;
        for (var i = 1; i < sorted.Count; i++)
            totalSeconds += (sorted[i] - sorted[i - 1]).TotalSeconds;

        return Math.Round(totalSeconds / (sorted.Count - 1), 1);
    }

    private static decimal ComputeQtyPerHour(List<DateTime> timestamps, DateTime windowStart, DateTime windowEnd)
    {
        if (timestamps.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        var effectiveEnd = now < windowEnd ? now : windowEnd;
        var hours = (decimal)(effectiveEnd - windowStart).TotalHours;

        if (hours <= 0)
            return 0;

        return Math.Round(timestamps.Count / hours, 1);
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
