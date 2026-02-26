using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class DefectAnalyticsServiceTests
{
    private static readonly Guid CharLongSeamId = Guid.Parse("c1000001-0000-0000-0000-000000000001");
    private static readonly Guid DefectLocationId = Guid.Parse("d1000001-0000-0000-0000-000000000001");
    private const string TestDate = "2026-06-10";
    private static readonly DateTime MidDay = new(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);

    private static DefectAnalyticsService CreateService(Data.MesDbContext db) => new(db);

    private static void SeedRecord(
        Data.MesDbContext db, Guid wcId, DateTime timestamp, Guid? operatorId = null, Guid? snId = null)
    {
        var id = snId ?? Guid.NewGuid();
        if (!db.SerialNumbers.Any(s => s.Id == id))
            db.SerialNumbers.Add(new SerialNumber { Id = id, Serial = id.ToString("N")[..6] });

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = id,
            WorkCenterId = wcId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = operatorId ?? TestHelpers.TestUserId,
            Timestamp = timestamp,
        });
    }

    [Fact]
    public async Task GetDefectPareto_ReturnsSortedCountsAndCumulativePercent()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var codeAId = Guid.Parse("d9010001-0000-0000-0000-000000000001");
        var codeBId = Guid.Parse("d9010002-0000-0000-0000-000000000002");
        db.DefectCodes.AddRange(
            new DefectCode { Id = codeAId, Code = "D001", Name = "Undercut" },
            new DefectCode { Id = codeBId, Code = "D002", Name = "Burn Through" }
        );

        var snA = Guid.NewGuid();
        var snB = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30), snId: snA);
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-20), snId: snB);
        await db.SaveChangesAsync();

        var recA = db.ProductionRecords.Single(r => r.SerialNumberId == snA).Id;
        var recB = db.ProductionRecords.Single(r => r.SerialNumberId == snB).Id;

        db.DefectLogs.AddRange(
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recA,
                SerialNumberId = snA,
                DefectCodeId = codeAId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-15),
                CreatedAt = MidDay.AddMinutes(-15),
            },
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recA,
                SerialNumberId = snA,
                DefectCodeId = codeAId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-14),
                CreatedAt = MidDay.AddMinutes(-14),
            },
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recB,
                SerialNumberId = snB,
                DefectCodeId = codeBId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-13),
                CreatedAt = MidDay.AddMinutes(-13),
            }
        );
        await db.SaveChangesAsync();

        var result = await sut.GetDefectParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day");

        Assert.Equal(3, result.TotalDefects);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("D001", result.Items[0].DefectCode);
        Assert.Equal(2, result.Items[0].Count);
        Assert.Equal(66.7m, result.Items[0].CumulativePercent);
        Assert.Equal("D002", result.Items[1].DefectCode);
        Assert.Equal(1, result.Items[1].Count);
        Assert.Equal(100m, result.Items[1].CumulativePercent);
    }

    [Fact]
    public async Task GetDefectPareto_OperatorFilter_RestrictsResults()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var otherOperatorId = Guid.Parse("88888888-8888-8888-8888-888888888801");
        var codeId = Guid.Parse("d9010003-0000-0000-0000-000000000003");
        db.DefectCodes.Add(new DefectCode { Id = codeId, Code = "D003", Name = "Crack" });

        var snA = Guid.NewGuid();
        var snB = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30), TestHelpers.TestUserId, snA);
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-25), otherOperatorId, snB);
        await db.SaveChangesAsync();

        var recA = db.ProductionRecords.Single(r => r.SerialNumberId == snA).Id;
        var recB = db.ProductionRecords.Single(r => r.SerialNumberId == snB).Id;

        db.DefectLogs.AddRange(
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recA,
                SerialNumberId = snA,
                DefectCodeId = codeId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-20),
                CreatedAt = MidDay.AddMinutes(-20),
            },
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recB,
                SerialNumberId = snB,
                DefectCodeId = codeId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-19),
                CreatedAt = MidDay.AddMinutes(-19),
            }
        );
        await db.SaveChangesAsync();

        var filtered = await sut.GetDefectParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day", TestHelpers.TestUserId);

        Assert.Equal(1, filtered.TotalDefects);
        Assert.Single(filtered.Items);
        Assert.Equal(1, filtered.Items[0].Count);
    }

    [Fact]
    public async Task GetDefectPareto_CountsDefectsWithoutProductionRecordId_WhenSerialHasWorkCenterActivity()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var codeId = Guid.Parse("d9010004-0000-0000-0000-000000000004");
        db.DefectCodes.Add(new DefectCode { Id = codeId, Code = "D004", Name = "Overlap" });

        var snId = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30), snId: snId);
        await db.SaveChangesAsync();

        db.DefectLogs.Add(new DefectLog
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = null,
            SerialNumberId = snId,
            DefectCodeId = codeId,
            CharacteristicId = CharLongSeamId,
            LocationId = DefectLocationId,
            Timestamp = MidDay.AddMinutes(-20),
            CreatedAt = MidDay.AddMinutes(-20),
        });
        await db.SaveChangesAsync();

        var result = await sut.GetDefectParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day");

        Assert.Equal(1, result.TotalDefects);
        Assert.Single(result.Items);
        Assert.Equal("D004", result.Items[0].DefectCode);
        Assert.Equal(1, result.Items[0].Count);
    }

    [Fact]
    public async Task GetDefectPareto_CountsDefects_WhenSerialIsLinkedByInspectionActivity()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var codeId = Guid.Parse("d9010007-0000-0000-0000-000000000007");
        db.DefectCodes.Add(new DefectCode { Id = codeId, Code = "D007", Name = "Pinhole" });

        var snId = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcFitupId, MidDay.AddMinutes(-40), snId: snId);
        await db.SaveChangesAsync();
        var fitupRecordId = db.ProductionRecords.Single(r => r.SerialNumberId == snId).Id;

        db.InspectionRecords.Add(new InspectionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = snId,
            ProductionRecordId = fitupRecordId,
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = MidDay.AddMinutes(-30),
            ControlPlanId = Guid.NewGuid(),
        });

        db.DefectLogs.Add(new DefectLog
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = fitupRecordId,
            SerialNumberId = snId,
            DefectCodeId = codeId,
            CharacteristicId = CharLongSeamId,
            LocationId = DefectLocationId,
            Timestamp = MidDay.AddMinutes(-10),
            CreatedAt = MidDay.AddMinutes(-10),
        });
        await db.SaveChangesAsync();

        var result = await sut.GetDefectParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day");

        Assert.Equal(1, result.TotalDefects);
        Assert.Single(result.Items);
        Assert.Equal("D007", result.Items[0].DefectCode);
    }

    [Fact]
    public async Task GetDefectPareto_TotalDefects_EqualsSumOfParetoItems()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var codeAId = Guid.Parse("d9010005-0000-0000-0000-000000000005");
        var codeBId = Guid.Parse("d9010006-0000-0000-0000-000000000006");
        db.DefectCodes.AddRange(
            new DefectCode { Id = codeAId, Code = "D005", Name = "Porosity" },
            new DefectCode { Id = codeBId, Code = "D006", Name = "Spatter" }
        );

        var snId = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-40), snId: snId);
        await db.SaveChangesAsync();
        var recId = db.ProductionRecords.Single(r => r.SerialNumberId == snId).Id;

        db.DefectLogs.AddRange(
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recId,
                SerialNumberId = snId,
                DefectCodeId = codeAId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-20),
                CreatedAt = MidDay.AddMinutes(-20),
            },
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recId,
                SerialNumberId = snId,
                DefectCodeId = codeBId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-10),
                CreatedAt = MidDay.AddMinutes(-10),
            },
            new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recId,
                SerialNumberId = snId,
                DefectCodeId = codeBId,
                CharacteristicId = CharLongSeamId,
                LocationId = DefectLocationId,
                Timestamp = MidDay.AddMinutes(-5),
                CreatedAt = MidDay.AddMinutes(-5),
            });
        await db.SaveChangesAsync();

        var result = await sut.GetDefectParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day");

        Assert.Equal(result.TotalDefects, result.Items.Sum(i => i.Count));
        Assert.Equal(3, result.TotalDefects);
    }

    [Fact]
    public async Task GetDowntimePareto_ReturnsGroupedMinutesAndCumulativePercent()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var categoryId = Guid.Parse("f3010001-0000-0000-0000-000000000001");
        var reasonAId = Guid.Parse("f3020001-0000-0000-0000-000000000001");
        var reasonBId = Guid.Parse("f3020002-0000-0000-0000-000000000002");
        db.DowntimeReasonCategories.Add(new DowntimeReasonCategory
        {
            Id = categoryId,
            PlantId = TestHelpers.PlantPlt1Id,
            Name = "Line Stops",
            SortOrder = 1,
            IsActive = true,
        });
        db.DowntimeReasons.AddRange(
            new DowntimeReason
            {
                Id = reasonAId,
                DowntimeReasonCategoryId = categoryId,
                Name = "Maintenance",
                CountsAsDowntime = true,
                SortOrder = 1,
                IsActive = true,
            },
            new DowntimeReason
            {
                Id = reasonBId,
                DowntimeReasonCategoryId = categoryId,
                Name = "Material",
                CountsAsDowntime = true,
                SortOrder = 2,
                IsActive = true,
            });

        db.DowntimeEvents.AddRange(
            new DowntimeEvent
            {
                Id = Guid.NewGuid(),
                WorkCenterProductionLineId = TestHelpers.wcplRollsId,
                OperatorUserId = TestHelpers.TestUserId,
                DowntimeReasonId = reasonAId,
                StartedAt = MidDay.AddMinutes(-40),
                EndedAt = MidDay.AddMinutes(-20),
                DurationMinutes = 20m,
                CreatedAt = MidDay.AddMinutes(-20),
            },
            new DowntimeEvent
            {
                Id = Guid.NewGuid(),
                WorkCenterProductionLineId = TestHelpers.wcplRollsId,
                OperatorUserId = TestHelpers.TestUserId,
                DowntimeReasonId = reasonBId,
                StartedAt = MidDay.AddMinutes(-15),
                EndedAt = MidDay.AddMinutes(-5),
                DurationMinutes = 10m,
                CreatedAt = MidDay.AddMinutes(-5),
            });
        await db.SaveChangesAsync();

        var result = await sut.GetDowntimeParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day");

        Assert.Equal(30m, result.TotalDowntimeMinutes);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Maintenance", result.Items[0].ReasonName);
        Assert.Equal(20m, result.Items[0].Minutes);
        Assert.Equal(66.7m, result.Items[0].CumulativePercent);
        Assert.Equal("Material", result.Items[1].ReasonName);
        Assert.Equal(10m, result.Items[1].Minutes);
        Assert.Equal(100m, result.Items[1].CumulativePercent);
    }

    [Fact]
    public async Task GetDowntimePareto_OperatorFilter_RestrictsResults()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var categoryId = Guid.Parse("f3010002-0000-0000-0000-000000000002");
        var reasonId = Guid.Parse("f3020003-0000-0000-0000-000000000003");
        var otherOperatorId = Guid.Parse("88888888-8888-8888-8888-888888888801");
        db.DowntimeReasonCategories.Add(new DowntimeReasonCategory
        {
            Id = categoryId,
            PlantId = TestHelpers.PlantPlt1Id,
            Name = "Stops",
            SortOrder = 1,
            IsActive = true,
        });
        db.DowntimeReasons.Add(new DowntimeReason
        {
            Id = reasonId,
            DowntimeReasonCategoryId = categoryId,
            Name = "Changeover",
            CountsAsDowntime = true,
            SortOrder = 1,
            IsActive = true,
        });

        db.DowntimeEvents.AddRange(
            new DowntimeEvent
            {
                Id = Guid.NewGuid(),
                WorkCenterProductionLineId = TestHelpers.wcplRollsId,
                OperatorUserId = TestHelpers.TestUserId,
                DowntimeReasonId = reasonId,
                StartedAt = MidDay.AddMinutes(-25),
                EndedAt = MidDay.AddMinutes(-15),
                DurationMinutes = 10m,
                CreatedAt = MidDay.AddMinutes(-15),
            },
            new DowntimeEvent
            {
                Id = Guid.NewGuid(),
                WorkCenterProductionLineId = TestHelpers.wcplRollsId,
                OperatorUserId = otherOperatorId,
                DowntimeReasonId = reasonId,
                StartedAt = MidDay.AddMinutes(-10),
                EndedAt = MidDay.AddMinutes(-5),
                DurationMinutes = 5m,
                CreatedAt = MidDay.AddMinutes(-5),
            });
        await db.SaveChangesAsync();

        var filtered = await sut.GetDowntimeParetoAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, "day", TestHelpers.TestUserId);

        Assert.Equal(10m, filtered.TotalDowntimeMinutes);
        Assert.Single(filtered.Items);
        Assert.Equal(10m, filtered.Items[0].Minutes);
    }
}
