using Microsoft.Extensions.Logging;
using Moq;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class SupervisorDashboardServiceTests
{
    private static readonly Guid CharLongSeamId = Guid.Parse("c1000001-0000-0000-0000-000000000001");
    private static readonly Guid DefectCodeId = Guid.Parse("d1010001-0000-0000-0000-000000000001");
    private static readonly Guid DefectLocationId = Guid.Parse("d1000001-0000-0000-0000-000000000001");
    private static readonly Guid NoteAnnotationTypeId = Guid.Parse("a1000001-0000-0000-0000-000000000001");

    // Cleveland plant uses America/Chicago.
    // Pick a date/time that is solidly mid-day in Central Time = 2026-06-10 18:00 UTC = 12:00 CDT
    private const string TestDate = "2026-06-10";
    private static readonly DateTime MidDay = new(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);
    // Three days earlier (same week, still mid-day)
    private static readonly DateTime EarlierInWeek = new(2026, 6, 8, 18, 0, 0, DateTimeKind.Utc);

    private SupervisorDashboardService CreateService(Data.MesDbContext db) =>
        new(db, new Mock<ILogger<SupervisorDashboardService>>().Object);

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
    public async Task GetMetrics_ReturnsCorrectDayAndWeekCounts()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30));
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-60));
        SeedRecord(db, TestHelpers.wcRollsId, EarlierInWeek);
        await db.SaveChangesAsync();

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Equal(2, result.DayCount);
        Assert.Equal(3, result.WeekCount);
    }

    [Fact]
    public async Task GetMetrics_HourlyCounts_Has24Entries()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-10));
        await db.SaveChangesAsync();

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Equal(24, result.HourlyCounts.Count);
        Assert.Contains(result.HourlyCounts, h => h.Count > 0);
    }

    [Fact]
    public async Task GetMetrics_FpyCalculation_PerfectYield()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30));
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-60));
        await db.SaveChangesAsync();

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.NotNull(result.DayFPY);
        Assert.Equal(100.0m, result.DayFPY);
        Assert.Equal(0, result.DayDefects);
    }

    [Fact]
    public async Task GetMetrics_FpyCalculation_WithDefect()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var snWithDefect = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30), snId: snWithDefect);
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-60));

        db.DefectLogs.Add(new DefectLog
        {
            Id = Guid.NewGuid(),
            SerialNumberId = snWithDefect,
            DefectCodeId = DefectCodeId,
            CharacteristicId = CharLongSeamId,
            LocationId = DefectLocationId,
            Timestamp = MidDay.AddMinutes(-20),
            CreatedAt = MidDay.AddMinutes(-20),
        });
        await db.SaveChangesAsync();

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.NotNull(result.DayFPY);
        Assert.Equal(50.0m, result.DayFPY);
        Assert.Equal(1, result.DayDefects);
    }

    [Fact]
    public async Task GetMetrics_FpyNull_ForNonApplicableWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcHydroId, MidDay.AddMinutes(-30));
        await db.SaveChangesAsync();

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcHydroId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Null(result.DayFPY);
        Assert.Null(result.WeekFPY);
    }

    [Fact]
    public async Task GetMetrics_OperatorFilter_NarrowsResults()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var otherOperatorId = Guid.Parse("88888888-8888-8888-8888-888888888801");

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30), TestHelpers.TestUserId);
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-60), otherOperatorId);
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-90), otherOperatorId);
        await db.SaveChangesAsync();

        var filtered = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate, otherOperatorId);

        Assert.Equal(2, filtered.DayCount);
        Assert.True(filtered.Operators.Count >= 2);
    }

    [Fact]
    public async Task GetMetrics_AvgTimeBetweenScans_Calculated()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-60));
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-30));
        SeedRecord(db, TestHelpers.wcRollsId, MidDay);
        await db.SaveChangesAsync();

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.True(result.DayAvgTimeBetweenScans > 0);
    }

    [Fact]
    public async Task GetRecords_ReturnsAnnotations()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var snId = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-10), snId: snId);
        await db.SaveChangesAsync();

        var recId = db.ProductionRecords.First(r => r.SerialNumberId == snId).Id;

        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = recId,
            AnnotationTypeId = NoteAnnotationTypeId,
            Flag = true,
            Notes = "test note",
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = MidDay,
        });
        await db.SaveChangesAsync();

        var records = await sut.GetRecordsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Single(records);
        Assert.Single(records[0].Annotations);
        Assert.Equal("Note", records[0].Annotations[0].TypeName);
    }

    [Fact]
    public async Task SubmitAnnotation_CreatesAnnotation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-10));
        await db.SaveChangesAsync();

        var recId = db.ProductionRecords.OrderByDescending(r => r.Timestamp).First().Id;

        var result = await sut.SubmitAnnotationAsync(TestHelpers.TestUserId, new CreateSupervisorAnnotationRequest
        {
            RecordIds = new List<Guid> { recId },
            AnnotationTypeId = NoteAnnotationTypeId,
            Comment = "Test annotation",
        });

        Assert.Equal(1, result.AnnotationsCreated);
        var annotation = db.Annotations.FirstOrDefault(a => a.ProductionRecordId == recId);
        Assert.NotNull(annotation);
        Assert.Equal(NoteAnnotationTypeId, annotation.AnnotationTypeId);
    }

    [Fact]
    public async Task SubmitAnnotation_Idempotent_DoesNotDuplicate()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-10));
        await db.SaveChangesAsync();

        var recId = db.ProductionRecords.OrderByDescending(r => r.Timestamp).First().Id;

        await sut.SubmitAnnotationAsync(TestHelpers.TestUserId, new CreateSupervisorAnnotationRequest
        {
            RecordIds = new List<Guid> { recId },
            AnnotationTypeId = NoteAnnotationTypeId,
            Comment = "First",
        });

        var secondResult = await sut.SubmitAnnotationAsync(TestHelpers.TestUserId, new CreateSupervisorAnnotationRequest
        {
            RecordIds = new List<Guid> { recId },
            AnnotationTypeId = NoteAnnotationTypeId,
            Comment = "Second",
        });

        Assert.Equal(0, secondResult.AnnotationsCreated);
        Assert.Single(db.Annotations.Where(a => a.ProductionRecordId == recId && a.AnnotationTypeId == NoteAnnotationTypeId));
    }

    [Fact]
    public async Task GetMetrics_WeekDailyCounts_Has7Entries()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetMetricsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Equal(7, result.WeekDailyCounts.Count);
    }
}
