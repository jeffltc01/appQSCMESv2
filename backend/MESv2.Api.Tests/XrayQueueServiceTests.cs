using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class XrayQueueServiceTests
{
    [Fact]
    public async Task GetQueue_ReturnsItemsForWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.XrayQueueItems.Add(new XrayQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            SerialNumber = "SH001",
            OperatorId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new XrayQueueService(db);
        var result = await sut.GetQueueAsync(TestHelpers.WorkCenter1Plt1Id);

        Assert.Single(result);
        Assert.Equal("SH001", result[0].SerialNumber);
    }

    [Fact]
    public async Task AddAsync_ValidSerial_AddsToQueue()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.SerialNumbers.Add(new SerialNumber { Id = Guid.NewGuid(), Serial = "SH002", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = new XrayQueueService(db);
        var result = await sut.AddAsync(TestHelpers.WorkCenter1Plt1Id, new AddXrayQueueItemDto
        {
            SerialNumber = "SH002",
            OperatorId = TestHelpers.TestUserId
        });

        Assert.NotNull(result);
        Assert.Equal("SH002", result.SerialNumber);
    }

    [Fact]
    public async Task AddAsync_UnknownSerial_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new XrayQueueService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddAsync(TestHelpers.WorkCenter1Plt1Id, new AddXrayQueueItemDto
            {
                SerialNumber = "UNKNOWN",
                OperatorId = TestHelpers.TestUserId
            }));
    }

    [Fact]
    public async Task AddAsync_Duplicate_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber { Id = snId, Serial = "SH003", CreatedAt = DateTime.UtcNow });
        db.XrayQueueItems.Add(new XrayQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            SerialNumber = "SH003",
            OperatorId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new XrayQueueService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddAsync(TestHelpers.WorkCenter1Plt1Id, new AddXrayQueueItemDto
            {
                SerialNumber = "SH003",
                OperatorId = TestHelpers.TestUserId
            }));
    }

    [Fact]
    public async Task RemoveAsync_ExistingItem_ReturnsTrue()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var itemId = Guid.NewGuid();
        db.XrayQueueItems.Add(new XrayQueueItem
        {
            Id = itemId,
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            SerialNumber = "SH004",
            OperatorId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new XrayQueueService(db);
        var result = await sut.RemoveAsync(TestHelpers.WorkCenter1Plt1Id, itemId);

        Assert.True(result);
    }
}
