using Microsoft.EntityFrameworkCore;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class WorkCenterServiceTests
{
    [Fact]
    public async Task GetWorkCenters_FiltersBySiteCode()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db);

        var result = await sut.GetWorkCentersAsync("000");

        Assert.NotNull(result);
        Assert.All(result, wc => Assert.Equal(TestHelpers.PlantPlt1Id, wc.PlantId));
        Assert.True(result.Count >= 1);
    }

    [Fact]
    public async Task AdvanceQueue_ReturnsNextItem_AndUpdatesStatus()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.MaterialQueueItems.Add(new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            Position = 1,
            Status = "queued",
            ProductDescription = "Test Coil",
            HeatNumber = "H1",
            CoilNumber = "C1",
            Quantity = 5,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db);

        var result = await sut.AdvanceQueueAsync(TestHelpers.WorkCenter1Plt1Id);

        Assert.NotNull(result);
        Assert.Equal("H1", result.HeatNumber);
        Assert.Equal("C1", result.CoilNumber);
        Assert.Equal(5, result.Quantity);
        Assert.Equal("Test Coil", result.ProductDescription);

        var item = await db.MaterialQueueItems
            .FirstOrDefaultAsync(m => m.WorkCenterId == TestHelpers.WorkCenter1Plt1Id && m.HeatNumber == "H1");
        Assert.NotNull(item);
        Assert.Equal("active", item.Status);
    }

    [Fact]
    public async Task GetDefectCodes_ReturnsCodesForWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var defectCodeId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "DC1", Name = "Defect 1" });
        db.DefectWorkCenters.Add(new DefectWorkCenter
        {
            Id = Guid.NewGuid(),
            DefectCodeId = defectCodeId,
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id
        });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db);

        var result = await sut.GetDefectCodesAsync(TestHelpers.WorkCenter1Plt1Id);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("DC1", result[0].Code);
        Assert.Equal("Defect 1", result[0].Name);
    }
}
