using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class WorkCenterServiceTests
{
    private static readonly Guid TestProductTypeId = Guid.Parse("a3333333-3333-3333-3333-333333333333");
    private static readonly Guid TestProductId = Guid.Parse("b3011111-1111-1111-1111-111111111111");
    private static readonly Guid TestPlantGearId = Guid.Parse("61111111-1111-1111-1111-111111111111");

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

    private static void SeedProductionRecord(MesDbContext db, Guid wcId, DateTime utcTimestamp)
    {
        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "SN-" + snId.ToString("N")[..6],
            ProductId = TestProductId,
            CreatedAt = utcTimestamp
        });
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = snId,
            WorkCenterId = wcId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = utcTimestamp,
            PlantGearId = TestPlantGearId
        });
    }

    [Fact]
    public async Task GetHistory_ReturnsRecordsForLocalDate_UsingPlantTimezone()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db);

        // Cleveland plant uses America/Chicago (UTC-6 standard / UTC-5 DST).
        // Create a record at 2026-03-15 03:00 UTC = 2026-03-14 22:00 CDT (still March 14 locally)
        var utcTimestamp = new DateTime(2026, 3, 15, 3, 0, 0, DateTimeKind.Utc);
        SeedProductionRecord(db, TestHelpers.WorkCenter1Plt1Id, utcTimestamp);
        await db.SaveChangesAsync();

        // Querying for March 14 (local) should find this record
        var result = await sut.GetHistoryAsync(TestHelpers.WorkCenter1Plt1Id, "2026-03-14", 10);
        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
    }

    [Fact]
    public async Task GetHistory_ExcludesRecordsOutsideLocalDay()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db);

        // Record at 2026-03-15 03:00 UTC = March 14 22:00 CDT
        SeedProductionRecord(db, TestHelpers.WorkCenter1Plt1Id, new DateTime(2026, 3, 15, 3, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        // Querying for March 15 (local) should NOT find this record (it's March 14 in Central time)
        var result = await sut.GetHistoryAsync(TestHelpers.WorkCenter1Plt1Id, "2026-03-15", 10);
        Assert.Equal(0, result.DayCount);
        Assert.Empty(result.RecentRecords);
    }

    [Fact]
    public async Task GetHistory_HandlesMultipleRecords_SameLocalDay()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db);

        // Both timestamps fall within Feb 20 Central time (UTC-6):
        //   06:00 UTC = 00:00 CST, 23:59 UTC = 17:59 CST
        SeedProductionRecord(db, TestHelpers.WorkCenter1Plt1Id, new DateTime(2026, 2, 20, 6, 0, 0, DateTimeKind.Utc));
        SeedProductionRecord(db, TestHelpers.WorkCenter1Plt1Id, new DateTime(2026, 2, 20, 23, 59, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(TestHelpers.WorkCenter1Plt1Id, "2026-02-20", 10);
        Assert.Equal(2, result.DayCount);
        Assert.Equal(2, result.RecentRecords.Count);
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

    [Fact]
    public async Task GetDefectCodes_ExcludesInactive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var activeId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = activeId, Code = "A1", Name = "Active", IsActive = true });
        db.DefectCodes.Add(new DefectCode { Id = inactiveId, Code = "I1", Name = "Inactive", IsActive = false });
        db.DefectWorkCenters.Add(new DefectWorkCenter { Id = Guid.NewGuid(), DefectCodeId = activeId, WorkCenterId = TestHelpers.WorkCenter1Plt1Id });
        db.DefectWorkCenters.Add(new DefectWorkCenter { Id = Guid.NewGuid(), DefectCodeId = inactiveId, WorkCenterId = TestHelpers.WorkCenter1Plt1Id });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db);
        var result = await sut.GetDefectCodesAsync(TestHelpers.WorkCenter1Plt1Id);

        Assert.Single(result);
        Assert.Equal("A1", result[0].Code);
    }

    [Fact]
    public async Task AddWelder_ReturnsNull_WhenNotCertified()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");
        user.IsCertifiedWelder = false;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db);
        var result = await sut.AddWelderAsync(TestHelpers.WorkCenter1Plt1Id, "EMP001");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddWelder_ReturnsWelder_WhenCertified()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");
        user.IsCertifiedWelder = true;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db);
        var result = await sut.AddWelderAsync(TestHelpers.WorkCenter1Plt1Id, "EMP001");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("EMP001", result.EmployeeNumber);
    }

    [Fact]
    public async Task AddWelder_ReturnsNull_WhenInactive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");
        user.IsCertifiedWelder = true;
        user.IsActive = false;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db);
        var result = await sut.AddWelderAsync(TestHelpers.WorkCenter1Plt1Id, "EMP001");

        Assert.Null(result);
    }
}
