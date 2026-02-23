using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class FrontendTelemetryServiceTests
{
    private static (FrontendTelemetryService service, MesDbContext db) CreateService()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var service = new FrontendTelemetryService(db);
        return (service, db);
    }

    [Fact]
    public async Task IngestAsync_NormalizesAndPersists()
    {
        var (service, db) = CreateService();
        var longMessage = new string('a', 5000);

        await service.IngestAsync(new FrontendTelemetryIngestDto
        {
            Category = "",
            Source = "",
            Severity = "",
            Message = longMessage,
        }, CancellationToken.None);

        var stored = Assert.Single(db.FrontendTelemetryEvents);
        Assert.Equal("unknown", stored.Category);
        Assert.Equal("unknown", stored.Source);
        Assert.Equal("error", stored.Severity);
        Assert.Equal(2048, stored.Message.Length);
    }

    [Fact]
    public async Task GetEventsAsync_RespectsReactRuntimeFilter()
    {
        var (service, db) = CreateService();
        var now = DateTime.UtcNow;
        db.FrontendTelemetryEvents.AddRange(
            new FrontendTelemetryEvent
            {
                OccurredAtUtc = now.AddMinutes(-1),
                ReceivedAtUtc = now.AddMinutes(-1),
                Category = "runtime_error",
                Source = "window_error",
                Severity = "error",
                IsReactRuntimeOverlayCandidate = true,
                Message = "react error",
            },
            new FrontendTelemetryEvent
            {
                OccurredAtUtc = now,
                ReceivedAtUtc = now,
                Category = "api_error",
                Source = "api_client",
                Severity = "error",
                IsReactRuntimeOverlayCandidate = false,
                Message = "api failure",
            }
        );
        await db.SaveChangesAsync();

        var all = await service.GetEventsAsync(
            null, null, null, null, null, null, null, false, 1, 50, CancellationToken.None);
        var reactOnly = await service.GetEventsAsync(
            null, null, null, null, null, null, null, true, 1, 50, CancellationToken.None);

        Assert.Equal(2, all.TotalCount);
        Assert.Single(reactOnly.Items);
        Assert.True(reactOnly.Items[0].IsReactRuntimeOverlayCandidate);
    }

    [Fact]
    public async Task ArchiveOldestAsync_DeletesOldestRowsFirst()
    {
        var (service, db) = CreateService();
        var start = DateTime.UtcNow.AddMinutes(-10);

        for (var i = 0; i < 5; i++)
        {
            db.FrontendTelemetryEvents.Add(new FrontendTelemetryEvent
            {
                OccurredAtUtc = start.AddMinutes(i),
                ReceivedAtUtc = start.AddMinutes(i),
                Category = "runtime_error",
                Source = "window_error",
                Severity = "error",
                IsReactRuntimeOverlayCandidate = true,
                Message = $"event-{i}",
            });
        }
        await db.SaveChangesAsync();

        var result = await service.ArchiveOldestAsync(3, CancellationToken.None);
        var remaining = db.FrontendTelemetryEvents
            .OrderBy(e => e.OccurredAtUtc)
            .Select(e => e.Message)
            .ToList();

        Assert.Equal(2, result.DeletedRows);
        Assert.Equal(3, result.RemainingRows);
        Assert.Equal(["event-2", "event-3", "event-4"], remaining);
    }
}
