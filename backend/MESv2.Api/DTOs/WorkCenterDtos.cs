namespace MESv2.Api.DTOs;

public class WelderDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
}

public class WCHistoryEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string SerialOrIdentifier { get; set; } = string.Empty;
    public int? TankSize { get; set; }
    public bool HasAnnotation { get; set; }
    public string? AnnotationColor { get; set; }
}

public class WCHistoryDto
{
    public int DayCount { get; set; }
    public List<WCHistoryEntryDto> RecentRecords { get; set; } = new();
}

public class FaultReportDto
{
    public string Description { get; set; } = string.Empty;
}

public class AddWelderDto
{
    public string EmployeeNumber { get; set; } = string.Empty;
}
