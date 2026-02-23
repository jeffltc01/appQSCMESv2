namespace MESv2.Api.DTOs;

public class FrontendTelemetryIngestDto
{
    public DateTime? OccurredAtUtc { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Severity { get; set; } = "error";
    public bool IsReactRuntimeOverlayCandidate { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
    public string? Route { get; set; }
    public string? Screen { get; set; }
    public string? MetadataJson { get; set; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ApiPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? HttpStatus { get; set; }
    public Guid? UserId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public Guid? PlantId { get; set; }
    public string? Fingerprint { get; set; }
}

public class FrontendTelemetryEventDto
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsReactRuntimeOverlayCandidate { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
    public string? Route { get; set; }
    public string? Screen { get; set; }
    public string? MetadataJson { get; set; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ApiPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? HttpStatus { get; set; }
    public Guid? UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public Guid? WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public Guid? ProductionLineId { get; set; }
    public string? ProductionLineName { get; set; }
    public Guid? PlantId { get; set; }
    public string? PlantName { get; set; }
}

public class FrontendTelemetryPageDto
{
    public List<FrontendTelemetryEventDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class FrontendTelemetryFilterOptionsDto
{
    public List<string> Categories { get; set; } = new();
    public List<string> Sources { get; set; } = new();
    public List<string> Severities { get; set; } = new();
}

public class FrontendTelemetryCountDto
{
    public long RowCount { get; set; }
    public long WarningThreshold { get; set; }
    public bool IsWarning => RowCount >= WarningThreshold;
}

public class FrontendTelemetryArchiveRequestDto
{
    public int KeepRows { get; set; } = 250_000;
}

public class FrontendTelemetryArchiveResultDto
{
    public int DeletedRows { get; set; }
    public long RemainingRows { get; set; }
}
