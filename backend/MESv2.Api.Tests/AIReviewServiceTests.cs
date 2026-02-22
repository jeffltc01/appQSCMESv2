using Microsoft.Extensions.Logging.Abstractions;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AIReviewServiceTests
{
    private static readonly Guid AIReviewAnnotationTypeId =
        Guid.Parse("a1000002-0000-0000-0000-000000000002");

    private static AIReviewService CreateService(Data.MesDbContext db) =>
        new(db, NullLogger<AIReviewService>.Instance);

    /// <summary>
    /// Returns a well-known date and midday timestamp that avoids timezone edge cases.
    /// "2026-06-15" at 18:00 UTC = noon in Chicago (America/Chicago is UTC-6 in summer).
    /// </summary>
    private static readonly string TestDate = "2026-06-15";
    private static readonly DateTime TestTimestamp = new(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc);

    private static ProductionRecord SeedProductionRecord(
        Data.MesDbContext db, Guid? wcId = null, DateTime? timestamp = null)
    {
        var sn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-" + Guid.NewGuid().ToString("N")[..6],
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
        };
        db.SerialNumbers.Add(sn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            WorkCenterId = wcId ?? TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp ?? TestTimestamp,
        };
        db.ProductionRecords.Add(record);
        db.SaveChanges();
        return record;
    }

    [Fact]
    public async Task GetRecords_ReturnsProductionRecordsForWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedProductionRecord(db, TestHelpers.wcRollsId);
        SeedProductionRecord(db, TestHelpers.wcRollsId);
        SeedProductionRecord(db, TestHelpers.wcLongSeamId);

        var records = await sut.GetRecordsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Equal(2, records.Count);
        Assert.All(records, r => Assert.False(r.AlreadyReviewed));
    }

    [Fact]
    public async Task GetRecords_MarksAlreadyReviewedRecords()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var r1 = SeedProductionRecord(db, TestHelpers.wcRollsId);
        SeedProductionRecord(db, TestHelpers.wcRollsId);

        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = r1.Id,
            AnnotationTypeId = AIReviewAnnotationTypeId,
            Flag = true,
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();

        var records = await sut.GetRecordsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, TestDate);

        Assert.Equal(2, records.Count);
        var reviewed = records.Single(r => r.Id == r1.Id);
        var notReviewed = records.Single(r => r.Id != r1.Id);
        Assert.True(reviewed.AlreadyReviewed);
        Assert.False(notReviewed.AlreadyReviewed);
    }

    [Fact]
    public async Task SubmitReview_CreatesAnnotationsForSelectedRecords()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var r1 = SeedProductionRecord(db, TestHelpers.wcRollsId);
        var r2 = SeedProductionRecord(db, TestHelpers.wcRollsId);

        var result = await sut.SubmitReviewAsync(TestHelpers.TestUserId, new CreateAIReviewRequest
        {
            ProductionRecordIds = new List<Guid> { r1.Id, r2.Id },
            Comment = "Looks good",
        });

        Assert.Equal(2, result.AnnotationsCreated);

        var annotations = db.Annotations
            .Where(a => a.AnnotationTypeId == AIReviewAnnotationTypeId)
            .ToList();
        Assert.Equal(2, annotations.Count);
        Assert.All(annotations, a =>
        {
            Assert.Equal("Looks good", a.Notes);
            Assert.True(a.Flag);
            Assert.Equal(TestHelpers.TestUserId, a.InitiatedByUserId);
        });
    }

    [Fact]
    public async Task SubmitReview_SkipsAlreadyReviewedRecords()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var r1 = SeedProductionRecord(db, TestHelpers.wcRollsId);
        var r2 = SeedProductionRecord(db, TestHelpers.wcRollsId);

        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = r1.Id,
            AnnotationTypeId = AIReviewAnnotationTypeId,
            Flag = true,
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow,
        });
        db.SaveChanges();

        var result = await sut.SubmitReviewAsync(TestHelpers.TestUserId, new CreateAIReviewRequest
        {
            ProductionRecordIds = new List<Guid> { r1.Id, r2.Id },
            Comment = "Review pass",
        });

        Assert.Equal(1, result.AnnotationsCreated);
    }

    [Fact]
    public async Task SubmitReview_NoComment_CreatesAnnotationsWithNullNotes()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var r1 = SeedProductionRecord(db, TestHelpers.wcRollsId);

        var result = await sut.SubmitReviewAsync(TestHelpers.TestUserId, new CreateAIReviewRequest
        {
            ProductionRecordIds = new List<Guid> { r1.Id },
        });

        Assert.Equal(1, result.AnnotationsCreated);
        var annotation = db.Annotations.Single(a => a.ProductionRecordId == r1.Id);
        Assert.Null(annotation.Notes);
    }

    [Fact]
    public async Task GetRecords_ReturnsEmptyForNoRecords()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var records = await sut.GetRecordsAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id, today);

        Assert.Empty(records);
    }
}
