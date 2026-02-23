namespace MESv2.Api.Services;

public interface INlqModelClient
{
    Task<NlqModelInterpretation> InterpretAsync(
        string redactedQuestion,
        string? view,
        CancellationToken cancellationToken = default);
}

public enum NlqIntent
{
    Unknown = 0,
    TanksProducedToday = 1,
    WorkCentersBehindTargetToday = 2,
    TopDowntimeDriversToday = 3,
    WorkCenterPerformanceSummary = 4,
}

public sealed class NlqModelInterpretation
{
    public NlqIntent Intent { get; init; } = NlqIntent.Unknown;
    public decimal Confidence { get; init; } = 0.5m;
    public string ScopeHint { get; init; } = "context";
    public List<string> FollowUps { get; init; } = new();
}
