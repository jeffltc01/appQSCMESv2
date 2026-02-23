namespace MESv2.Api.Models;

public class FrontendTelemetryEvent
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsReactRuntimeOverlayCandidate { get; set; }

    public string? Route { get; set; }
    public string? Screen { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
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

    public User? User { get; set; }
    public WorkCenter? WorkCenter { get; set; }
    public ProductionLine? ProductionLine { get; set; }
    public Plant? Plant { get; set; }
}
