namespace MESv2.Api.DTOs;

public class NaturalLanguageQueryRequestDto
{
    public string Question { get; set; } = string.Empty;
    public NaturalLanguageQueryContextDto? Context { get; set; }
}

public class NaturalLanguageQueryContextDto
{
    public Guid? PlantId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? OperatorId { get; set; }
    public string? Date { get; set; }
    public string? View { get; set; }
}

public class NaturalLanguageQueryResponseDto
{
    public string AnswerText { get; set; } = string.Empty;
    public string ScopeUsed { get; set; } = "unknown";
    public decimal Confidence { get; set; }
    public List<NaturalLanguageQueryDataPointDto> DataPoints { get; set; } = new();
    public List<string> FollowUps { get; set; } = new();
    public NaturalLanguageQueryTraceDto Trace { get; set; } = new();
}

public class NaturalLanguageQueryDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
}

public class NaturalLanguageQueryTraceDto
{
    public string Intent { get; set; } = "Unknown";
    public bool UsedModel { get; set; }
    public bool UsedCache { get; set; }
    public long DurationMs { get; set; }
}
