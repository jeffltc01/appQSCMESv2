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
        var monthStart = new DateTime(localDate.Year, localDate.Month, 1);
        var startOfMonth = TimeZoneInfo.ConvertTimeToUtc(monthStart, tz);
        var endOfMonth = TimeZoneInfo.ConvertTimeToUtc(monthStart.AddMonths(1), tz);

        var dataEntryType = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.DataEntryType)
            .FirstOrDefaultAsync(cancellationToken);

        var baseQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId);

        if (operatorId.HasValue)
            baseQuery = baseQuery.Where(r => r.OperatorId == operatorId.Value);

        var monthRecords = await baseQuery
            .Where(r => r.Timestamp >= startOfMonth && r.Timestamp < endOfMonth)
            .Select(r => new { r.Id, r.Timestamp, r.SerialNumberId, r.OperatorId })
            .ToListAsync(cancellationToken);

        var weekRecords = monthRecords
            .Where(r => r.Timestamp >= startOfWeek && r.Timestamp < endOfWeek)
            .ToList();

        var dayRecords = weekRecords
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .ToList();

        var dto = new SupervisorDashboardMetricsDto
        {
            DayCount = dayRecords.Count,
            WeekCount = weekRecords.Count,
            MonthCount = monthRecords.Count,
        };

        var dayTimestamps = dayRecords.Select(r => r.Timestamp).ToList();
        var weekTimestamps = weekRecords.Select(r => r.Timestamp).ToList();
        var monthTimestamps = monthRecords.Select(r => r.Timestamp).ToList();

        ComputeHourlyCounts(dto, dayTimestamps, tz);
        ComputeWeeklyCounts(dto, weekTimestamps, weekStart, tz);

        dto.DayAvgTimeBetweenScans = ComputeAvgTimeBetweenScans(dayTimestamps);
        dto.WeekAvgTimeBetweenScans = ComputeAvgTimeBetweenScans(weekTimestamps);
        dto.MonthAvgTimeBetweenScans = ComputeAvgTimeBetweenScans(monthTimestamps);
        dto.DayQtyPerHour = ComputeQtyPerHour(dayTimestamps, startOfDay, endOfDay);
        dto.WeekQtyPerHour = ComputeQtyPerHour(weekTimestamps, startOfWeek, endOfWeek);
        dto.MonthQtyPerHour = ComputeQtyPerHour(monthTimestamps, startOfMonth, endOfMonth);

        await ComputeFpyAndDefects(
            dto,
            wcId,
            plantId,
            dataEntryType,
            startOfDay,
            endOfDay,
            startOfWeek,
            endOfWeek,
            startOfMonth,
            endOfMonth,
            operatorId,
            cancellationToken);
        dto.Operators = await BuildOperatorSummary(wcId, plantId, startOfWeek, endOfWeek, cancellationToken);
        await ComputeOeeMetrics(dto, wcId, plantId, date, cancellationToken);

        return dto;
    }

    public async Task<SupervisorDashboardTrendsDto> GetTrendsAsync(
        Guid wcId, Guid plantId, string date, Guid? operatorId = null, int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var normalizedDays = Math.Clamp(days, 1, 30);
        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var startLocalDate = localDate.AddDays(-(normalizedDays - 1));
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocalDate, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var dataEntryType = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.DataEntryType)
            .FirstOrDefaultAsync(cancellationToken);
        var characteristicIds = GetApplicableCharacteristicIds(dataEntryType);

        var recordsQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= startUtc
                        && r.Timestamp < endUtc);
        if (operatorId.HasValue)
            recordsQuery = recordsQuery.Where(r => r.OperatorId == operatorId.Value);

        var records = await recordsQuery
            .Select(r => new { r.Timestamp })
            .ToListAsync(cancellationToken);

        var timestampsByDay = records
            .GroupBy(r => TimeZoneInfo.ConvertTimeFromUtc(r.Timestamp, tz).Date)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Timestamp).ToList());

        var trendDates = Enumerable
            .Range(0, normalizedDays)
            .Select(offset => startLocalDate.AddDays(offset))
            .ToList();

        var fpyByDay = new Dictionary<DateTime, (decimal? fpy, int defectCount)>();
        if (characteristicIds is not null)
        {
            var periods = trendDates
                .Select(day => (
                    key: day,
                    utcStart: TimeZoneInfo.ConvertTimeToUtc(day, tz),
                    utcEnd: TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1), tz)))
                .ToList();
            fpyByDay = await BatchComputeFpyAsync(
                wcId, plantId, periods, characteristicIds, operatorId, cancellationToken);
        }

        var dto = new SupervisorDashboardTrendsDto();
        foreach (var day in trendDates)
        {
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(day, tz);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1), tz);
            var timestamps = timestampsByDay.GetValueOrDefault(day) ?? new List<DateTime>();
            var count = timestamps.Count;
            var dateKey = day.ToString("yyyy-MM-dd");
            var fpyTuple = fpyByDay.GetValueOrDefault(day);
            var oee = await _oeeService.CalculateOeeAsync(wcId, plantId, dateKey, cancellationToken);
            var availability = oee.Availability;
            var performance = oee.Performance;
            decimal? quality = null;
            decimal? overallOee = null;

            if (fpyTuple.fpy.HasValue)
            {
                quality = fpyTuple.fpy;
                if (availability.HasValue && performance.HasValue)
                    overallOee = Math.Round(
                        availability.Value / 100m * performance.Value / 100m * quality.Value / 100m * 100m, 1);
            }
            else if (availability.HasValue && performance.HasValue)
            {
                quality = 100m;
                overallOee = Math.Round(availability.Value / 100m * performance.Value / 100m * 100m, 1);
            }

            dto.Count.Add(new KpiTrendPointDto { Date = dateKey, Value = count });
            dto.Fpy.Add(new KpiTrendPointDto { Date = dateKey, Value = fpyTuple.fpy });
            dto.Defects.Add(new KpiTrendPointDto { Date = dateKey, Value = fpyTuple.defectCount });
            dto.QtyPerHour.Add(new KpiTrendPointDto
            {
                Date = dateKey,
                Value = ComputeQtyPerHour(timestamps, utcStart, utcEnd),
            });
            dto.AvgBetweenScans.Add(new KpiTrendPointDto
            {
                Date = dateKey,
                Value = Convert.ToDecimal(ComputeAvgTimeBetweenScans(timestamps)),
            });
            dto.Availability.Add(new KpiTrendPointDto { Date = dateKey, Value = availability });
            dto.Performance.Add(new KpiTrendPointDto { Date = dateKey, Value = performance });
            dto.Quality.Add(new KpiTrendPointDto { Date = dateKey, Value = quality });
            dto.Oee.Add(new KpiTrendPointDto { Date = dateKey, Value = overallOee });
        }

        return dto;
    }

    private static void ComputeHourlyCounts(
        SupervisorDashboardMetricsDto dto,
        List<DateTime> dayTimestamps,
        TimeZoneInfo tz)
    {
        var hourlyGroups = dayTimestamps
            .GroupBy(t => TimeZoneInfo.ConvertTimeFromUtc(t, tz).Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        for (var h = 0; h < 24; h++)
            dto.HourlyCounts.Add(new HourlyCountDto { Hour = h, Count = hourlyGroups.GetValueOrDefault(h) });
    }

    private static void ComputeWeeklyCounts(
        SupervisorDashboardMetricsDto dto,
        List<DateTime> weekTimestamps,
        DateTime weekStart,
        TimeZoneInfo tz)
    {
        var dailyGroups = weekTimestamps
            .GroupBy(t => TimeZoneInfo.ConvertTimeFromUtc(t, tz).Date)
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
    }

    private async Task ComputeFpyAndDefects(
        SupervisorDashboardMetricsDto dto,
        Guid wcId, Guid plantId, string? dataEntryType,
        DateTime startOfDay, DateTime endOfDay,
        DateTime startOfWeek, DateTime endOfWeek,
        DateTime startOfMonth, DateTime endOfMonth,
        Guid? operatorId, CancellationToken cancellationToken)
    {
        var charIds = GetApplicableCharacteristicIds(dataEntryType);
        dto.SupportsFirstPassYield = charIds is not null;
        if (charIds is null) return;

        var (dayFpy, dayDefects) = await ComputeFpyAsync(
            wcId, plantId, startOfDay, endOfDay, charIds, operatorId, cancellationToken);
        var (weekFpy, weekDefects) = await ComputeFpyAsync(
            wcId, plantId, startOfWeek, endOfWeek, charIds, operatorId, cancellationToken);
        var (monthFpy, monthDefects) = await ComputeFpyAsync(
            wcId, plantId, startOfMonth, endOfMonth, charIds, operatorId, cancellationToken);

        dto.DayFPY = dayFpy;
        dto.WeekFPY = weekFpy;
        dto.MonthFPY = monthFpy;
        dto.DayDefects = dayDefects;
        dto.WeekDefects = weekDefects;
        dto.MonthDefects = monthDefects;
    }

    private async Task<List<OperatorSummaryDto>> BuildOperatorSummary(
        Guid wcId, Guid plantId,
        DateTime startOfWeek, DateTime endOfWeek,
        CancellationToken cancellationToken)
    {
        return await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= startOfWeek
                        && r.Timestamp < endOfWeek)
            .GroupBy(r => new { r.OperatorId, r.Operator.DisplayName })
            .Select(g => new OperatorSummaryDto
            {
                Id = g.Key.OperatorId,
                DisplayName = g.Key.DisplayName,
                RecordCount = g.Count(),
            })
            .OrderByDescending(o => o.RecordCount)
            .ToListAsync(cancellationToken);
    }

    private async Task ComputeOeeMetrics(
        SupervisorDashboardMetricsDto dto,
        Guid wcId, Guid plantId, string date,
        CancellationToken cancellationToken)
    {
        try
        {
            var oee = await _oeeService.CalculateOeeAsync(wcId, plantId, date, cancellationToken);
            dto.OeeAvailability = oee.Availability;
            dto.OeePerformance = oee.Performance;
            dto.OeePlannedMinutes = oee.PlannedMinutes;
            dto.OeeDowntimeMinutes = oee.DowntimeMinutes;
            dto.OeeRunTimeMinutes = oee.RunTimeMinutes;

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
                Status = AnnotationStatus.Open,
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

    public async Task<PerformanceTableResponseDto> GetPerformanceTableAsync(
        Guid wcId, Guid plantId, string date, string view,
        Guid? operatorId = null, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;

        var dataEntryType = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.DataEntryType)
            .FirstOrDefaultAsync(cancellationToken);

        var charIds = GetApplicableCharacteristicIds(dataEntryType);

        var wcplIds = await _db.WorkCenterProductionLines
            .Where(wcpl => wcpl.WorkCenterId == wcId && wcpl.ProductionLine.PlantId == plantId)
            .Select(wcpl => wcpl.Id)
            .ToListAsync(cancellationToken);

        var avgTargetUnitsPerHour = await ResolveTargetUnitsPerHourAsync(wcId, plantId, cancellationToken);

        return view.ToLowerInvariant() switch
        {
            "day" => await BuildDayTableAsync(wcId, plantId, localDate, tz, charIds, wcplIds, avgTargetUnitsPerHour, operatorId, cancellationToken),
            "week" => await BuildWeekTableAsync(wcId, plantId, localDate, tz, charIds, wcplIds, avgTargetUnitsPerHour, operatorId, cancellationToken),
            "month" => await BuildMonthTableAsync(wcId, plantId, localDate, tz, charIds, wcplIds, avgTargetUnitsPerHour, operatorId, cancellationToken),
            _ => new PerformanceTableResponseDto(),
        };
    }

    private async Task<decimal?> ResolveTargetUnitsPerHourAsync(
        Guid wcId, Guid plantId, CancellationToken ct)
    {
        var wcplIds = await _db.WorkCenterProductionLines
            .Where(wcpl => wcpl.WorkCenterId == wcId && wcpl.ProductionLine.PlantId == plantId)
            .Select(wcpl => wcpl.Id)
            .ToListAsync(ct);

        if (wcplIds.Count == 0) return null;

        var targets = await _db.WorkCenterCapacityTargets
            .Where(t => wcplIds.Contains(t.WorkCenterProductionLineId))
            .ToListAsync(ct);

        if (targets.Count == 0) return null;

        // Find the most recently used gear from production records
        var recentGearId = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId && r.PlantGearId != null)
            .OrderByDescending(r => r.Timestamp)
            .Select(r => r.PlantGearId)
            .FirstOrDefaultAsync(ct);

        IEnumerable<WorkCenterCapacityTarget> filtered = targets;
        if (recentGearId != null)
        {
            var gearFiltered = targets.Where(t => t.PlantGearId == recentGearId.Value).ToList();
            if (gearFiltered.Count > 0)
                filtered = gearFiltered;
        }

        // Sum across production lines (a WC with 2 lines each doing 5/hr = 10/hr WC capacity)
        var byLine = filtered.GroupBy(t => t.WorkCenterProductionLineId)
            .Select(g => g.Average(t => t.TargetUnitsPerHour));

        return byLine.Sum();
    }

    private async Task<PerformanceTableResponseDto> BuildDayTableAsync(
        Guid wcId, Guid plantId, DateTime localDate, TimeZoneInfo tz,
        Guid[]? charIds, List<Guid> wcplIds, decimal? targetUph,
        Guid? operatorId, CancellationToken ct)
    {
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var baseQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= startOfDay && r.Timestamp < endOfDay);
        if (operatorId.HasValue)
            baseQuery = baseQuery.Where(r => r.OperatorId == operatorId.Value);

        var records = await baseQuery
            .Select(r => new { r.Timestamp, r.SerialNumberId })
            .ToListAsync(ct);

        var hourlyActual = records
            .GroupBy(r => TimeZoneInfo.ConvertTimeFromUtc(r.Timestamp, tz).Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var downtimeByHour = await GetDowntimeByHourAsync(wcplIds, startOfDay, endOfDay, tz, ct);

        var activeHours = new SortedSet<int>();
        for (var h = 0; h < 24; h++)
        {
            if (hourlyActual.GetValueOrDefault(h) > 0 || downtimeByHour.GetValueOrDefault(h) > 0)
                activeHours.Add(h);
        }

        var flooredTarget = targetUph.HasValue ? Math.Floor(targetUph.Value) : (decimal?)null;

        var fpyPeriods = new List<(int key, DateTime utcStart, DateTime utcEnd)>();
        if (charIds is not null)
        {
            foreach (var h in activeHours)
            {
                var hStart = TimeZoneInfo.ConvertTimeToUtc(localDate.AddHours(h), tz);
                var hEnd = TimeZoneInfo.ConvertTimeToUtc(localDate.AddHours(h + 1), tz);
                fpyPeriods.Add((h, hStart, hEnd));
            }
        }
        var fpyByHour = charIds is not null
            ? await BatchComputeFpyAsync(wcId, plantId, fpyPeriods, charIds, operatorId, ct)
            : new Dictionary<int, (decimal? fpy, int defectCount)>();

        var rows = new List<PerformanceTableRowDto>();
        foreach (var h in activeHours)
        {
            var actual = hourlyActual.GetValueOrDefault(h);
            decimal? fpy = charIds is not null ? fpyByHour.GetValueOrDefault(h).fpy : null;

            rows.Add(new PerformanceTableRowDto
            {
                Label = $"{h:D2}:00",
                Planned = flooredTarget,
                Actual = actual,
                Delta = flooredTarget.HasValue ? actual - flooredTarget.Value : null,
                Fpy = fpy,
                DowntimeMinutes = downtimeByHour.GetValueOrDefault(h),
            });
        }

        var schedule = await _db.ShiftSchedules
            .Where(s => s.PlantId == plantId && s.EffectiveDate <= DateOnly.FromDateTime(localDate))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        var plannedMinutes = schedule?.GetPlannedMinutes(localDate.DayOfWeek) ?? 0;
        var totalPlanned = targetUph.HasValue && plannedMinutes > 0
            ? Math.Floor(targetUph.Value * (plannedMinutes / 60m))
            : (decimal?)null;
        var totalActual = rows.Sum(r => r.Actual);

        var fpyRows = rows.Where(r => r.Fpy.HasValue && r.Actual > 0).ToList();
        decimal? totalFpy = fpyRows.Count > 0
            ? Math.Round(fpyRows.Sum(r => r.Fpy!.Value * r.Actual) / fpyRows.Sum(r => r.Actual), 1)
            : null;

        return new PerformanceTableResponseDto
        {
            Rows = rows,
            TotalRow = new PerformanceTableRowDto
            {
                Label = "Total",
                Planned = totalPlanned,
                Actual = totalActual,
                Delta = totalPlanned.HasValue ? totalActual - totalPlanned.Value : null,
                Fpy = totalFpy,
                DowntimeMinutes = Math.Round(rows.Sum(r => r.DowntimeMinutes), 1),
            },
        };
    }

    private async Task<PerformanceTableResponseDto> BuildWeekTableAsync(
        Guid wcId, Guid plantId, DateTime localDate, TimeZoneInfo tz,
        Guid[]? charIds, List<Guid> wcplIds, decimal? targetUph,
        Guid? operatorId, CancellationToken ct)
    {
        var dow = localDate.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)localDate.DayOfWeek - 1;
        var weekStart = localDate.AddDays(-dow);
        var startOfWeek = TimeZoneInfo.ConvertTimeToUtc(weekStart, tz);
        var endOfWeek = TimeZoneInfo.ConvertTimeToUtc(weekStart.AddDays(7), tz);

        var schedule = await _db.ShiftSchedules
            .Where(s => s.PlantId == plantId && s.EffectiveDate <= DateOnly.FromDateTime(localDate))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        var baseQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= startOfWeek && r.Timestamp < endOfWeek);
        if (operatorId.HasValue)
            baseQuery = baseQuery.Where(r => r.OperatorId == operatorId.Value);

        var records = await baseQuery
            .Select(r => new { r.Timestamp, r.SerialNumberId })
            .ToListAsync(ct);

        var dailyActual = records
            .GroupBy(r => TimeZoneInfo.ConvertTimeFromUtc(r.Timestamp, tz).Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var allDowntimeEvents = wcplIds.Count > 0
            ? await _db.DowntimeEvents
                .Where(e => wcplIds.Contains(e.WorkCenterProductionLineId)
                            && e.StartedAt < endOfWeek && e.EndedAt > startOfWeek
                            && (e.DowntimeReasonId == null || e.DowntimeReason!.CountsAsDowntime))
                .Select(e => new { e.StartedAt, e.EndedAt, e.DurationMinutes })
                .ToListAsync(ct)
            : new();

        var fpyPeriods = new List<(int key, DateTime utcStart, DateTime utcEnd)>();
        if (charIds is not null)
        {
            for (var d = 0; d < 7; d++)
            {
                var day = weekStart.AddDays(d);
                fpyPeriods.Add((d,
                    TimeZoneInfo.ConvertTimeToUtc(day, tz),
                    TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1), tz)));
            }
        }
        var fpyByDay = charIds is not null
            ? await BatchComputeFpyAsync(wcId, plantId, fpyPeriods, charIds, operatorId, ct)
            : new Dictionary<int, (decimal? fpy, int defectCount)>();

        var rows = new List<PerformanceTableRowDto>();
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        for (var d = 0; d < 7; d++)
        {
            var day = weekStart.AddDays(d);
            var dayStart = TimeZoneInfo.ConvertTimeToUtc(day, tz);
            var dayEnd = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1), tz);
            var actual = dailyActual.GetValueOrDefault(day);

            var plannedMinutes = schedule?.GetPlannedMinutes(day.DayOfWeek) ?? 0;
            var dailyPlanned = targetUph.HasValue && plannedMinutes > 0
                ? Math.Floor(targetUph.Value * (plannedMinutes / 60m))
                : (decimal?)null;

            var downtimeMin = allDowntimeEvents
                .Where(e => e.StartedAt < dayEnd && e.EndedAt > dayStart)
                .Sum(e => e.DurationMinutes);

            decimal? fpy = charIds is not null ? fpyByDay.GetValueOrDefault(d).fpy : null;

            rows.Add(new PerformanceTableRowDto
            {
                Label = dayNames[d],
                Planned = dailyPlanned,
                Actual = actual,
                Delta = dailyPlanned.HasValue ? actual - dailyPlanned.Value : null,
                Fpy = fpy,
                DowntimeMinutes = Math.Round(downtimeMin, 1),
            });
        }

        return BuildResponse(rows);
    }

    private async Task<PerformanceTableResponseDto> BuildMonthTableAsync(
        Guid wcId, Guid plantId, DateTime localDate, TimeZoneInfo tz,
        Guid[]? charIds, List<Guid> wcplIds, decimal? targetUph,
        Guid? operatorId, CancellationToken ct)
    {
        var monthStart = new DateTime(localDate.Year, localDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var utcMonthStart = TimeZoneInfo.ConvertTimeToUtc(monthStart, tz);
        var utcMonthEnd = TimeZoneInfo.ConvertTimeToUtc(monthEnd, tz);

        var schedule = await _db.ShiftSchedules
            .Where(s => s.PlantId == plantId && s.EffectiveDate <= DateOnly.FromDateTime(localDate))
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        var baseQuery = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= utcMonthStart && r.Timestamp < utcMonthEnd);
        if (operatorId.HasValue)
            baseQuery = baseQuery.Where(r => r.OperatorId == operatorId.Value);

        var records = await baseQuery
            .Select(r => new { r.Timestamp, r.SerialNumberId })
            .ToListAsync(ct);

        // Group days in the month by ISO week number
        var weeks = new SortedDictionary<int, List<DateTime>>();
        for (var d = monthStart; d < monthEnd; d = d.AddDays(1))
        {
            var isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(d);
            if (!weeks.ContainsKey(isoWeek))
                weeks[isoWeek] = new List<DateTime>();
            weeks[isoWeek].Add(d);
        }

        var allDowntimeEvents = wcplIds.Count > 0
            ? await _db.DowntimeEvents
                .Where(e => wcplIds.Contains(e.WorkCenterProductionLineId)
                            && e.StartedAt < utcMonthEnd && e.EndedAt > utcMonthStart
                            && (e.DowntimeReasonId == null || e.DowntimeReason!.CountsAsDowntime))
                .Select(e => new { e.StartedAt, e.EndedAt, e.DurationMinutes })
                .ToListAsync(ct)
            : new();

        var fpyPeriods = new List<(int key, DateTime utcStart, DateTime utcEnd)>();
        if (charIds is not null)
        {
            foreach (var (wn, wDays) in weeks)
            {
                fpyPeriods.Add((wn,
                    TimeZoneInfo.ConvertTimeToUtc(wDays.Min(), tz),
                    TimeZoneInfo.ConvertTimeToUtc(wDays.Max().AddDays(1), tz)));
            }
        }
        var fpyByWeek = charIds is not null
            ? await BatchComputeFpyAsync(wcId, plantId, fpyPeriods, charIds, operatorId, ct)
            : new Dictionary<int, (decimal? fpy, int defectCount)>();

        var rows = new List<PerformanceTableRowDto>();
        foreach (var (weekNum, days) in weeks)
        {
            var weekStartLocal = days.Min();
            var weekEndLocal = days.Max().AddDays(1);
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(weekStartLocal, tz);
            var utcEnd = TimeZoneInfo.ConvertTimeToUtc(weekEndLocal, tz);

            var actual = records
                .Count(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd);

            decimal? weekPlanned = null;
            if (targetUph.HasValue && schedule != null)
            {
                decimal sum = 0;
                foreach (var day in days)
                {
                    var pm = schedule.GetPlannedMinutes(day.DayOfWeek);
                    if (pm > 0)
                        sum += targetUph.Value * (pm / 60m);
                }
                weekPlanned = Math.Floor(sum);
            }

            var downtimeMin = allDowntimeEvents
                .Where(e => e.StartedAt < utcEnd && e.EndedAt > utcStart)
                .Sum(e => e.DurationMinutes);

            decimal? fpy = charIds is not null ? fpyByWeek.GetValueOrDefault(weekNum).fpy : null;

            rows.Add(new PerformanceTableRowDto
            {
                Label = $"Week {weekNum}",
                Planned = weekPlanned,
                Actual = actual,
                Delta = weekPlanned.HasValue ? actual - weekPlanned.Value : null,
                Fpy = fpy,
                DowntimeMinutes = Math.Round(downtimeMin, 1),
            });
        }

        return BuildResponse(rows);
    }

    private async Task<Dictionary<int, decimal>> GetDowntimeByHourAsync(
        List<Guid> wcplIds, DateTime utcDayStart, DateTime utcDayEnd,
        TimeZoneInfo tz, CancellationToken ct)
    {
        if (wcplIds.Count == 0)
            return new Dictionary<int, decimal>();

        var events = await _db.DowntimeEvents
            .Where(e => wcplIds.Contains(e.WorkCenterProductionLineId)
                        && e.StartedAt < utcDayEnd && e.EndedAt > utcDayStart
                        && (e.DowntimeReasonId == null || e.DowntimeReason!.CountsAsDowntime))
            .Select(e => new { e.StartedAt, e.EndedAt })
            .ToListAsync(ct);

        var result = new Dictionary<int, decimal>();
        foreach (var evt in events)
        {
            var clampedStart = evt.StartedAt < utcDayStart ? utcDayStart : evt.StartedAt;
            var clampedEnd = evt.EndedAt > utcDayEnd ? utcDayEnd : evt.EndedAt;

            var localStart = TimeZoneInfo.ConvertTimeFromUtc(clampedStart, tz);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(clampedEnd, tz);

            // Distribute minutes across the hours the event spans
            var cursor = localStart;
            while (cursor < localEnd)
            {
                var hourEnd = new DateTime(cursor.Year, cursor.Month, cursor.Day, cursor.Hour, 0, 0).AddHours(1);
                var bucketEnd = hourEnd < localEnd ? hourEnd : localEnd;
                var minutes = (decimal)(bucketEnd - cursor).TotalMinutes;
                var hour = cursor.Hour;

                result[hour] = result.GetValueOrDefault(hour) + Math.Round(minutes, 1);
                cursor = bucketEnd;
            }
        }

        return result;
    }

    private static PerformanceTableResponseDto BuildResponse(List<PerformanceTableRowDto> rows)
    {
        var totalPlanned = rows.Any(r => r.Planned.HasValue) ? rows.Sum(r => r.Planned ?? 0) : (decimal?)null;
        var totalActual = rows.Sum(r => r.Actual);

        var fpyRows = rows.Where(r => r.Fpy.HasValue && r.Actual > 0).ToList();
        decimal? totalFpy = fpyRows.Count > 0
            ? Math.Round(fpyRows.Sum(r => r.Fpy!.Value * r.Actual) / fpyRows.Sum(r => r.Actual), 1)
            : null;

        return new PerformanceTableResponseDto
        {
            Rows = rows,
            TotalRow = new PerformanceTableRowDto
            {
                Label = "Total",
                Planned = totalPlanned.HasValue ? Math.Floor(totalPlanned.Value) : null,
                Actual = totalActual,
                Delta = totalPlanned.HasValue ? totalActual - Math.Floor(totalPlanned.Value) : null,
                Fpy = totalFpy,
                DowntimeMinutes = Math.Round(rows.Sum(r => r.DowntimeMinutes), 1),
            },
        };
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

    private async Task<Dictionary<TKey, (decimal? fpy, int defectCount)>> BatchComputeFpyAsync<TKey>(
        Guid wcId, Guid plantId,
        List<(TKey key, DateTime utcStart, DateTime utcEnd)> periods,
        Guid[] characteristicIds, Guid? operatorId,
        CancellationToken ct) where TKey : notnull
    {
        var result = new Dictionary<TKey, (decimal? fpy, int defectCount)>();
        if (periods.Count == 0)
            return result;

        var overallStart = periods.Min(p => p.utcStart);
        var overallEnd = periods.Max(p => p.utcEnd);

        var allWcRecords = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp >= overallStart
                        && r.Timestamp < overallEnd)
            .Select(r => new { r.SerialNumberId, r.Timestamp, r.OperatorId })
            .ToListAsync(ct);

        if (allWcRecords.Count == 0)
        {
            foreach (var (key, _, _) in periods)
                result[key] = (null, 0);
            return result;
        }

        // Earliest timestamp per SN at this WC (no operator filter) for first-pass determination
        var snEarliestInRange = allWcRecords
            .GroupBy(r => r.SerialNumberId)
            .ToDictionary(g => g.Key, g => g.Min(r => r.Timestamp));

        var operatorRecords = operatorId.HasValue
            ? allWcRecords.Where(r => r.OperatorId == operatorId.Value).ToList()
            : allWcRecords;

        var allOpSnIds = operatorRecords.Select(r => r.SerialNumberId).Distinct().ToList();

        if (allOpSnIds.Count == 0)
        {
            foreach (var (key, _, _) in periods)
                result[key] = (null, 0);
            return result;
        }

        // SNs with records before the batch range (no operator filter)
        var snsBeforeRange = (await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                        && r.ProductionLine.PlantId == plantId
                        && r.Timestamp < overallStart
                        && allOpSnIds.Contains(r.SerialNumberId))
            .Select(r => r.SerialNumberId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        var potentialFirstPassSnIds = allOpSnIds.Where(sn => !snsBeforeRange.Contains(sn)).ToList();

        var allDefects = potentialFirstPassSnIds.Count > 0
            ? await _db.DefectLogs
                .Where(d => potentialFirstPassSnIds.Contains(d.SerialNumberId)
                            && characteristicIds.Contains(d.CharacteristicId)
                            && d.Timestamp >= overallStart
                            && d.Timestamp < overallEnd)
                .Select(d => new { d.SerialNumberId, d.Timestamp })
                .ToListAsync(ct)
            : new();

        foreach (var (key, utcStart, utcEnd) in periods)
        {
            var periodSnIds = operatorRecords
                .Where(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd)
                .Select(r => r.SerialNumberId)
                .Distinct()
                .ToHashSet();

            var trueFirstPassSns = periodSnIds
                .Where(sn => !snsBeforeRange.Contains(sn) && snEarliestInRange[sn] >= utcStart)
                .ToHashSet();

            var opportunities = trueFirstPassSns.Count;
            if (opportunities == 0)
            {
                result[key] = (null, 0);
                continue;
            }

            var periodDefects = allDefects
                .Where(d => trueFirstPassSns.Contains(d.SerialNumberId)
                            && d.Timestamp >= utcStart && d.Timestamp < utcEnd)
                .ToList();

            var defectSnCount = periodDefects.Select(d => d.SerialNumberId).Distinct().Count();
            var totalDefects = periodDefects.Count;
            var fpy = Math.Round((decimal)(opportunities - defectSnCount) / opportunities * 100, 1);
            result[key] = (fpy, totalDefects);
        }

        return result;
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
