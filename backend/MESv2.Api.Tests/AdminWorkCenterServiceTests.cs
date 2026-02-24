using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AdminWorkCenterServiceTests
{
    private AdminWorkCenterService CreateService(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new AdminWorkCenterService(db);
    }

    [Fact]
    public async Task GetAllGrouped_ReturnsWorkCenters()
    {
        var service = CreateService(out _);
        var groups = await service.GetAllGroupedAsync();

        Assert.NotEmpty(groups);
        Assert.All(groups, g =>
        {
            Assert.NotEqual(Guid.Empty, g.GroupId);
            Assert.False(string.IsNullOrEmpty(g.BaseName));
            Assert.NotEmpty(g.SiteConfigs);
        });
    }

    [Fact]
    public async Task CreateWorkCenter_Succeeds()
    {
        var service = CreateService(out _);
        var dto = new CreateWorkCenterDto
        {
            Name = "Service Test WC",
            WorkCenterTypeId = TestHelpers.WorkCenterTypeRollsId,
            DataEntryType = "Rolls",
        };

        var result = await service.CreateWorkCenterAsync(dto);

        Assert.Equal("Service Test WC", result.BaseName);
        Assert.Equal("Rolls", result.DataEntryType);
        Assert.NotEqual(Guid.Empty, result.GroupId);
    }

    [Fact]
    public async Task CreateWorkCenter_DuplicateName_Throws()
    {
        var service = CreateService(out _);
        var dto = new CreateWorkCenterDto
        {
            Name = "Rolls",
            WorkCenterTypeId = TestHelpers.WorkCenterTypeRollsId,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateWorkCenterAsync(dto));
    }

    [Fact]
    public async Task UpdateGroup_NotFound_ReturnsNull()
    {
        var service = CreateService(out _);
        var dto = new UpdateWorkCenterGroupDto { BaseName = "X", DataEntryType = "Rolls" };

        var result = await service.UpdateGroupAsync(Guid.NewGuid(), dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteProductionLineConfig_NotFound_ReturnsFalse()
    {
        var service = CreateService(out _);

        var result = await service.DeleteProductionLineConfigAsync(TestHelpers.wcRollsId, Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task CreateProductionLineConfig_Succeeds()
    {
        var service = CreateService(out var db);

        var newPlId = Guid.NewGuid();
        db.ProductionLines.Add(new MESv2.Api.Models.ProductionLine
        {
            Id = newPlId,
            Name = "Svc Test Line",
            PlantId = TestHelpers.PlantPlt1Id
        });
        await db.SaveChangesAsync();

        var dto = new CreateWorkCenterProductionLineDto
        {
            ProductionLineId = newPlId,
            DisplayName = "Rolls - Svc Test",
            NumberOfWelders = 2,
            EnableWorkCenterChecklist = true,
            EnableSafetyChecklist = false,
        };

        var result = await service.CreateProductionLineConfigAsync(TestHelpers.wcRollsId, dto);

        Assert.Equal("Rolls - Svc Test", result.DisplayName);
        Assert.Equal(2, result.NumberOfWelders);
        Assert.Equal("Svc Test Line", result.ProductionLineName);
        Assert.True(result.EnableWorkCenterChecklist);
        Assert.False(result.EnableSafetyChecklist);
    }

    [Fact]
    public async Task UpdateProductionLineConfig_Succeeds()
    {
        var service = CreateService(out _);
        var dto = new UpdateWorkCenterProductionLineDto
        {
            DisplayName = "Updated via Service",
            NumberOfWelders = 7,
            DowntimeTrackingEnabled = true,
            DowntimeThresholdMinutes = 10,
            EnableWorkCenterChecklist = true,
            EnableSafetyChecklist = true,
        };

        var result = await service.UpdateProductionLineConfigAsync(
            TestHelpers.wcRollsId, TestHelpers.ProductionLine1Plt1Id, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated via Service", result!.DisplayName);
        Assert.Equal(7, result.NumberOfWelders);
        Assert.True(result.DowntimeTrackingEnabled);
        Assert.Equal(10, result.DowntimeThresholdMinutes);
        Assert.True(result.EnableWorkCenterChecklist);
        Assert.True(result.EnableSafetyChecklist);
    }

    [Fact]
    public async Task DeleteProductionLineConfig_Succeeds()
    {
        var service = CreateService(out var db);
        var countBefore = await db.WorkCenterProductionLines
            .CountAsync(x => x.WorkCenterId == TestHelpers.wcRollsId);

        var result = await service.DeleteProductionLineConfigAsync(
            TestHelpers.wcRollsId, TestHelpers.ProductionLine1Plt1Id);

        Assert.True(result);
        var countAfter = await db.WorkCenterProductionLines
            .CountAsync(x => x.WorkCenterId == TestHelpers.wcRollsId);
        Assert.Equal(countBefore - 1, countAfter);
    }
}
