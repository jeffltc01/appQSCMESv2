namespace MESv2.Api.Services;

public class MockPrivateLlmClient : INlqModelClient
{
    public Task<NlqModelInterpretation> InterpretAsync(
        string redactedQuestion,
        string? view,
        CancellationToken cancellationToken = default)
    {
        var q = redactedQuestion.ToLowerInvariant();

        if (q.Contains("downtime") || q.Contains("down"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.TopDowntimeDriversToday,
                Confidence = 0.9m,
                ScopeHint = "plant-wide",
                FollowUps =
                [
                    "Which reason category is causing the most downtime?",
                    "Which shift had the highest downtime?"
                ]
            });
        }

        if ((q.Contains("behind") || q.Contains("metric") || q.Contains("target")) && q.Contains("work center"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.WorkCentersBehindTargetToday,
                Confidence = 0.88m,
                ScopeHint = "plant-wide",
                FollowUps =
                [
                    "Show the top 3 work centers by negative delta.",
                    "How much downtime contributed to each shortfall?"
                ]
            });
        }

        if (q.Contains("how many") || q.Contains("produced") || q.Contains("count") || q.Contains("tanks"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.TanksProducedToday,
                Confidence = 0.86m,
                ScopeHint = q.Contains("plant") || q.Contains("all work centers") ? "plant-wide" : "context",
                FollowUps =
                [
                    "Break this down by work center.",
                    "How does this compare to yesterday?"
                ]
            });
        }

        if (q.Contains("performance") || q.Contains("fpy") || q.Contains("oee"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.WorkCenterPerformanceSummary,
                Confidence = 0.82m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Show me today versus week-to-date performance.",
                    "What is driving the largest gap?"
                ]
            });
        }

        return Task.FromResult(new NlqModelInterpretation
        {
            Intent = NlqIntent.Unknown,
            Confidence = 0.4m,
            ScopeHint = "context",
            FollowUps = ["Try asking about production count, behind target, downtime, or performance."]
        });
    }
}
