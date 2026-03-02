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
    public async Task AdvanceQueue_CompletesActiveAndActivatesNext_WhenNextQueued()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var (_, activeItem) = SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "active", 1,
            "250 gal", 250, "HA", "CA", 3);
        activeItem.QuantityCompleted = 3;
        SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "queued", 2,
            "320 gal", 320, "HB", "CB", 7);
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AdvanceQueueAsync(TestHelpers.wcRollsId);

        Assert.NotNull(result);
        Assert.Equal("HB", result.HeatNumber);
        Assert.Equal("320", result.ShellSize);
        Assert.Equal("320 gal", result.ProductDescription);

        var oldActive = await db.MaterialQueueItems
            .FirstAsync(m => m.WorkCenterId == TestHelpers.wcRollsId && m.Position == 1);
        Assert.Equal("completed", oldActive.Status);
    }

    [Fact]
    public async Task AdvanceQueue_ReturnsActive_WhenNoNextQueued()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedQueueItemWithSN(db, TestHelpers.wcRollsId, "active", 1,
            "250 gal", 250, "HA", "CA", 3);
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AdvanceQueueAsync(TestHelpers.wcRollsId);

        Assert.NotNull(result);
        Assert.Equal("HA", result.HeatNumber);
        Assert.Equal("250 gal", result.ProductDescription);

        var item = await db.MaterialQueueItems
            .FirstAsync(m => m.WorkCenterId == TestHelpers.wcRollsId);
        Assert.Equal("active", item.Status);
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
        var (_, queueItem) = SeedQueueItemWithSN(db, TestHelpers.wcFitupId, "queued", 1,
            "250 gal", 250, "HK-01", "CK-01", 1,
            queueType: "fitup", cardId: "03", cardColor: "Blue");
        queueItem.ProductionLineId = TestHelpers.ProductionLine1Plt1Id;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetCardLookupAsync(TestHelpers.wcFitupId, TestHelpers.ProductionLine1Plt1Id, "03");

        Assert.NotNull(result);
        Assert.Equal("HK-01", result.HeatNumber);
        Assert.Equal("CK-01", result.CoilNumber);
        Assert.Equal("250 gal", result.ProductDescription);
        Assert.Equal("Blue", result.CardColor);
    }

    [Fact]
    public async Task GetCardLookup_ReturnsNull_WhenCardOnlyQueuedOnDifferentProductionLine()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var (_, queueItem) = SeedQueueItemWithSN(db, TestHelpers.wcFitupId, "queued", 1,
            "250 gal", 250, "HK-01", "CK-01", 1,
            queueType: "fitup", cardId: "03", cardColor: "Blue");
        queueItem.ProductionLineId = TestHelpers.ProductionLine1Plt2Id;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetCardLookupAsync(TestHelpers.wcFitupId, TestHelpers.ProductionLine1Plt1Id, "03");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCardLookup_ReturnsNull_WhenCardExistsButNotQueued()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.BarcodeCards.Add(new BarcodeCard
        {
            Id = Guid.NewGuid(),
            CardValue = "77",
            Color = "Green",
            Description = "Loose card"
        });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetCardLookupAsync(TestHelpers.wcFitupId, TestHelpers.ProductionLine1Plt1Id, "77");

        Assert.Null(result);
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

    private static void SeedSpotIncrementWithTankCount(MesDbContext db, DateTime utcTimestamp, int tankCount)
    {
        var assemblyIds = Enumerable.Range(0, tankCount)
            .Select(_ => Guid.NewGuid())
            .ToList();

        foreach (var assemblyId in assemblyIds)
        {
            db.SerialNumbers.Add(new SerialNumber
            {
                Id = assemblyId,
                Serial = "ASM-" + assemblyId.ToString("N")[..6],
                ProductId = TestProductId,
                PlantId = TestHelpers.PlantPlt1Id,
                CreatedAt = utcTimestamp
            });
        }

        var productionRecordId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = productionRecordId,
            SerialNumberId = assemblyIds[0],
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = utcTimestamp,
            PlantGearId = TestPlantGearId
        });

        var incrementId = Guid.NewGuid();
        db.SpotXrayIncrements.Add(new SpotXrayIncrement
        {
            Id = incrementId,
            ManufacturingLogId = productionRecordId,
            IncrementNo = "SPOT-INC-1",
            OverallStatus = "Pending",
            LaneNo = "Lane 1",
            IsDraft = true,
            CreatedDateTime = utcTimestamp
        });

        for (var i = 0; i < assemblyIds.Count; i++)
        {
            db.SpotXrayIncrementTanks.Add(new SpotXrayIncrementTank
            {
                Id = Guid.NewGuid(),
                SpotXrayIncrementId = incrementId,
                SerialNumberId = assemblyIds[i],
                Position = i + 1
            });
        }
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
        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id, "2026-03-14", 10);
        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
    }

    [Fact]
    public async Task GetHistory_DayCountZero_ButRecentRecordsStillReturned()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        // Record at 2026-03-15 03:00 UTC = March 14 22:00 CDT
        SeedProductionRecord(db, TestHelpers.wcRollsId, new DateTime(2026, 3, 15, 3, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        // Querying for March 15 (local) — day count should be 0, but the record still appears in recent list
        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id, "2026-03-15", 10);
        Assert.Equal(0, result.DayCount);
        Assert.Single(result.RecentRecords);
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

        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id, "2026-02-20", 10);
        Assert.Equal(2, result.DayCount);
        Assert.Equal(2, result.RecentRecords.Count);
    }

    [Fact]
    public async Task GetHistory_SpotCountsIncrementTanks_ForDayAndHourly()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        SeedSpotIncrementWithTankCount(db, new DateTime(2026, 2, 20, 18, 30, 0, DateTimeKind.Utc), tankCount: 3);
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcSpotXrayId,
            TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            "2026-02-20",
            10);

        Assert.Equal(3, result.DayCount);
        Assert.Equal(3, result.HourlyCounts.Sum(h => h.Count));
        Assert.Empty(result.RecentRecords);
    }

    [Fact]
    public async Task GetHistory_SpotIgnoresAssetFilter_WhenCountingTanks()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        SeedSpotIncrementWithTankCount(db, new DateTime(2026, 2, 20, 18, 30, 0, DateTimeKind.Utc), tankCount: 2);
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcSpotXrayId,
            TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            "2026-02-20",
            10,
            assetId: Guid.NewGuid());

        Assert.Equal(2, result.DayCount);
        Assert.Equal(2, result.HourlyCounts.Sum(h => h.Count));
    }

    [Fact]
    public async Task GetHistory_ReturnsAnnotationColor_WhenAnnotationExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var utcTimestamp = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "SN-ANNOT1",
            ProductId = TestProductId,
            CreatedAt = utcTimestamp
        });
        var prId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = prId,
            SerialNumberId = snId,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = utcTimestamp,
            PlantGearId = TestPlantGearId
        });

        var defectTypeId = Guid.Parse("a1000003-0000-0000-0000-000000000003");
        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = prId,
            AnnotationTypeId = defectTypeId,
            Status = AnnotationStatus.Open,
            Notes = "Test defect",
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = utcTimestamp
        });
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id, "2026-02-20", 10);

        Assert.Single(result.RecentRecords);
        Assert.True(result.RecentRecords[0].HasAnnotation);
        Assert.Equal("#ff0000", result.RecentRecords[0].AnnotationColor);
    }

    [Fact]
    public async Task GetHistory_ReturnsNullAnnotationColor_WhenNoAnnotation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        SeedProductionRecord(db, TestHelpers.wcRollsId, new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id, "2026-02-20", 10);

        Assert.Single(result.RecentRecords);
        Assert.False(result.RecentRecords[0].HasAnnotation);
        Assert.Null(result.RecentRecords[0].AnnotationColor);
    }

    [Fact]
    public async Task GetHistory_FitupWorkCenter_IncludesShellSerialsInIdentifier()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var utcTimestamp = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var assemblySnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = assemblySnId,
            Serial = "AB",
            ProductId = TestProductId,
            CreatedAt = utcTimestamp
        });
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = assemblySnId,
            WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = utcTimestamp
        });

        var shell1Id = Guid.NewGuid();
        var shell2Id = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber { Id = shell1Id, Serial = "SH001", ProductId = TestProductId, CreatedAt = utcTimestamp });
        db.SerialNumbers.Add(new SerialNumber { Id = shell2Id, Serial = "SH002", ProductId = TestProductId, CreatedAt = utcTimestamp });

        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shell1Id,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = utcTimestamp
        });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shell2Id,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = utcTimestamp
        });
        await db.SaveChangesAsync();

        var result = await sut.GetHistoryAsync(TestHelpers.wcFitupId, TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id, "2026-02-20", 10);

        Assert.Single(result.RecentRecords);
        Assert.Contains("AB", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Contains("SH001", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Contains("SH002", result.RecentRecords[0].SerialOrIdentifier);
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
    public async Task GetDefectLocations_IncludesUniversalAndMappedCharacteristicLocations()
    {
        await using var db = TestHelpers.CreateInMemoryContext();

        var mappedCharacteristicId = Guid.NewGuid();
        var unmappedCharacteristicId = Guid.NewGuid();

        db.Characteristics.AddRange(
            new Characteristic
            {
                Id = mappedCharacteristicId,
                Code = "LS-MAP",
                Name = "LS Mapped",
                ProductTypeId = null,
                IsActive = true
            },
            new Characteristic
            {
                Id = unmappedCharacteristicId,
                Code = "LS-OTHER",
                Name = "LS Unmapped",
                ProductTypeId = null,
                IsActive = true
            });

        db.CharacteristicWorkCenters.Add(new CharacteristicWorkCenter
        {
            Id = Guid.NewGuid(),
            CharacteristicId = mappedCharacteristicId,
            WorkCenterId = TestHelpers.wcLongSeamInspId
        });

        db.DefectLocations.AddRange(
            new DefectLocation
            {
                Id = Guid.NewGuid(),
                Code = "ZZ-MAP",
                Name = "Mapped Location",
                CharacteristicId = mappedCharacteristicId,
                IsActive = true
            },
            new DefectLocation
            {
                Id = Guid.NewGuid(),
                Code = "ZZ-ANY",
                Name = "Universal Location",
                CharacteristicId = null,
                IsActive = true
            },
            new DefectLocation
            {
                Id = Guid.NewGuid(),
                Code = "ZZ-NOMAP",
                Name = "Unmapped Location",
                CharacteristicId = unmappedCharacteristicId,
                IsActive = true
            });

        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetDefectLocationsAsync(TestHelpers.wcLongSeamInspId);

        var codes = result.Select(r => r.Code).ToList();
        Assert.Contains("ZZ-MAP", codes);
        Assert.Contains("ZZ-ANY", codes);
        Assert.DoesNotContain("ZZ-NOMAP", codes);
    }

    [Fact]
    public async Task GetDefectLocations_ExcludesInactiveUniversalLocations()
    {
        await using var db = TestHelpers.CreateInMemoryContext();

        db.DefectLocations.Add(new DefectLocation
        {
            Id = Guid.NewGuid(),
            Code = "ZZ-INACTIVE-ANY",
            Name = "Inactive Universal",
            CharacteristicId = null,
            IsActive = false
        });

        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetDefectLocationsAsync(TestHelpers.wcLongSeamInspId);

        Assert.DoesNotContain(result, r => r.Code == "ZZ-INACTIVE-ANY");
    }

    [Fact]
    public async Task GetCharacteristics_WithoutTankSize_ReturnsAll()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.GetCharacteristicsAsync(TestHelpers.wcRoundSeamInspId);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetCharacteristics_SmallTank_ReturnsOnlyRS1AndRS2()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.GetCharacteristicsAsync(TestHelpers.wcRoundSeamInspId, tankSize: 500);

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.True(c.Name == "RS1" || c.Name == "RS2"));
    }

    [Fact]
    public async Task GetCharacteristics_1000Tank_ReturnsRS1_RS2_RS3()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.GetCharacteristicsAsync(TestHelpers.wcRoundSeamInspId, tankSize: 1000);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, c => c.Name == "RS3");
        Assert.DoesNotContain(result, c => c.Name == "RS4");
    }

    [Fact]
    public async Task GetCharacteristics_LargeTank_ReturnsAllFour()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.GetCharacteristicsAsync(TestHelpers.wcRoundSeamInspId, tankSize: 1500);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task LookupWelder_ReturnsWelder_WhenActive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.LookupWelderAsync("EMP001");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("EMP001", result.EmployeeNumber);
    }

    [Fact]
    public async Task LookupWelder_ReturnsNull_WhenInactive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = await db.Users.FirstAsync(u => u.EmployeeNumber == "EMP001");
        user.IsActive = false;
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.LookupWelderAsync("EMP001");

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
    public async Task LookupWelder_ReturnsNull_WhenNotFound()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.LookupWelderAsync("NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddMaterialQueueItem_UpdatesProductId_WhenSameHeatCoilReusedWithDifferentProduct()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product120 = new Product
        {
            Id = Guid.NewGuid(),
            ProductNumber = "PL 120",
            TankSize = 120,
            TankType = "Plate",
            ProductTypeId = TestProductTypeId
        };
        var product1000 = new Product
        {
            Id = Guid.NewGuid(),
            ProductNumber = "PL 1000",
            TankSize = 1000,
            TankType = "Plate",
            ProductTypeId = TestProductTypeId
        };
        db.Products.AddRange(product120, product1000);
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var first = await sut.AddMaterialQueueItemAsync(TestHelpers.wcRollsId,
            new DTOs.CreateMaterialQueueItemDto
            {
                ProductId = product120.Id,
                HeatNumber = "H-REUSE",
                CoilNumber = "C-REUSE",
                Quantity = 5
            });
        Assert.Equal("120", first.ShellSize);

        var second = await sut.AddMaterialQueueItemAsync(TestHelpers.wcRollsId,
            new DTOs.CreateMaterialQueueItemDto
            {
                ProductId = product1000.Id,
                HeatNumber = "H-REUSE",
                CoilNumber = "C-REUSE",
                Quantity = 2
            });
        Assert.Equal("1000", second.ShellSize);

        var sn = await db.SerialNumbers
            .Include(s => s.Product)
            .FirstAsync(s => s.Serial == "Heat H-REUSE Coil C-REUSE");
        Assert.Equal(product1000.Id, sn.ProductId);
    }

    [Fact]
    public async Task AddMaterialQueueItem_WhenQueueHasFiveItems_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductNumber = "PL 250",
            TankSize = 250,
            TankType = "Plate",
            ProductTypeId = TestProductTypeId
        };
        db.Products.Add(product);

        for (var i = 1; i <= 5; i++)
        {
            SeedQueueItemWithSN(
                db,
                TestHelpers.wcRollsId,
                i == 1 ? "active" : "queued",
                i,
                "250 gal",
                250,
                $"H{i}",
                $"C{i}",
                1);
        }

        await db.SaveChangesAsync();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddMaterialQueueItemAsync(TestHelpers.wcRollsId, new DTOs.CreateMaterialQueueItemDto
            {
                ProductId = product.Id,
                HeatNumber = "H-OVER",
                CoilNumber = "C-OVER",
                Quantity = 1
            }));

        Assert.Contains("Queue is full", ex.Message);
    }

    [Fact]
    public async Task AddFitupQueueItem_WhenQueueHasFiveItems_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductNumber = "HEAD 500",
            TankSize = 500,
            TankType = "Head",
            ProductTypeId = TestProductTypeId
        };
        db.Products.Add(product);

        for (var i = 1; i <= 5; i++)
        {
            SeedQueueItemWithSN(
                db,
                TestHelpers.wcFitupId,
                "queued",
                i,
                "ELLIP 24\" OD",
                500,
                $"FH{i}",
                $"FC{i}",
                1,
                queueType: "fitup",
                cardId: i.ToString("00"),
                cardColor: "Blue");
        }

        await db.SaveChangesAsync();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
            {
                ProductId = product.Id,
                VendorHeadId = Guid.NewGuid(),
                LotNumber = "LOT-OVER",
                CardCode = "99"
            }));

        Assert.Contains("Queue is full", ex.Message);
    }

    [Fact]
    public async Task AddMaterialQueueItem_DuplicateSubmit_ReturnsExistingItem()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "plate");
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var first = await sut.AddMaterialQueueItemAsync(TestHelpers.wcRollsId, new DTOs.CreateMaterialQueueItemDto
        {
            ProductId = product.Id,
            HeatNumber = "H-IDEMP",
            CoilNumber = "C-IDEMP",
            Quantity = 3
        });

        var second = await sut.AddMaterialQueueItemAsync(TestHelpers.wcRollsId, new DTOs.CreateMaterialQueueItemDto
        {
            ProductId = product.Id,
            HeatNumber = "H-IDEMP",
            CoilNumber = "C-IDEMP",
            Quantity = 3
        });

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, db.MaterialQueueItems.Count(m => m.WorkCenterId == TestHelpers.wcRollsId && m.QueueType == "rolls"));
    }

    [Fact]
    public async Task AddFitupQueueItem_DuplicateSubmit_ReturnsExistingItem()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var first = await sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
        {
            ProductId = product.Id,
            VendorHeadId = Guid.NewGuid(),
            LotNumber = "LOT-IDEMP",
            CardCode = "03"
        });

        var second = await sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
        {
            ProductId = product.Id,
            VendorHeadId = Guid.NewGuid(),
            LotNumber = "LOT-IDEMP",
            CardCode = "03"
        });

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, db.MaterialQueueItems.Count(m => m.WorkCenterId == TestHelpers.wcFitupId && m.QueueType == "fitup"));
    }

    [Fact]
    public async Task AddFitupQueueItem_ReturnsVendorHeadId_ForEditPrepopulation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First();
        var vendorId = Guid.NewGuid();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var created = await sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
        {
            ProductId = product.Id,
            VendorHeadId = vendorId,
            LotNumber = "LOT-EDIT",
            CardCode = "04"
        });

        Assert.Equal(vendorId, created.VendorHeadId);

        var queue = await sut.GetMaterialQueueAsync(TestHelpers.wcFitupId, "fitup");
        var fetched = Assert.Single(queue);
        Assert.Equal(vendorId, fetched.VendorHeadId);
    }

    [Fact]
    public async Task GetMaterialQueue_ScopesToProductionLine_WhenProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var insideLineId = TestHelpers.ProductionLine1Plt1Id;
        var outsideLineId = Guid.Parse("e2111111-1111-1111-1111-111111111111");

        var (_, insideItem) = SeedQueueItemWithSN(
            db, TestHelpers.wcRollsId, "queued", 1,
            "120 gal", 120, "H-IN", "C-IN", 4);
        insideItem.ProductionLineId = insideLineId;

        var (_, outsideItem) = SeedQueueItemWithSN(
            db, TestHelpers.wcRollsId, "queued", 2,
            "250 gal", 250, "H-OUT", "C-OUT", 6);
        outsideItem.ProductionLineId = outsideLineId;

        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetMaterialQueueAsync(TestHelpers.wcRollsId, type: null, productionLineId: insideLineId);

        Assert.Single(result);
        Assert.Equal("H-IN", result[0].HeatNumber);
    }

    [Fact]
    public async Task AdvanceQueue_ScopesToProductionLine_WhenProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var insideLineId = TestHelpers.ProductionLine1Plt1Id;
        var outsideLineId = Guid.Parse("e2111111-1111-1111-1111-111111111111");

        var (_, outsideActive) = SeedQueueItemWithSN(
            db, TestHelpers.wcRollsId, "active", 1,
            "250 gal", 250, "H-ACTIVE-OUT", "C-ACTIVE-OUT", 10);
        outsideActive.ProductionLineId = outsideLineId;

        var (_, insideQueued) = SeedQueueItemWithSN(
            db, TestHelpers.wcRollsId, "queued", 2,
            "120 gal", 120, "H-NEXT-IN", "C-NEXT-IN", 8);
        insideQueued.ProductionLineId = insideLineId;

        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.AdvanceQueueAsync(TestHelpers.wcRollsId, productionLineId: insideLineId);

        Assert.NotNull(result);
        Assert.Equal("H-NEXT-IN", result!.HeatNumber);
    }

    [Fact]
    public async Task AddFitupQueueItem_AllowsSameCardAcrossDifferentProductionLines()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First();
        var vendorId = Guid.NewGuid();
        var insideLineId = TestHelpers.ProductionLine1Plt1Id;
        var outsideLineId = Guid.Parse("e2111111-1111-1111-1111-111111111111");

        var (_, existingOutsideLineItem) = SeedQueueItemWithSN(
            db, TestHelpers.wcFitupId, "queued", 1,
            "ELLIP 24\" OD", 500, "FH-OUT", "FC-OUT", 1,
            queueType: "fitup", cardId: "01", cardColor: "Blue");
        existingOutsideLineItem.ProductionLineId = outsideLineId;

        await db.SaveChangesAsync();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var result = await sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
        {
            ProductId = product.Id,
            VendorHeadId = vendorId,
            LotNumber = "LOT-IN",
            CardCode = "01",
            ProductionLineId = insideLineId
        });

        Assert.Equal("01", result.CardId);
        Assert.Equal(2, db.MaterialQueueItems.Count(m => m.WorkCenterId == TestHelpers.wcFitupId && m.QueueType == "fitup" && m.CardId == "01" && m.Status == "queued"));
    }

    [Fact]
    public async Task AddFitupQueueItem_BlocksSameCardOnSameProductionLine()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First();
        var vendorId = Guid.NewGuid();
        var lineId = TestHelpers.ProductionLine1Plt1Id;

        var (_, existingItem) = SeedQueueItemWithSN(
            db, TestHelpers.wcFitupId, "queued", 1,
            "ELLIP 24\" OD", 500, "FH-IN", "FC-IN", 1,
            queueType: "fitup", cardId: "01", cardColor: "Blue");
        existingItem.ProductionLineId = lineId;

        await db.SaveChangesAsync();
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
            {
                ProductId = product.Id,
                VendorHeadId = vendorId,
                LotNumber = "LOT-IN-2",
                CardCode = "01",
                ProductionLineId = lineId
            }));

        Assert.Contains("already assigned", ex.Message);
    }

    [Fact]
    public async Task GetQueueTransactions_FiltersByProductionLine_AndAction()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var targetLineId = TestHelpers.ProductionLine1Plt1Id;
        var otherLineId = TestHelpers.ProductionLine1Plt2Id;
        var now = DateTime.UtcNow;

        db.QueueTransactions.AddRange(
            new QueueTransaction
            {
                Id = Guid.NewGuid(),
                WorkCenterId = TestHelpers.wcFitupId,
                ProductionLineId = targetLineId,
                Action = "added",
                ItemSummary = "target added",
                OperatorName = string.Empty,
                Timestamp = now
            },
            new QueueTransaction
            {
                Id = Guid.NewGuid(),
                WorkCenterId = TestHelpers.wcFitupId,
                ProductionLineId = targetLineId,
                Action = "removed",
                ItemSummary = "target removed",
                OperatorName = string.Empty,
                Timestamp = now.AddMinutes(-1)
            },
            new QueueTransaction
            {
                Id = Guid.NewGuid(),
                WorkCenterId = TestHelpers.wcFitupId,
                ProductionLineId = otherLineId,
                Action = "added",
                ItemSummary = "other line added",
                OperatorName = string.Empty,
                Timestamp = now.AddMinutes(-2)
            });
        await db.SaveChangesAsync();

        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);
        var result = await sut.GetQueueTransactionsAsync(
            TestHelpers.wcFitupId,
            targetLineId,
            limit: 10,
            plantId: TestHelpers.PlantPlt1Id,
            action: "added");

        var tx = Assert.Single(result);
        Assert.Equal("added", tx.Action);
        Assert.Equal("target added", tx.ItemSummary);
    }

    [Fact]
    public async Task AddFitupQueueItem_WritesQueueTransactionWithProductionLine()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First();
        var lineId = TestHelpers.ProductionLine1Plt1Id;
        var sut = new WorkCenterService(db, NullLogger<WorkCenterService>.Instance);

        await sut.AddFitupQueueItemAsync(TestHelpers.wcFitupId, new DTOs.CreateFitupQueueItemDto
        {
            ProductId = product.Id,
            VendorHeadId = Guid.NewGuid(),
            LotNumber = "LOT-TX-LINE",
            CardCode = "09",
            ProductionLineId = lineId
        });

        var tx = await db.QueueTransactions
            .OrderByDescending(t => t.Timestamp)
            .FirstAsync(t => t.WorkCenterId == TestHelpers.wcFitupId && t.Action == "added");

        Assert.Equal(lineId, tx.ProductionLineId);
    }
}
