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
    DefectHotspotsToday = 5,
    FirstPassYieldTrend = 6,
    OperatorOutlierPerformance = 7,
    DowntimeByAsset = 8,
    DowntimeUncodedEvents = 9,
    BottleneckWorkCenterNow = 10,
    QueueBacklogRisk = 11,
    TargetAtRiskByShift = 12,
    CycleTimeAnomalies = 13,
    AnnotationFollowUpNeeded = 14,
    QualityVsThroughputTradeoff = 15,
    CurrentScreenFilteredRecordCount = 16,
}

public sealed class NlqModelInterpretation
{
    public NlqIntent Intent { get; init; } = NlqIntent.Unknown;
    public decimal Confidence { get; init; } = 0.5m;
    public string ScopeHint { get; init; } = "context";
    public List<string> FollowUps { get; init; } = new();
}
