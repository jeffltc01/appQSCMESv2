using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MESv2.Api.Tests;

public class DowntimeServiceTests
{
    private static readonly Guid CorrectionNeededTypeId = Guid.Parse("a1000005-0000-0000-0000-000000000005");

    private static (MesDbContext db, DowntimeService sut, Guid categoryId, Guid reasonId, Guid wcplId) SetupWithReasons()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var sut = new DowntimeService(db);

        var category = new DowntimeReasonCategory
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            Name = "Equipment",
            IsActive = true,
            SortOrder = 0
        };
        db.DowntimeReasonCategories.Add(category);

        var reason = new DowntimeReason
        {
            Id = Guid.NewGuid(),
            DowntimeReasonCategoryId = category.Id,
            Name = "Breakdown",
            IsActive = true,
            SortOrder = 0
        };
        db.DowntimeReasons.Add(reason);

        var wcpl = db.WorkCenterProductionLines.First();
        wcpl.DowntimeTrackingEnabled = true;
        wcpl.DowntimeThresholdMinutes = 5;

        db.WorkCenterProductionLineDowntimeReasons.Add(new WorkCenterProductionLineDowntimeReason
        {
            Id = Guid.NewGuid(),
            WorkCenterProductionLineId = wcpl.Id,
            DowntimeReasonId = reason.Id
        });

        db.SaveChanges();

        return (db, sut, category.Id, reason.Id, wcpl.Id);
    }

    // ---- Category CRUD ----

    [Fact]
    public async Task CreateCategory_ReturnsNewCategory()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new DowntimeService(db);

        var result = await sut.CreateCategoryAsync(new CreateDowntimeReasonCategoryDto
        {
            PlantId = TestHelpers.PlantPlt1Id,
            Name = "Equipment",
            SortOrder = 1
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Equipment", result.Name);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetCategories_ReturnsCategoriesForPlant()
    {
        var (db, sut, categoryId, _, _) = SetupWithReasons();

        var categories = await sut.GetCategoriesAsync(TestHelpers.PlantPlt1Id);

        Assert.Single(categories);
        Assert.Equal("Equipment", categories[0].Name);
        Assert.Single(categories[0].Reasons);
    }

    [Fact]
    public async Task UpdateCategory_ChangesNameAndSortOrder()
    {
        var (db, sut, categoryId, _, _) = SetupWithReasons();

        var result = await sut.UpdateCategoryAsync(categoryId, new UpdateDowntimeReasonCategoryDto
        {
            Name = "Mechanical",
            SortOrder = 5,
            IsActive = true
        });

        Assert.NotNull(result);
        Assert.Equal("Mechanical", result!.Name);
        Assert.Equal(5, result.SortOrder);
    }

    [Fact]
    public async Task DeleteCategory_SoftDeletes()
    {
        var (db, sut, categoryId, _, _) = SetupWithReasons();

        var success = await sut.DeleteCategoryAsync(categoryId);

        Assert.True(success);
        var entity = await db.DowntimeReasonCategories.FindAsync(categoryId);
        Assert.False(entity!.IsActive);
    }

    // ---- Reason CRUD ----

    [Fact]
    public async Task CreateReason_ReturnsNewReason()
    {
        var (db, sut, categoryId, _, _) = SetupWithReasons();

        var result = await sut.CreateReasonAsync(new CreateDowntimeReasonDto
        {
            DowntimeReasonCategoryId = categoryId,
            Name = "Material Shortage",
            SortOrder = 2
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Material Shortage", result.Name);
        Assert.Equal("Equipment", result.CategoryName);
    }

    [Fact]
    public async Task GetReasons_ReturnsReasonsForPlant()
    {
        var (db, sut, _, _, _) = SetupWithReasons();

        var reasons = await sut.GetReasonsAsync(TestHelpers.PlantPlt1Id);

        Assert.Single(reasons);
        Assert.Equal("Breakdown", reasons[0].Name);
    }

    [Fact]
    public async Task DeleteReason_SoftDeletes()
    {
        var (db, sut, _, reasonId, _) = SetupWithReasons();

        var success = await sut.DeleteReasonAsync(reasonId);

        Assert.True(success);
        var entity = await db.DowntimeReasons.FindAsync(reasonId);
        Assert.False(entity!.IsActive);
    }

    // ---- Downtime Config ----

    [Fact]
    public async Task GetDowntimeConfig_ReturnsConfigAndReasons()
    {
        var (db, sut, _, _, wcplId) = SetupWithReasons();
        var wcpl = await db.WorkCenterProductionLines.FindAsync(wcplId);

        var config = await sut.GetDowntimeConfigAsync(wcpl!.WorkCenterId, wcpl.ProductionLineId);

        Assert.NotNull(config);
        Assert.True(config!.DowntimeTrackingEnabled);
        Assert.Equal(5, config.DowntimeThresholdMinutes);
        Assert.Single(config.ApplicableReasons);
        Assert.Equal("Breakdown", config.ApplicableReasons[0].Name);
    }

    [Fact]
    public async Task UpdateDowntimeConfig_ChangesThreshold()
    {
        var (db, sut, _, _, wcplId) = SetupWithReasons();
        var wcpl = await db.WorkCenterProductionLines.FindAsync(wcplId);

        var config = await sut.UpdateDowntimeConfigAsync(wcpl!.WorkCenterId, wcpl.ProductionLineId,
            new UpdateDowntimeConfigDto { DowntimeTrackingEnabled = true, DowntimeThresholdMinutes = 10 });

        Assert.NotNull(config);
        Assert.Equal(10, config!.DowntimeThresholdMinutes);
    }

    [Fact]
    public async Task SetDowntimeReasons_ReplacesExistingReasons()
    {
        var (db, sut, categoryId, _, wcplId) = SetupWithReasons();
        var wcpl = await db.WorkCenterProductionLines.FindAsync(wcplId);

        var newReason = new DowntimeReason
        {
            Id = Guid.NewGuid(),
            DowntimeReasonCategoryId = categoryId,
            Name = "Tooling Change",
            IsActive = true,
            SortOrder = 1
        };
        db.DowntimeReasons.Add(newReason);
        await db.SaveChangesAsync();

        var success = await sut.SetDowntimeReasonsAsync(wcpl!.WorkCenterId, wcpl.ProductionLineId,
            new List<Guid> { newReason.Id });

        Assert.True(success);

        var config = await sut.GetDowntimeConfigAsync(wcpl.WorkCenterId, wcpl.ProductionLineId);
        Assert.Single(config!.ApplicableReasons);
        Assert.Equal("Tooling Change", config.ApplicableReasons[0].Name);
    }

    // ---- Downtime Events ----

    [Fact]
    public async Task CreateDowntimeEvent_WithReason_ReturnsEvent()
    {
        var (db, sut, _, reasonId, wcplId) = SetupWithReasons();

        var startedAt = DateTime.UtcNow.AddMinutes(-12);
        var endedAt = DateTime.UtcNow;

        var result = await sut.CreateDowntimeEventAsync(new CreateDowntimeEventDto
        {
            WorkCenterProductionLineId = wcplId,
            OperatorUserId = TestHelpers.TestUserId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DowntimeReasonId = reasonId,
            IsAutoGenerated = false
        }, TestHelpers.TestUserId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Breakdown", result.DowntimeReasonName);
        Assert.True(result.DurationMinutes > 11);
        Assert.False(result.IsAutoGenerated);
    }

    [Fact]
    public async Task CreateDowntimeEvent_AutoGenerated_CreatesAnnotation()
    {
        var (db, sut, _, _, wcplId) = SetupWithReasons();

        var startedAt = DateTime.UtcNow.AddMinutes(-60);
        var endedAt = DateTime.UtcNow;

        var result = await sut.CreateDowntimeEventAsync(new CreateDowntimeEventDto
        {
            WorkCenterProductionLineId = wcplId,
            OperatorUserId = TestHelpers.TestUserId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DowntimeReasonId = null,
            IsAutoGenerated = true
        }, TestHelpers.TestUserId);

        Assert.True(result.IsAutoGenerated);
        Assert.Null(result.DowntimeReasonId);

        var annotation = await db.Annotations
            .Include(a => a.AnnotationType)
            .FirstOrDefaultAsync(a => a.DowntimeEventId == result.Id);

        Assert.NotNull(annotation);
        Assert.Equal("Correction Needed", annotation!.AnnotationType.Name);
        Assert.True(annotation.Flag);
        Assert.Contains("Auto-generated", annotation.Notes);
        Assert.Contains("Auto-logout", annotation.SystemTypeInfo);
    }

    [Fact]
    public async Task CreateDowntimeEvent_CalculatesDurationCorrectly()
    {
        var (db, sut, _, reasonId, wcplId) = SetupWithReasons();

        var startedAt = new DateTime(2026, 2, 22, 10, 0, 0, DateTimeKind.Utc);
        var endedAt = new DateTime(2026, 2, 22, 10, 19, 0, DateTimeKind.Utc);

        var result = await sut.CreateDowntimeEventAsync(new CreateDowntimeEventDto
        {
            WorkCenterProductionLineId = wcplId,
            OperatorUserId = TestHelpers.TestUserId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            DowntimeReasonId = reasonId,
            IsAutoGenerated = false
        }, TestHelpers.TestUserId);

        Assert.Equal(19m, result.DurationMinutes);
    }
}
