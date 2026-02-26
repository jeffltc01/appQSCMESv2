namespace MESv2.Api.DTOs;

public class SupervisorDashboardMetricsDto
{
    public int DayCount { get; set; }
    public int WeekCount { get; set; }
    public int MonthCount { get; set; }
    public bool SupportsFirstPassYield { get; set; }
    public decimal? DayFPY { get; set; }
    public decimal? WeekFPY { get; set; }
    public decimal? MonthFPY { get; set; }
    public int DayDefects { get; set; }
    public int WeekDefects { get; set; }
    public int MonthDefects { get; set; }
    public decimal DayDowntimeMinutes { get; set; }
    public decimal WeekDowntimeMinutes { get; set; }
    public decimal MonthDowntimeMinutes { get; set; }
    public decimal DayQtyPerHour { get; set; }
    public decimal WeekQtyPerHour { get; set; }
    public decimal MonthQtyPerHour { get; set; }
    public List<HourlyCountDto> HourlyCounts { get; set; } = new();
    public List<DailyCountDto> WeekDailyCounts { get; set; } = new();
    public List<OperatorSummaryDto> Operators { get; set; } = new();

    // OEE metrics (day only, null when shift schedule or capacity targets are not configured)
    public decimal? OeeAvailability { get; set; }
    public decimal? OeePerformance { get; set; }
    public decimal? OeeQuality { get; set; }
    public decimal? OeeOverall { get; set; }
    public decimal? OeePlannedMinutes { get; set; }
    public decimal? OeeDowntimeMinutes { get; set; }
    public decimal? OeeRunTimeMinutes { get; set; }
}

public class KpiTrendPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}

public class SupervisorDashboardTrendsDto
{
    public List<KpiTrendPointDto> Count { get; set; } = new();
    public List<KpiTrendPointDto> Fpy { get; set; } = new();
    public List<KpiTrendPointDto> Defects { get; set; } = new();
    public List<KpiTrendPointDto> QtyPerHour { get; set; } = new();
    public List<KpiTrendPointDto> DowntimeMinutes { get; set; } = new();
    public List<KpiTrendPointDto> Oee { get; set; } = new();
    public List<KpiTrendPointDto> Availability { get; set; } = new();
    public List<KpiTrendPointDto> Performance { get; set; } = new();
    public List<KpiTrendPointDto> Quality { get; set; } = new();
}

public class DefectParetoItemDto
{
    public string DefectCode { get; set; } = string.Empty;
    public string DefectName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal CumulativePercent { get; set; }
}

public class DefectParetoResponseDto
{
    public int TotalDefects { get; set; }
    public List<DefectParetoItemDto> Items { get; set; } = new();
}

public class DowntimeParetoItemDto
{
    public string ReasonName { get; set; } = string.Empty;
    public decimal Minutes { get; set; }
    public decimal CumulativePercent { get; set; }
}

public class DowntimeParetoResponseDto
{
    public decimal TotalDowntimeMinutes { get; set; }
    public List<DowntimeParetoItemDto> Items { get; set; } = new();
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

public class PerformanceTableRowDto
{
    public string Label { get; set; } = string.Empty;
    public decimal? Planned { get; set; }
    public int Actual { get; set; }
    public decimal? Delta { get; set; }
    public decimal? Fpy { get; set; }
    public decimal DowntimeMinutes { get; set; }
}

public class PerformanceTableResponseDto
{
    public List<PerformanceTableRowDto> Rows { get; set; } = new();
    public PerformanceTableRowDto? TotalRow { get; set; }
}
