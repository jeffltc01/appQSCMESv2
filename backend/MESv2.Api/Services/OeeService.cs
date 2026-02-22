using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class OeeService : IOeeService
{
    private readonly MesDbContext _db;
    private readonly ILogger<OeeService> _logger;

    public OeeService(MesDbContext db, ILogger<OeeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ---- Shift Schedules ----

    public async Task<IReadOnlyList<ShiftScheduleDto>> GetShiftSchedulesAsync(
        Guid plantId, CancellationToken ct = default)
    {
        return await _db.ShiftSchedules
            .Where(s => s.PlantId == plantId)
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => MapScheduleDto(s))
            .ToListAsync(ct);
    }

    public async Task<ShiftScheduleDto> CreateShiftScheduleAsync(
        CreateShiftScheduleDto dto, Guid? userId, CancellationToken ct = default)
    {
        if (!DateOnly.TryParse(dto.EffectiveDate, out var effectiveDate))
            throw new ArgumentException("Invalid EffectiveDate format.");

        var entity = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            PlantId = dto.PlantId,
            EffectiveDate = effectiveDate,
            MondayHours = dto.MondayHours,
            MondayBreakMinutes = dto.MondayBreakMinutes,
            TuesdayHours = dto.TuesdayHours,
            TuesdayBreakMinutes = dto.TuesdayBreakMinutes,
            WednesdayHours = dto.WednesdayHours,
            WednesdayBreakMinutes = dto.WednesdayBreakMinutes,
            ThursdayHours = dto.ThursdayHours,
            ThursdayBreakMinutes = dto.ThursdayBreakMinutes,
            FridayHours = dto.FridayHours,
            FridayBreakMinutes = dto.FridayBreakMinutes,
            SaturdayHours = dto.SaturdayHours,
            SaturdayBreakMinutes = dto.SaturdayBreakMinutes,
            SundayHours = dto.SundayHours,
            SundayBreakMinutes = dto.SundayBreakMinutes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
        };

        _db.ShiftSchedules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return MapScheduleDto(entity);
    }

    public async Task<ShiftScheduleDto?> UpdateShiftScheduleAsync(
        Guid id, UpdateShiftScheduleDto dto, CancellationToken ct = default)
    {
        var entity = await _db.ShiftSchedules.FindAsync(new object[] { id }, ct);
        if (entity == null) return null;

        entity.MondayHours = dto.MondayHours;
        entity.MondayBreakMinutes = dto.MondayBreakMinutes;
        entity.TuesdayHours = dto.TuesdayHours;
        entity.TuesdayBreakMinutes = dto.TuesdayBreakMinutes;
        entity.WednesdayHours = dto.WednesdayHours;
        entity.WednesdayBreakMinutes = dto.WednesdayBreakMinutes;
        entity.ThursdayHours = dto.ThursdayHours;
        entity.ThursdayBreakMinutes = dto.ThursdayBreakMinutes;
        entity.FridayHours = dto.FridayHours;
        entity.FridayBreakMinutes = dto.FridayBreakMinutes;
        entity.SaturdayHours = dto.SaturdayHours;
        entity.SaturdayBreakMinutes = dto.SaturdayBreakMinutes;
        entity.SundayHours = dto.SundayHours;
        entity.SundayBreakMinutes = dto.SundayBreakMinutes;

        await _db.SaveChangesAsync(ct);
        return MapScheduleDto(entity);
    }

    public async Task<bool> DeleteShiftScheduleAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ShiftSchedules.FindAsync(new object[] { id }, ct);
        if (entity == null) return false;
        _db.ShiftSchedules.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---- Capacity Targets ----

    public async Task<IReadOnlyList<WorkCenterCapacityTargetDto>> GetCapacityTargetsAsync(
        Guid plantId, CancellationToken ct = default)
    {
        return await _db.WorkCenterCapacityTargets
            .Include(t => t.WorkCenterProductionLine).ThenInclude(wcpl => wcpl.WorkCenter)
            .Include(t => t.WorkCenterProductionLine).ThenInclude(wcpl => wcpl.ProductionLine)
            .Include(t => t.PlantGear)
            .Where(t => t.WorkCenterProductionLine.ProductionLine.PlantId == plantId)
            .OrderBy(t => t.WorkCenterProductionLine.WorkCenter.Name)
            .ThenBy(t => t.PlantGear.Level)
            .ThenBy(t => t.TankSize)
            .Select(t => new WorkCenterCapacityTargetDto
            {
                Id = t.Id,
                WorkCenterProductionLineId = t.WorkCenterProductionLineId,
                WorkCenterName = t.WorkCenterProductionLine.WorkCenter.Name,
                ProductionLineName = t.WorkCenterProductionLine.ProductionLine.Name,
                TankSize = t.TankSize,
                PlantGearId = t.PlantGearId,
                GearLevel = t.PlantGear.Level,
                TargetUnitsPerHour = t.TargetUnitsPerHour,
            })
            .ToListAsync(ct);
    }

    public async Task<WorkCenterCapacityTargetDto> CreateCapacityTargetAsync(
        CreateWorkCenterCapacityTargetDto dto, CancellationToken ct = default)
    {
        var entity = new WorkCenterCapacityTarget
        {
            Id = Guid.NewGuid(),
            WorkCenterProductionLineId = dto.WorkCenterProductionLineId,
            TankSize = dto.TankSize,
            PlantGearId = dto.PlantGearId,
            TargetUnitsPerHour = dto.TargetUnitsPerHour,
        };

        _db.WorkCenterCapacityTargets.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(entity).Reference(e => e.WorkCenterProductionLine).LoadAsync(ct);
        await _db.Entry(entity.WorkCenterProductionLine).Reference(w => w.WorkCenter).LoadAsync(ct);
        await _db.Entry(entity.WorkCenterProductionLine).Reference(w => w.ProductionLine).LoadAsync(ct);
        await _db.Entry(entity).Reference(e => e.PlantGear).LoadAsync(ct);

        return new WorkCenterCapacityTargetDto
        {
            Id = entity.Id,
            WorkCenterProductionLineId = entity.WorkCenterProductionLineId,
            WorkCenterName = entity.WorkCenterProductionLine.WorkCenter.Name,
            ProductionLineName = entity.WorkCenterProductionLine.ProductionLine.Name,
            TankSize = entity.TankSize,
            PlantGearId = entity.PlantGearId,
            GearLevel = entity.PlantGear.Level,
            TargetUnitsPerHour = entity.TargetUnitsPerHour,
        };
    }

    public async Task<WorkCenterCapacityTargetDto?> UpdateCapacityTargetAsync(
        Guid id, UpdateWorkCenterCapacityTargetDto dto, CancellationToken ct = default)
    {
        var entity = await _db.WorkCenterCapacityTargets
            .Include(t => t.WorkCenterProductionLine).ThenInclude(w => w.WorkCenter)
            .Include(t => t.WorkCenterProductionLine).ThenInclude(w => w.ProductionLine)
            .Include(t => t.PlantGear)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity == null) return null;

        entity.TargetUnitsPerHour = dto.TargetUnitsPerHour;
        await _db.SaveChangesAsync(ct);

        return new WorkCenterCapacityTargetDto
        {
            Id = entity.Id,
            WorkCenterProductionLineId = entity.WorkCenterProductionLineId,
            WorkCenterName = entity.WorkCenterProductionLine.WorkCenter.Name,
            ProductionLineName = entity.WorkCenterProductionLine.ProductionLine.Name,
            TankSize = entity.TankSize,
            PlantGearId = entity.PlantGearId,
            GearLevel = entity.PlantGear.Level,
            TargetUnitsPerHour = entity.TargetUnitsPerHour,
        };
    }

    public async Task<bool> DeleteCapacityTargetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.WorkCenterCapacityTargets.FindAsync(new object[] { id }, ct);
        if (entity == null) return false;
        _db.WorkCenterCapacityTargets.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<WorkCenterCapacityTargetDto>> BulkUpsertCapacityTargetsAsync(
        BulkUpsertCapacityTargetsDto dto, CancellationToken ct = default)
    {
        var wcplIds = await _db.WorkCenterProductionLines
            .Where(w => w.ProductionLineId == dto.ProductionLineId)
            .Select(w => w.Id)
            .ToListAsync(ct);

        if (wcplIds.Count == 0)
            return Array.Empty<WorkCenterCapacityTargetDto>();

        var existing = await _db.WorkCenterCapacityTargets
            .Where(t => wcplIds.Contains(t.WorkCenterProductionLineId))
            .ToListAsync(ct);

        var incomingKeys = dto.Targets
            .Select(t => (t.WorkCenterProductionLineId, t.PlantGearId, t.TankSize))
            .ToHashSet();

        // Delete targets not present in payload
        var toDelete = existing
            .Where(e => !incomingKeys.Contains((e.WorkCenterProductionLineId, e.PlantGearId, e.TankSize)))
            .ToList();
        _db.WorkCenterCapacityTargets.RemoveRange(toDelete);

        // Upsert targets from payload
        foreach (var item in dto.Targets)
        {
            var match = existing.FirstOrDefault(e =>
                e.WorkCenterProductionLineId == item.WorkCenterProductionLineId
                && e.PlantGearId == item.PlantGearId
                && e.TankSize == item.TankSize);

            if (match != null)
            {
                match.TargetUnitsPerHour = item.TargetUnitsPerHour;
            }
            else
            {
                _db.WorkCenterCapacityTargets.Add(new WorkCenterCapacityTarget
                {
                    Id = Guid.NewGuid(),
                    WorkCenterProductionLineId = item.WorkCenterProductionLineId,
                    PlantGearId = item.PlantGearId,
                    TankSize = item.TankSize,
                    TargetUnitsPerHour = item.TargetUnitsPerHour,
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        // Return refreshed list for the plant
        var plantId = await _db.ProductionLines
            .Where(pl => pl.Id == dto.ProductionLineId)
            .Select(pl => pl.PlantId)
            .FirstOrDefaultAsync(ct);

        return await GetCapacityTargetsAsync(plantId, ct);
    }

    public async Task<IReadOnlyList<int>> GetDistinctTankSizesAsync(
        Guid plantId, CancellationToken ct = default)
    {
        return await _db.ProductPlants
            .Where(pp => pp.PlantId == plantId)
            .Select(pp => pp.Product.TankSize)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);
    }

    // ---- OEE Calculation ----

    public async Task<OeeMetricsDto> CalculateOeeAsync(
        Guid wcId, Guid plantId, string date, CancellationToken ct = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, ct);
        var localDate = dateParsed.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);
        var dateOnly = DateOnly.FromDateTime(localDate);
        var dayOfWeek = localDate.DayOfWeek;

        // 1. Find the active shift schedule
        var schedule = await _db.ShiftSchedules
            .Where(s => s.PlantId == plantId && s.EffectiveDate <= dateOnly)
            .OrderByDescending(s => s.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        if (schedule == null)
            return new OeeMetricsDto();

        var plannedMinutes = schedule.GetPlannedMinutes(dayOfWeek);
        if (plannedMinutes <= 0)
            return new OeeMetricsDto { PlannedMinutes = 0 };

        // 2. Get WC production line IDs for this WC at this plant
        var wcplIds = await _db.WorkCenterProductionLines
            .Where(wcpl => wcpl.WorkCenterId == wcId && wcpl.ProductionLine.PlantId == plantId)
            .Select(wcpl => wcpl.Id)
            .ToListAsync(ct);

        // 3. Sum downtime for the day across all production lines of this WC
        //    Only include events whose reason counts as downtime (or has no reason, e.g. auto-generated)
        var downtimeMinutes = wcplIds.Count > 0
            ? await _db.DowntimeEvents
                .Where(e => wcplIds.Contains(e.WorkCenterProductionLineId)
                    && e.StartedAt < endOfDay
                    && e.EndedAt > startOfDay
                    && (e.DowntimeReasonId == null || e.DowntimeReason!.CountsAsDowntime))
                .SumAsync(e => e.DurationMinutes, ct)
            : 0m;

        var runTimeMinutes = Math.Max(0, plannedMinutes - downtimeMinutes);

        // Availability
        var availability = runTimeMinutes / plannedMinutes;

        // 4. Get production records for the day with tank sizes and gear IDs
        var records = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId
                && r.ProductionLine.PlantId == plantId
                && r.Timestamp >= startOfDay
                && r.Timestamp < endOfDay)
            .Select(r => new
            {
                TankSize = r.SerialNumber.Product != null ? (int?)r.SerialNumber.Product.TankSize : null,
                r.PlantGearId,
                r.ProductionLineId,
            })
            .ToListAsync(ct);

        // 5. Load all capacity targets for this WC's production lines
        var capacityTargets = wcplIds.Count > 0
            ? await _db.WorkCenterCapacityTargets
                .Where(t => wcplIds.Contains(t.WorkCenterProductionLineId))
                .ToListAsync(ct)
            : new List<WorkCenterCapacityTarget>();

        // Map ProductionLineId -> WorkCenterProductionLineId
        var plToWcpl = await _db.WorkCenterProductionLines
            .Where(wcpl => wcpl.WorkCenterId == wcId && wcpl.ProductionLine.PlantId == plantId)
            .ToDictionaryAsync(wcpl => wcpl.ProductionLineId, wcpl => wcpl.Id, ct);

        // 6. Calculate Performance: sum of ideal cycle times / run time
        decimal idealTimeSum = 0;
        int matchedRecords = 0;
        foreach (var rec in records)
        {
            if (rec.PlantGearId == null) continue;

            plToWcpl.TryGetValue(rec.ProductionLineId, out var wcplId);
            if (wcplId == Guid.Empty) continue;

            // Look for exact match first, then fall back to null TankSize default
            var target = capacityTargets.FirstOrDefault(t =>
                    t.WorkCenterProductionLineId == wcplId
                    && t.PlantGearId == rec.PlantGearId
                    && t.TankSize == rec.TankSize)
                ?? capacityTargets.FirstOrDefault(t =>
                    t.WorkCenterProductionLineId == wcplId
                    && t.PlantGearId == rec.PlantGearId
                    && t.TankSize == null);

            if (target != null && target.TargetUnitsPerHour > 0)
            {
                idealTimeSum += 60m / target.TargetUnitsPerHour;
                matchedRecords++;
            }
        }

        decimal? performance = null;
        if (runTimeMinutes > 0 && matchedRecords > 0)
            performance = Math.Min(idealTimeSum / runTimeMinutes, 2.0m);

        // 7. Quality = FPY (already computed by SupervisorDashboardService, passed in from caller)
        //    We return null here and let the caller merge it in.

        // 8. OEE = A * P * Q (caller multiplies quality in)
        decimal? oee = null;
        if (performance.HasValue)
            oee = availability * performance.Value * 100m;

        return new OeeMetricsDto
        {
            Availability = Math.Round(availability * 100, 1),
            Performance = performance.HasValue ? Math.Round(performance.Value * 100, 1) : null,
            Quality = null,
            Oee = null,
            PlannedMinutes = Math.Round(plannedMinutes, 1),
            DowntimeMinutes = Math.Round(downtimeMinutes, 1),
            RunTimeMinutes = Math.Round(runTimeMinutes, 1),
        };
    }

    // ---- Private helpers ----

    private static ShiftScheduleDto MapScheduleDto(ShiftSchedule s) => new()
    {
        Id = s.Id,
        PlantId = s.PlantId,
        EffectiveDate = s.EffectiveDate.ToString("yyyy-MM-dd"),
        MondayHours = s.MondayHours,
        MondayBreakMinutes = s.MondayBreakMinutes,
        TuesdayHours = s.TuesdayHours,
        TuesdayBreakMinutes = s.TuesdayBreakMinutes,
        WednesdayHours = s.WednesdayHours,
        WednesdayBreakMinutes = s.WednesdayBreakMinutes,
        ThursdayHours = s.ThursdayHours,
        ThursdayBreakMinutes = s.ThursdayBreakMinutes,
        FridayHours = s.FridayHours,
        FridayBreakMinutes = s.FridayBreakMinutes,
        SaturdayHours = s.SaturdayHours,
        SaturdayBreakMinutes = s.SaturdayBreakMinutes,
        SundayHours = s.SundayHours,
        SundayBreakMinutes = s.SundayBreakMinutes,
        CreatedAt = s.CreatedAt,
        CreatedByName = s.CreatedByUser?.DisplayName,
    };

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(Guid plantId, CancellationToken ct)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrEmpty(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
