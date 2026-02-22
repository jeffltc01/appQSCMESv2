namespace MESv2.Api.DTOs;

public class LogAnnotationBadgeDto
{
    public string Abbreviation { get; set; } = string.Empty;
    public string Color { get; set; } = "#212529";
}

public class RollsLogEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string CoilHeatLot { get; set; } = string.Empty;
    public string? Thickness { get; set; }
    public string ShellCode { get; set; } = string.Empty;
    public int? TankSize { get; set; }
    public List<string> Welders { get; set; } = new();
    public List<LogAnnotationBadgeDto> Annotations { get; set; } = new();
}

public class FitupLogEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? HeadNo1 { get; set; }
    public string? HeadNo2 { get; set; }
    public string? ShellNo1 { get; set; }
    public string? ShellNo2 { get; set; }
    public string? ShellNo3 { get; set; }
    public string? AlphaCode { get; set; }
    public int? TankSize { get; set; }
    public List<string> Welders { get; set; } = new();
    public List<LogAnnotationBadgeDto> Annotations { get; set; } = new();
}

public class HydroLogEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Nameplate { get; set; }
    public string? AlphaCode { get; set; }
    public int? TankSize { get; set; }
    public string Operator { get; set; } = string.Empty;
    public List<string> Welders { get; set; } = new();
    public string? Result { get; set; }
    public int DefectCount { get; set; }
    public List<LogAnnotationBadgeDto> Annotations { get; set; } = new();
}

public class RtXrayLogEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ShellCode { get; set; } = string.Empty;
    public int? TankSize { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Defects { get; set; }
    public List<LogAnnotationBadgeDto> Annotations { get; set; } = new();
}

public class SpotXrayShotCountDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SpotXrayLogEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Tanks { get; set; } = string.Empty;
    public string? Inspected { get; set; }
    public int? TankSize { get; set; }
    public string Operator { get; set; } = string.Empty;
    public string? Result { get; set; }
    public string? Shots { get; set; }
    public List<LogAnnotationBadgeDto> Annotations { get; set; } = new();
}

public class SpotXrayLogResponseDto
{
    public List<SpotXrayShotCountDto> ShotCounts { get; set; } = new();
    public List<SpotXrayLogEntryDto> Entries { get; set; } = new();
}

public class CreateLogAnnotationDto
{
    public Guid ProductionRecordId { get; set; }
    public Guid AnnotationTypeId { get; set; }
    public string? Notes { get; set; }
    public Guid InitiatedByUserId { get; set; }
}
