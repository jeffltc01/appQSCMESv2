using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AnnotationServiceTests
{
    private static readonly Guid AnnotationTypeNoteId = Guid.Parse("a1000001-0000-0000-0000-000000000001");

    private static (AnnotationService service, MesDbContext db) CreateService()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var service = new AnnotationService(db);
        return (service, db);
    }

    private static Annotation SeedAnnotation(MesDbContext db)
    {
        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            AnnotationTypeId = AnnotationTypeNoteId,
            Status = AnnotationStatus.Open,
            Notes = "Test note",
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow,
            LinkedEntityType = "Plant",
            LinkedEntityId = TestHelpers.PlantPlt1Id,
        };
        db.Annotations.Add(annotation);
        db.SaveChanges();
        return annotation;
    }

    private static ProductionRecord SeedProductionRecord(MesDbContext db)
    {
        var serial = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-TEST-001",
            PlantId = TestHelpers.PlantPlt1Id,
        };
        db.SerialNumbers.Add(serial);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serial.Id,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow,
        };
        db.ProductionRecords.Add(record);
        db.SaveChanges();
        return record;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAnnotations()
    {
        var (service, db) = CreateService();
        SeedAnnotation(db);

        var result = await service.GetAllAsync(null);

        Assert.NotEmpty(result);
        Assert.Equal("Test note", result[0].Notes);
        Assert.Equal("Note", result[0].AnnotationTypeName);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesAnnotation()
    {
        var (service, db) = CreateService();
        var dto = new CreateAnnotationDto
        {
            AnnotationTypeId = AnnotationTypeNoteId,
            Notes = "New annotation",
            InitiatedByUserId = TestHelpers.TestUserId,
            LinkedEntityType = "Plant",
            LinkedEntityId = TestHelpers.PlantPlt1Id,
        };

        var result = await service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("New annotation", result.Notes);
        Assert.Equal("Note", result.AnnotationTypeName);
        Assert.Equal("Open", result.Status);
        Assert.Single(db.Annotations);
    }

    [Fact]
    public async Task CreateAsync_InvalidAnnotationType_Throws()
    {
        var (service, _) = CreateService();
        var dto = new CreateAnnotationDto
        {
            AnnotationTypeId = Guid.NewGuid(),
            InitiatedByUserId = TestHelpers.TestUserId,
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        Assert.Equal("Invalid annotation type.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidUser_Throws()
    {
        var (service, _) = CreateService();
        var dto = new CreateAnnotationDto
        {
            AnnotationTypeId = AnnotationTypeNoteId,
            InitiatedByUserId = Guid.NewGuid(),
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        Assert.Equal("Invalid user.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidLinkedEntity_Throws()
    {
        var (service, _) = CreateService();
        var dto = new CreateAnnotationDto
        {
            AnnotationTypeId = AnnotationTypeNoteId,
            InitiatedByUserId = TestHelpers.TestUserId,
            LinkedEntityType = "Plant",
            LinkedEntityId = Guid.NewGuid(),
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(dto));
        Assert.Equal("Plant not found.", ex.Message);
    }

    [Fact]
    public async Task CreateForProductionRecordAsync_Succeeds()
    {
        var (service, db) = CreateService();
        var record = SeedProductionRecord(db);
        var dto = new CreateLogAnnotationDto
        {
            ProductionRecordId = record.Id,
            AnnotationTypeId = AnnotationTypeNoteId,
            Notes = "Log annotation",
            InitiatedByUserId = TestHelpers.TestUserId,
        };

        var result = await service.CreateForProductionRecordAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-TEST-001", result.SerialNumber);
        Assert.Equal("Log annotation", result.Notes);
        Assert.Equal("Open", result.Status);
    }

    [Fact]
    public async Task CreateForProductionRecordAsync_MissingRecord_Throws()
    {
        var (service, _) = CreateService();
        var dto = new CreateLogAnnotationDto
        {
            ProductionRecordId = Guid.NewGuid(),
            AnnotationTypeId = AnnotationTypeNoteId,
            InitiatedByUserId = TestHelpers.TestUserId,
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateForProductionRecordAsync(dto));
        Assert.Equal("Production record not found.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        var (service, _) = CreateService();
        var dto = new UpdateAnnotationDto
        {
            Status = "Closed",
            Notes = "updated",
        };

        var result = await service.UpdateAsync(Guid.NewGuid(), dto);

        Assert.Null(result);
    }
}
