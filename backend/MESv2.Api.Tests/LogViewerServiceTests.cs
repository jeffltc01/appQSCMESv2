using MESv2.Api.Data;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class LogViewerServiceTests
{
    private static readonly Guid WctRollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WctFitupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid WctHydroId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
    private static readonly Guid WctXrayId = Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");
    private static readonly Guid WctSpotXrayId = Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3");

    private static readonly Guid ProductTypeId = Guid.Parse("a3333333-3333-3333-3333-333333333333");
    private static readonly Guid AnnotTypeNoteId = Guid.Parse("a1000001-0000-0000-0000-000000000001");
    private static readonly Guid AnnotTypeCorrectionId = Guid.Parse("a1000005-0000-0000-0000-000000000005");

    private static ProductionRecord SeedProductionRecord(
        MesDbContext db, Guid wcId, string serial = "SN001",
        int tankSize = 500, string? inspectionResult = null,
        DateTime? timestamp = null)
    {
        var product = db.Products.FirstOrDefault(p => p.TankSize == tankSize)
            ?? new Product
            {
                Id = Guid.NewGuid(),
                ProductNumber = $"P-{tankSize}",
                TankSize = tankSize,
                TankType = "Shell",
                ProductTypeId = ProductTypeId
            };
        if (!db.Products.Local.Contains(product) && db.Entry(product).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            db.Products.Add(product);

        var sn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = serial,
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            HeatNumber = "H100",
            CoilNumber = "C200",
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(sn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            WorkCenterId = wcId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp ?? DateTime.UtcNow,
            InspectionResult = inspectionResult
        };
        db.ProductionRecords.Add(record);

        return record;
    }

    [Fact]
    public async Task GetRollsLog_ReturnsRecords_ForRollsWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcRollsId, "SHELL-001", 500, "Pass");
        db.WelderLogs.Add(new WelderLog
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = r.Id,
            UserId = TestHelpers.TestUserId
        });
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetRollsLogAsync(
            TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        var entry = result[0];
        Assert.Equal("SHELL-001", entry.ShellCode);
        Assert.Equal(500, entry.TankSize);
        Assert.Equal("Pass", entry.Thickness);
        Assert.Contains("Coil:C200", entry.CoilHeatLot);
        Assert.Contains("Heat:H100", entry.CoilHeatLot);
        Assert.Single(entry.Welders);
    }

    [Fact]
    public async Task GetRollsLog_ReturnsAnnotationBadges()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcRollsId, "SN-ANNOT");
        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = r.Id,
            AnnotationTypeId = AnnotTypeCorrectionId,
            Flag = true,
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetRollsLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        Assert.Single(result[0].Annotations);
        Assert.Equal("C", result[0].Annotations[0].Abbreviation);
    }

    [Fact]
    public async Task GetFitupLog_ReturnsRecords_WithTraceability()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcFitupId, "OX", 500);

        var headSn = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "HEAD-01",
            PlantId = TestHelpers.PlantPlt1Id,
            CoilNumber = "HC1", HeatNumber = "HH1", CreatedAt = DateTime.UtcNow
        };
        var shellSn = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "020401",
            PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.AddRange(headSn, shellSn);

        db.TraceabilityLogs.AddRange(
            new TraceabilityLog
            {
                Id = Guid.NewGuid(), ProductionRecordId = r.Id,
                FromSerialNumberId = headSn.Id, Relationship = "Head",
                TankLocation = "Left", Timestamp = DateTime.UtcNow
            },
            new TraceabilityLog
            {
                Id = Guid.NewGuid(), ProductionRecordId = r.Id,
                FromSerialNumberId = shellSn.Id, Relationship = "ShellToAssembly",
                TankLocation = "1", Timestamp = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetFitupLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        var entry = result[0];
        Assert.Equal("OX", entry.AlphaCode);
        Assert.NotNull(entry.HeadNo1);
        Assert.Contains("Coil:HC1", entry.HeadNo1);
        Assert.Equal("020401", entry.ShellNo1);
    }

    [Fact]
    public async Task GetHydroLog_ReturnsRecords_WithDefects()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcHydroId, "HYDRO-01", 500, "Accept");
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetHydroLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        var entry = result[0];
        Assert.Equal("Accept", entry.Result);
        Assert.Equal(500, entry.TankSize);
        Assert.Equal(0, entry.DefectCount);
    }

    [Fact]
    public async Task GetRtXrayLog_ReturnsRecords_WithResult()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        SeedProductionRecord(db, TestHelpers.wcRtXrayQueueId, "014540", 1000, "Accept");
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetRtXrayLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        Assert.Equal("Accept", result[0].Result);
        Assert.Equal("014540", result[0].ShellCode);
    }

    [Fact]
    public async Task GetSpotXrayLog_ReturnsEntries_WithShotCounts()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcSpotXrayId, "SPOT-01", 500);
        db.SpotXrayIncrements.Add(new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = r.Id,
            IncrementNo = "1",
            OverallStatus = "Accept",
            LaneNo = "1",
            TankSize = 500,
            InspectTank = "020401 (OX)",
            Seam1ShotNo = "1",
            Seam1ShotDateTime = DateTime.UtcNow.ToString("O"),
            Seam2ShotNo = "2",
            Seam2ShotDateTime = DateTime.UtcNow.ToString("O"),
            CreatedDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetSpotXrayLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result.Entries);
        Assert.Equal("Accept", result.Entries[0].Result);
        Assert.NotEmpty(result.ShotCounts);
        Assert.True(result.ShotCounts[0].Count >= 2);
    }

    [Fact]
    public async Task GetRollsLog_DateFiltering_ExcludesOutOfRange()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        SeedProductionRecord(db, TestHelpers.wcRollsId, "IN-RANGE",
            timestamp: DateTime.UtcNow);
        SeedProductionRecord(db, TestHelpers.wcRollsId, "OUT-OF-RANGE",
            timestamp: DateTime.UtcNow.AddDays(-10));
        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await sut.GetRollsLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.All(result, e => Assert.NotEqual("OUT-OF-RANGE", e.ShellCode));
    }

    [Fact]
    public async Task GetRollsLog_NoRecords_ReturnsEmpty()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var result = await sut.GetRollsLogAsync(
            TestHelpers.PlantPlt1Id, "2020-01-01", "2020-01-02");

        Assert.Empty(result);
    }
}
