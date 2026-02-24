namespace MESv2.Api.Services;

public class MockPrivateLlmClient : INlqModelClient
{
    public Task<NlqModelInterpretation> InterpretAsync(
        string redactedQuestion,
        string? view,
        CancellationToken cancellationToken = default)
    {
        var q = redactedQuestion.ToLowerInvariant();

        if ((q.Contains("how many") || q.Contains("count") || q.Contains("total")) &&
            (q.Contains("filter") || q.Contains("search") || q.Contains("matching") || q.Contains("current screen")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.CurrentScreenFilteredRecordCount,
                Confidence = 0.93m,
                ScopeHint = "context",
                FollowUps =
                [
                    "What filters are currently applied?",
                    "Show this total after I change the date range."
                ]
            });
        }

        if (q.Contains("defect") && (q.Contains("hotspot") || q.Contains("highest") || q.Contains("most")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.DefectHotspotsToday,
                Confidence = 0.9m,
                ScopeHint = "plant-wide",
                FollowUps =
                [
                    "Which defect locations are highest today?",
                    "Which work center is driving most defects?"
                ]
            });
        }

        if (q.Contains("fpy") && (q.Contains("trend") || q.Contains("week") || q.Contains("improving")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.FirstPassYieldTrend,
                Confidence = 0.9m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Show daily FPY for this week.",
                    "Compare this week to last week."
                ]
            });
        }

        if ((q.Contains("operator") || q.Contains("top") || q.Contains("bottom")) &&
            (q.Contains("performance") || q.Contains("outlier") || q.Contains("below")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.OperatorOutlierPerformance,
                Confidence = 0.89m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Who has the lowest qty/hour today?",
                    "Who is most above expected pace?"
                ]
            });
        }

        if (q.Contains("downtime") && q.Contains("asset"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.DowntimeByAsset,
                Confidence = 0.92m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Which machine has the highest downtime minutes?",
                    "Show downtime by asset for this shift."
                ]
            });
        }

        if (q.Contains("downtime") &&
            (q.Contains("uncoded") || q.Contains("uncategorized") || q.Contains("no reason") || q.Contains("missing reason")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.DowntimeUncodedEvents,
                Confidence = 0.92m,
                ScopeHint = "plant-wide",
                FollowUps =
                [
                    "How many uncoded events are open?",
                    "What minutes are uncoded today?"
                ]
            });
        }

        if (q.Contains("bottleneck") || q.Contains("constraining") || q.Contains("constraint"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.BottleneckWorkCenterNow,
                Confidence = 0.88m,
                ScopeHint = "context",
                FollowUps =
                [
                    "How much WIP is at the bottleneck?",
                    "Which station is most likely causing delay?"
                ]
            });
        }

        if (q.Contains("queue") && (q.Contains("backlog") || q.Contains("risk") || q.Contains("backing up") || q.Contains("wip")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.QueueBacklogRisk,
                Confidence = 0.87m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Which queue is growing fastest?",
                    "Which station feed is at risk?"
                ]
            });
        }

        if ((q.Contains("target") && q.Contains("shift")) || q.Contains("miss target"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.TargetAtRiskByShift,
                Confidence = 0.9m,
                ScopeHint = "context",
                FollowUps =
                [
                    "How far behind plan is this shift?",
                    "What is the projected shortfall by end of shift?"
                ]
            });
        }

        if ((q.Contains("cycle") || q.Contains("scan gap") || q.Contains("between scans")) && (q.Contains("anomal") || q.Contains("longest") || q.Contains("unusual")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.CycleTimeAnomalies,
                Confidence = 0.87m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Show longest scan gaps today.",
                    "Which periods had abnormal cycle time?"
                ]
            });
        }

        if (q.Contains("annotation") || q.Contains("follow-up") || q.Contains("correction needed"))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.AnnotationFollowUpNeeded,
                Confidence = 0.87m,
                ScopeHint = "context",
                FollowUps =
                [
                    "How many correction-needed items are still open?",
                    "Which records need supervisor follow-up?"
                ]
            });
        }

        if ((q.Contains("quality") && q.Contains("throughput")) || (q.Contains("high output") && q.Contains("low fpy")))
        {
            return Task.FromResult(new NlqModelInterpretation
            {
                Intent = NlqIntent.QualityVsThroughputTradeoff,
                Confidence = 0.86m,
                ScopeHint = "context",
                FollowUps =
                [
                    "Where is output high but quality low?",
                    "Which work centers show quality tradeoff?"
                ]
            });
        }

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
            FollowUps =
            [
                "Try asking about defect hotspots, FPY trend, downtime by asset, bottlenecks, queue risk, or filtered record count.",
            ]
        });
    }
}
