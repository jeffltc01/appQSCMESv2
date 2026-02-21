using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task GetWorkCenters_ReturnsAllWorkCenters()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.GetWorkCentersAsync();

        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
    }

    private static (SerialNumber sn, MaterialQueueItem qi) SeedQueueItemWithSN(
        MesDbContext db, Guid wcId, string status, int position,
        string productNumber, int tankSize, string heatNumber, string coilNumber,
        int quantity, string? queueType = "rolls", string? cardId = null, string? cardColor = null)
    {
        var product = db.Products.FirstOrDefault(p => p.ProductNumber == productNumber)
            ?? new Product
            {
                Id = Guid.NewGuid(),
                ProductNumber = productNumber,
                TankSize = tankSize,
                TankType = "Shell",
                ProductTypeId = Guid.Parse("a3333333-3333-3333-3333-333333333333")
            };
        if (db.Entry(product).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            db.Products.Add(product);

        var sn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = $"Heat {heatNumber} Coil {coilNumber}",
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            HeatNumber = heatNumber,
            CoilNumber = coilNumber,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(sn);

        var qi = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            Position = position,
            Status = status,
            Quantity = quantity,
            QueueType = queueType,
            CardId = cardId,
            CardColor = cardColor,
            SerialNumberId = sn.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.MaterialQueueItems.Add(qi);

        return (sn, qi);
    }

    [Fact]
    public async Task AdvanceQueue_ReturnsNextItem_AndUpdatesStatus()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "queued", 1,
            "120 gal", 120, "H1", "C1", 5);
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AdvanceQueueAsync(TestHelpers.wcRollsId);

        Assert.NotNull(result);
        Assert.Equal("H1", result.HeatNumber);
        Assert.Equal("C1", result.CoilNumber);
        Assert.Equal(5, result.Quantity);
        Assert.Equal("120 gal", result.ProductDescription);

        var item = await db.MaterialQueueItems
            .FirstOrDefaultAsync(m => m.WorkCenterId == TestHelpers.wcRollsId && m.Status == "active");
        Assert.NotNull(item);
    }

    [Fact]
    public async Task AdvanceQueue_ReturnsActive_WhenAlreadyActive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "active", 1,
            "250 gal", 250, "HA", "CA", 3);
        SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "queued", 2,
            "320 gal", 320, "HB", "CB", 7);
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AdvanceQueueAsync(TestHelpers.wcRollsId);

        Assert.NotNull(result);
        Assert.Equal("HA", result.HeatNumber);
        Assert.Equal("250 gal", result.ProductDescription);
    }

    [Fact]
    public async Task GetMaterialQueue_ReturnsDerivedDataFromSerialNumber()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "queued", 1,
            "120 gal", 120, "H-TEST", "C-TEST", 5);
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetMaterialQueueAsync(TestHelpers.wcRollsId, null);

        Assert.Single(result);
        Assert.Equal("120 gal", result[0].ProductDescription);
        Assert.Equal("120", result[0].ShellSize);
        Assert.Equal("H-TEST", result[0].HeatNumber);
        Assert.Equal("C-TEST", result[0].CoilNumber);
    }

    [Fact]
    public async Task GetMaterialQueue_HandlesNullSerialNumber()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.MaterialQueueItems.Add(new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRollsId,
            Position = 1,
            Status = "queued",
            Quantity = 1,
            QueueType = "rolls",
            SerialNumberId = null,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetMaterialQueueAsync(TestHelpers.wcRollsId, null);

        Assert.Single(result);
        Assert.Equal("", result[0].ProductDescription);
        Assert.Null(result[0].ShellSize);
        Assert.Equal("", result[0].HeatNumber);
        Assert.Equal("", result[0].CoilNumber);
    }

    [Fact]
    public async Task GetCardLookup_ReturnsDataFromSerialNumber()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedQueueItemWithSN(db, TestHelpers.wcFitupId, "queued", 1,
            "250 gal", 250, "HK-01", "CK-01", 1,
            queueType: "fitup", cardId: "03", cardColor: "Blue");
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetCardLookupAsync("03");

        Assert.NotNull(result);
        Assert.Equal("HK-01", result.HeatNumber);
        Assert.Equal("CK-01", result.CoilNumber);
        Assert.Equal("250 gal", result.ProductDescription);
        Assert.Equal("Blue", result.CardColor);
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
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        // Cleveland plant uses America/Chicago (UTC-6 standard / UTC-5 DST).
        // Create a record at 2026-03-15 03:00 UTC = 2026-03-14 22:00 CDT (still March 14 locally)
        var utcTimestamp = new DateTime(2026, 3, 15, 3, 0, 0, DateTimeKind.Utc);
        SeedProductionRecord(db, TestHelpers.wcRollsId, utcTimestamp);
        await db.SaveChangesAsync();

        // Querying for March 14 (local) should find this record. PlantPlt1Id = Cleveland (America/Chicago).
        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, "2026-03-14", 10);
        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
    }

    [Fact]
    public async Task GetHistory_ExcludesRecordsOutsideLocalDay()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        // Record at 2026-03-15 03:00 UTC = March 14 22:00 CDT
        SeedProductionRecord(db, TestHelpers.wcRollsId, new DateTime(2026, 3, 15, 3, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        // Querying for March 15 (local) should NOT find this record (it's March 14 in Central time)
        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, "2026-03-15", 10);
        Assert.Equal(0, result.DayCount);
        Assert.Empty(result.RecentRecords);
    }

    [Fact]
    public async Task GetHistory_HandlesMultipleRecords_SameLocalDay()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        // Both timestamps fall within Feb 20 Central time (UTC-6):
        //   06:00 UTC = 00:00 CST, 23:59 UTC = 17:59 CST
        SeedProductionRecord(db, TestHelpers.wcRollsId, new DateTime(2026, 2, 20, 6, 0, 0, DateTimeKind.Utc));
        SeedProductionRecord(db, TestHelpers.wcRollsId, new DateTime(2026, 2, 20, 23, 59, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, "2026-02-20", 10);
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
            WorkCenterId = TestHelpers.wcRollsId
        });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.GetDefectCodesAsync(TestHelpers.wcRollsId);

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
        db.DefectWorkCenters.Add(new DefectWorkCenter { Id = Guid.NewGuid(), DefectCodeId = activeId, WorkCenterId = TestHelpers.wcRollsId });
        db.DefectWorkCenters.Add(new DefectWorkCenter { Id = Guid.NewGuid(), DefectCodeId = inactiveId, WorkCenterId = TestHelpers.wcRollsId });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetDefectCodesAsync(TestHelpers.wcRollsId);

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

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AddWelderAsync(TestHelpers.wcRollsId, "EMP001");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddWelder_ReturnsWelder_WhenCertified()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");
        user.IsCertifiedWelder = true;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AddWelderAsync(TestHelpers.wcRollsId, "EMP001");

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

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AddWelderAsync(TestHelpers.wcRollsId, "EMP001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaterialQueue_FiltersByQueueType_NotStatus()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedQueueItemWithSN(db, TestHelpers.wcFitupId, "queued", 1,
            "ELLIP 24\" OD", 120, "H1", "C1", 1, queueType: "fitup");
        SeedQueueItemWithSN(db, TestHelpers.wcFitupId, "queued", 2,
            "120 gal", 120, "H2", "C2", 5, queueType: "rolls");
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var fitupItems = await sut.GetMaterialQueueAsync(TestHelpers.wcFitupId, "fitup");
        Assert.Single(fitupItems);
        Assert.Equal("ELLIP 24\" OD", fitupItems[0].ProductDescription);

        var allItems = await sut.GetMaterialQueueAsync(TestHelpers.wcFitupId, null);
        Assert.Equal(2, allItems.Count);
    }

    [Fact]
    public async Task AddWelder_ReturnsNull_WhenWelderFromDifferentSite()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        // EMP004 is a certified welder from Plant 2
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP004");
        Assert.True(user.IsCertifiedWelder);
        Assert.Equal(TestHelpers.PlantPlt2Id, user.DefaultSiteId);

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        // wcRollsId belongs to Plant 1 via ProductionLine — should reject cross-site welder
        var result = await sut.AddWelderAsync(TestHelpers.wcRollsId, "EMP004");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddWelder_Succeeds_WhenWelderFromSameSite()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        // EMP003 is a certified welder from Plant 1
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP003");
        Assert.True(user.IsCertifiedWelder);
        Assert.Equal(TestHelpers.PlantPlt1Id, user.DefaultSiteId);

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        // wcRollsId belongs to Plant 1 — should accept same-site welder
        var result = await sut.AddWelderAsync(TestHelpers.wcRollsId, "EMP003");

        Assert.NotNull(result);
        Assert.Equal("EMP003", result.EmployeeNumber);
    }
}
