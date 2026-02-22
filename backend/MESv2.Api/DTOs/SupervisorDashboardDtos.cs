namespace MESv2.Api.DTOs;

public class SupervisorDashboardMetricsDto
{
    public int DayCount { get; set; }
    public int WeekCount { get; set; }
    public bool SupportsFirstPassYield { get; set; }
    public decimal? DayFPY { get; set; }
    public decimal? WeekFPY { get; set; }
    public int DayDefects { get; set; }
    public int WeekDefects { get; set; }
    public double DayAvgTimeBetweenScans { get; set; }
    public double WeekAvgTimeBetweenScans { get; set; }
    public decimal DayQtyPerHour { get; set; }
    public decimal WeekQtyPerHour { get; set; }
    public List<HourlyCountDto> HourlyCounts { get; set; } = new();
    public List<DailyCountDto> WeekDailyCounts { get; set; } = new();
    public List<OperatorSummaryDto> Operators { get; set; } = new();
}

public class HourlyCountDto
{
    public int Hour { get; set; }
    public int Count { get; set; }
}

public class DailyCountDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OperatorSummaryDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

public class SupervisorRecordDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string SerialOrIdentifier { get; set; } = string.Empty;
    public string? TankSize { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public List<ExistingAnnotationDto> Annotations { get; set; } = new();
}

public class ExistingAnnotationDto
{
    public Guid AnnotationTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public string? DisplayColor { get; set; }
}

public class CreateSupervisorAnnotationRequest
{
    public List<Guid> RecordIds { get; set; } = new();
    public Guid AnnotationTypeId { get; set; }
    public string? Comment { get; set; }
}

public class SupervisorAnnotationResultDto
{
    public int AnnotationsCreated { get; set; }
}
