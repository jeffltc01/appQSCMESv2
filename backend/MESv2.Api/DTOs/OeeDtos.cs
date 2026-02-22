namespace MESv2.Api.DTOs;

// ---- Shift Schedule ----

public class ShiftScheduleDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string EffectiveDate { get; set; } = string.Empty;
    public decimal MondayHours { get; set; }
    public int MondayBreakMinutes { get; set; }
    public decimal TuesdayHours { get; set; }
    public int TuesdayBreakMinutes { get; set; }
    public decimal WednesdayHours { get; set; }
    public int WednesdayBreakMinutes { get; set; }
    public decimal ThursdayHours { get; set; }
    public int ThursdayBreakMinutes { get; set; }
    public decimal FridayHours { get; set; }
    public int FridayBreakMinutes { get; set; }
    public decimal SaturdayHours { get; set; }
    public int SaturdayBreakMinutes { get; set; }
    public decimal SundayHours { get; set; }
    public int SundayBreakMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
}

public class CreateShiftScheduleDto
{
    public Guid PlantId { get; set; }
    public string EffectiveDate { get; set; } = string.Empty;
    public decimal MondayHours { get; set; }
    public int MondayBreakMinutes { get; set; }
    public decimal TuesdayHours { get; set; }
    public int TuesdayBreakMinutes { get; set; }
    public decimal WednesdayHours { get; set; }
    public int WednesdayBreakMinutes { get; set; }
    public decimal ThursdayHours { get; set; }
    public int ThursdayBreakMinutes { get; set; }
    public decimal FridayHours { get; set; }
    public int FridayBreakMinutes { get; set; }
    public decimal SaturdayHours { get; set; }
    public int SaturdayBreakMinutes { get; set; }
    public decimal SundayHours { get; set; }
    public int SundayBreakMinutes { get; set; }
}

public class UpdateShiftScheduleDto
{
    public decimal MondayHours { get; set; }
    public int MondayBreakMinutes { get; set; }
    public decimal TuesdayHours { get; set; }
    public int TuesdayBreakMinutes { get; set; }
    public decimal WednesdayHours { get; set; }
    public int WednesdayBreakMinutes { get; set; }
    public decimal ThursdayHours { get; set; }
    public int ThursdayBreakMinutes { get; set; }
    public decimal FridayHours { get; set; }
    public int FridayBreakMinutes { get; set; }
    public decimal SaturdayHours { get; set; }
    public int SaturdayBreakMinutes { get; set; }
    public decimal SundayHours { get; set; }
    public int SundayBreakMinutes { get; set; }
}

// ---- Capacity Targets ----

public class WorkCenterCapacityTargetDto
{
    public Guid Id { get; set; }
    public Guid WorkCenterProductionLineId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public string ProductionLineName { get; set; } = string.Empty;
    public int? TankSize { get; set; }
    public Guid PlantGearId { get; set; }
    public int GearLevel { get; set; }
    public decimal TargetUnitsPerHour { get; set; }
}

public class CreateWorkCenterCapacityTargetDto
{
    public Guid WorkCenterProductionLineId { get; set; }
    public int? TankSize { get; set; }
    public Guid PlantGearId { get; set; }
    public decimal TargetUnitsPerHour { get; set; }
}

public class UpdateWorkCenterCapacityTargetDto
{
    public decimal TargetUnitsPerHour { get; set; }
}

// ---- OEE Results ----

public class OeeMetricsDto
{
    public decimal? Availability { get; set; }
    public decimal? Performance { get; set; }
    public decimal? Quality { get; set; }
    public decimal? Oee { get; set; }
    public decimal? PlannedMinutes { get; set; }
    public decimal? DowntimeMinutes { get; set; }
    public decimal? RunTimeMinutes { get; set; }
}
