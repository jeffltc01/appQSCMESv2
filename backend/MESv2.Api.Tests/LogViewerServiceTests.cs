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
    private static readonly DateTime StableTimestampUtc = new(2026, 1, 15, 18, 0, 0, DateTimeKind.Utc);

    private static string PlantLocalDate(DateTime utc)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz).ToString("yyyy-MM-dd");
    }

    private static ProductionRecord SeedProductionRecord(
        MesDbContext db, Guid wcId, string serial = "SN001",
        int tankSize = 500, DateTime? timestamp = null,
        string? resultText = null, string resultType = "AcceptReject")
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
            Timestamp = timestamp ?? DateTime.UtcNow
        };
        db.ProductionRecords.Add(record);

        if (resultText != null)
        {
            var wcpl = db.WorkCenterProductionLines.FirstOrDefault(w => w.WorkCenterId == wcId)
                ?? db.WorkCenterProductionLines.First();
            var charId = Guid.NewGuid();
            db.Characteristics.Add(new Characteristic { Id = charId, Code = "T" + serial[..2], Name = serial + " Char", ProductTypeId = null });
            var cpId = Guid.NewGuid();
            db.ControlPlans.Add(new ControlPlan
            {
                Id = cpId,
                CharacteristicId = charId,
                WorkCenterProductionLineId = wcpl.Id,
                IsEnabled = true,
                ResultType = resultType,
                IsGateCheck = false,
                IsActive = true
            });
            db.InspectionRecords.Add(new InspectionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = sn.Id,
                ProductionRecordId = record.Id,
                WorkCenterId = wcId,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = timestamp ?? DateTime.UtcNow,
                ControlPlanId = cpId,
                ResultText = resultText
            });
        }

        return record;
    }

    [Fact]
    public async Task GetRollsLog_ReturnsRecords_ForRollsWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcRollsId, "SHELL-001", 500, timestamp: StableTimestampUtc, resultText: "Pass", resultType: "PassFail");
        db.WelderLogs.Add(new WelderLog
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = r.Id,
            UserId = TestHelpers.TestUserId
        });
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
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
    public async Task GetRollsLog_DeduplicatesWeldersByUserId()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcRollsId, "SHELL-DUP", 500, timestamp: StableTimestampUtc);
        db.WelderLogs.Add(new WelderLog
        {
            Id = Guid.NewGuid(), ProductionRecordId = r.Id, UserId = TestHelpers.TestUserId
        });
        db.WelderLogs.Add(new WelderLog
        {
            Id = Guid.NewGuid(), ProductionRecordId = r.Id, UserId = TestHelpers.TestUserId
        });
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
        var result = await sut.GetRollsLogAsync(TestHelpers.PlantPlt1Id, today, today);

        var entry = result.First(e => e.ShellCode == "SHELL-DUP");
        Assert.Single(entry.Welders);
    }

    [Fact]
    public async Task GetRollsLog_ReturnsAnnotationBadges()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcRollsId, "SN-ANNOT", timestamp: StableTimestampUtc);
        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = r.Id,
            AnnotationTypeId = AnnotTypeCorrectionId,
            Status = AnnotationStatus.Open,
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
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

        var r = SeedProductionRecord(db, TestHelpers.wcFitupId, "OX", 500, timestamp: StableTimestampUtc);

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

        var today = PlantLocalDate(StableTimestampUtc);
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

        var r = SeedProductionRecord(db, TestHelpers.wcHydroId, "HYDRO-01", 500, timestamp: StableTimestampUtc, resultText: "Accept", resultType: "AcceptReject");
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
        var result = await sut.GetHydroLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        var entry = result[0];
        Assert.Equal("Accept", entry.Result);
        Assert.Equal(500, entry.TankSize);
        Assert.Equal(0, entry.DefectCount);
    }

    [Fact]
    public async Task GetHydroLog_ReturnsNameplate_And_AlphaCode()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        var r = SeedProductionRecord(db, TestHelpers.wcHydroId, "W00100001", 500, timestamp: StableTimestampUtc);

        var assemblySn = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "OX",
            PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(assemblySn);

        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = assemblySn.Id,
            ToSerialNumberId = r.SerialNumberId,
            ProductionRecordId = r.Id,
            Relationship = "hydro-marriage",
            Timestamp = DateTime.UtcNow
        });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = r.SerialNumberId,
            ToSerialNumberId = assemblySn.Id,
            ProductionRecordId = r.Id,
            Relationship = "NameplateToAssembly",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
        var result = await sut.GetHydroLogAsync(TestHelpers.PlantPlt1Id, today, today);

        Assert.NotEmpty(result);
        var entry = result[0];
        Assert.Equal("W00100001", entry.Nameplate);
        Assert.Equal("OX", entry.AlphaCode);
    }

    [Fact]
    public async Task GetRtXrayLog_ReturnsRecords_WithResult()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new LogViewerService(db);

        SeedProductionRecord(db, TestHelpers.wcRtXrayQueueId, "014540", 1000, timestamp: StableTimestampUtc, resultText: "Accept", resultType: "AcceptReject");
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
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

        var r = SeedProductionRecord(db, TestHelpers.wcSpotXrayId, "SPOT-01", 500, timestamp: StableTimestampUtc);
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
            Seam1ShotDateTime = DateTime.UtcNow,
            Seam2ShotNo = "2",
            Seam2ShotDateTime = DateTime.UtcNow,
            CreatedDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var today = PlantLocalDate(StableTimestampUtc);
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

    #region GetShotData Direct Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void GetShotData_ReturnsCorrectSeam(int seamNumber)
    {
        var inc = new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = Guid.NewGuid(),
            Seam1ShotNo = "S1", Seam1ShotDateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Seam2ShotNo = "S2", Seam2ShotDateTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Seam3ShotNo = "S3", Seam3ShotDateTime = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            Seam4ShotNo = "S4", Seam4ShotDateTime = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc),
        };

        var (shotNo, shotDate) = LogViewerService.GetShotData(inc, seamNumber);

        Assert.Equal($"S{seamNumber}", shotNo);
        Assert.Equal(new DateTime(2026, 1, seamNumber, 0, 0, 0, DateTimeKind.Utc), shotDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(-1)]
    public void GetShotData_OutOfRange_ReturnsNulls(int seamNumber)
    {
        var inc = new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = Guid.NewGuid(),
            Seam1ShotNo = "S1",
        };

        var (shotNo, shotDate) = LogViewerService.GetShotData(inc, seamNumber);

        Assert.Null(shotNo);
        Assert.Null(shotDate);
    }

    #endregion

    #region ResolveDateKey Direct Tests

    [Fact]
    public void ResolveDateKey_ShotDatePresent_UsesIt()
    {
        var shotDate = new DateTime(2026, 3, 15, 18, 0, 0, DateTimeKind.Utc);
        var result = LogViewerService.ResolveDateKey(shotDate, null, TimeZoneInfo.Utc);

        Assert.Equal("03/15/2026", result);
    }

    [Fact]
    public void ResolveDateKey_ShotDateNull_FallsBackToCreatedDate()
    {
        var created = new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);
        var result = LogViewerService.ResolveDateKey(null, created, TimeZoneInfo.Utc);

        Assert.Equal("06/20/2026", result);
    }

    [Fact]
    public void ResolveDateKey_BothNull_ReturnsNull()
    {
        var result = LogViewerService.ResolveDateKey(null, null, TimeZoneInfo.Utc);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveDateKey_AppliesTimezone()
    {
        var utcMidnight = new DateTime(2026, 7, 4, 3, 0, 0, DateTimeKind.Utc);
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        var result = LogViewerService.ResolveDateKey(utcMidnight, null, eastern);

        Assert.Equal("07/03/2026", result);
    }

    #endregion

    #region ComputeShotCounts Direct Tests

    [Fact]
    public void ComputeShotCounts_EmptyList_ReturnsEmpty()
    {
        var result = LogViewerService.ComputeShotCounts(
            new List<SpotXrayIncrement>(), TimeZoneInfo.Utc);

        Assert.Empty(result);
    }

    [Fact]
    public void ComputeShotCounts_MultipleSeamsOnSameDate_Aggregates()
    {
        var inc = new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = Guid.NewGuid(),
            Seam1ShotNo = "1", Seam1ShotDateTime = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            Seam2ShotNo = "2", Seam2ShotDateTime = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            Seam3ShotNo = "3", Seam3ShotDateTime = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc),
        };

        var result = LogViewerService.ComputeShotCounts(
            new List<SpotXrayIncrement> { inc }, TimeZoneInfo.Utc);

        Assert.Single(result);
        Assert.Equal("01/10/2026", result[0].Date);
        Assert.Equal(3, result[0].Count);
    }

    [Fact]
    public void ComputeShotCounts_EmptyShotNo_IsSkipped()
    {
        var inc = new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = Guid.NewGuid(),
            Seam1ShotNo = "1", Seam1ShotDateTime = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc),
            Seam2ShotNo = null,
            Seam3ShotNo = "",
        };

        var result = LogViewerService.ComputeShotCounts(
            new List<SpotXrayIncrement> { inc }, TimeZoneInfo.Utc);

        Assert.Single(result);
        Assert.Equal(1, result[0].Count);
    }

    [Fact]
    public void ComputeShotCounts_NoDates_FallsBackToCreatedDateTime()
    {
        var inc = new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = Guid.NewGuid(),
            Seam1ShotNo = "1",
            Seam1ShotDateTime = null,
            CreatedDateTime = new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc),
        };

        var result = LogViewerService.ComputeShotCounts(
            new List<SpotXrayIncrement> { inc }, TimeZoneInfo.Utc);

        Assert.Single(result);
        Assert.Equal("02/05/2026", result[0].Date);
    }

    [Fact]
    public void ComputeShotCounts_NoDatesAtAll_SkipsShot()
    {
        var inc = new SpotXrayIncrement
        {
            Id = Guid.NewGuid(),
            ManufacturingLogId = Guid.NewGuid(),
            Seam1ShotNo = "1",
            Seam1ShotDateTime = null,
            CreatedDateTime = null,
        };

        var result = LogViewerService.ComputeShotCounts(
            new List<SpotXrayIncrement> { inc }, TimeZoneInfo.Utc);

        Assert.Empty(result);
    }

    #endregion
}
