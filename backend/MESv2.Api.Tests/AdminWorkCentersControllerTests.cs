using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using Moq;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AdminWorkCentersControllerTests
{
    private WorkCentersController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var mockService = new Mock<IWorkCenterService>();
        var mockXrayService = new Mock<IXrayQueueService>();
        var mockLogger = new Mock<ILogger<WorkCentersController>>();
        return new WorkCentersController(mockService.Object, mockXrayService.Object, db, mockLogger.Object);
    }

    [Fact]
    public async Task GetAllAdmin_ReturnsWorkCenters()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllAdmin(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminWorkCenterDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 1);
        Assert.All(list, wc => Assert.False(string.IsNullOrEmpty(wc.Name)));
    }

    [Fact]
    public async Task GetAllGrouped_ReturnsGroupedWorkCenters()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllGrouped(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminWorkCenterGroupDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 1);
        Assert.All(list, g =>
        {
            Assert.NotEqual(Guid.Empty, g.GroupId);
            Assert.False(string.IsNullOrEmpty(g.BaseName));
            Assert.NotEmpty(g.SiteConfigs);
        });
    }

    [Fact]
    public async Task GetAllGrouped_EachGroupHasSiteConfigs()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllGrouped(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsAssignableFrom<IEnumerable<AdminWorkCenterGroupDto>>(ok.Value).ToList();
        foreach (var group in groups)
        {
            Assert.All(group.SiteConfigs, sc =>
            {
                Assert.NotEqual(Guid.Empty, sc.WorkCenterId);
            });
        }
    }

    [Fact]
    public async Task GetAllGrouped_SiteConfigsSiteNameIsPlantName_WhenWCHasProductionLines()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllGrouped(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsAssignableFrom<IEnumerable<AdminWorkCenterGroupDto>>(ok.Value).ToList();

        var rollsGroup = groups.First(g => g.BaseName == "Rolls");
        var siteNames = rollsGroup.SiteConfigs.Select(sc => sc.SiteName).ToList();
        Assert.Contains("Cleveland", siteNames);
        Assert.DoesNotContain("Rolls", siteNames);
    }

    [Fact]
    public async Task GetAllGrouped_WCWithoutProductionLines_FallsBackToWCName()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllGrouped(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var groups = Assert.IsAssignableFrom<IEnumerable<AdminWorkCenterGroupDto>>(ok.Value).ToList();

        var rollsMatGroup = groups.First(g => g.BaseName == "Rolls Material");
        Assert.Single(rollsMatGroup.SiteConfigs);
        Assert.Equal("Rolls Material", rollsMatGroup.SiteConfigs[0].SiteName);
    }

    [Fact]
    public async Task UpdateGroup_ModifiesBaseNameAndDataEntryType()
    {
        var controller = CreateController(out var db);
        var firstWc = await db.WorkCenters.FirstAsync(w => w.Name == "Rolls");
        var groupId = firstWc.Id;

        var dto = new UpdateWorkCenterGroupDto
        {
            BaseName = "Modified Rolls",
            DataEntryType = "Barcode-LongSeam",
        };

        var result = await controller.UpdateGroup(groupId, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminWorkCenterGroupDto>(ok.Value);
        Assert.Equal("Modified Rolls", updated.BaseName);
        Assert.Equal("Barcode-LongSeam", updated.DataEntryType);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateWorkCenterGroupDto
        {
            BaseName = "X",
            DataEntryType = "Rolls",
        };
        var result = await controller.UpdateGroup(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateConfig_ModifiesNumberOfWelders()
    {
        var controller = CreateController(out var db);
        var wc = db.WorkCenters.First(w => w.Name == "Rolls");

        var dto = new UpdateWorkCenterConfigDto { NumberOfWelders = 5, DataEntryType = "standard" };
        var result = await controller.UpdateConfig(wc.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminWorkCenterDto>(ok.Value);
        Assert.Equal(5, updated.NumberOfWelders);
        Assert.Equal("standard", updated.DataEntryType);
    }

    [Fact]
    public async Task UpdateConfig_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateWorkCenterConfigDto { NumberOfWelders = 1 };
        var result = await controller.UpdateConfig(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ---- WorkCenterProductionLine endpoint tests ----

    [Fact]
    public async Task GetProductionLineConfigs_ReturnsSeededRecords()
    {
        var controller = CreateController(out _);
        var result = await controller.GetProductionLineConfigs(TestHelpers.wcRollsId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminWorkCenterProductionLineDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 1);
        Assert.All(list, pl =>
        {
            Assert.Equal(TestHelpers.wcRollsId, pl.WorkCenterId);
            Assert.False(string.IsNullOrEmpty(pl.DisplayName));
        });
    }

    [Fact]
    public async Task GetProductionLineConfig_ReturnsSingleRecord()
    {
        var controller = CreateController(out _);
        var result = await controller.GetProductionLineConfig(
            TestHelpers.wcRollsId, TestHelpers.ProductionLine1Plt1Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<WorkCenterProductionLineDto>(ok.Value);
        Assert.Equal(TestHelpers.wcRollsId, dto.WorkCenterId);
        Assert.Equal(TestHelpers.ProductionLine1Plt1Id, dto.ProductionLineId);
        Assert.Equal("Rolls", dto.DisplayName);
    }

    [Fact]
    public async Task GetProductionLineConfig_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var result = await controller.GetProductionLineConfig(
            TestHelpers.wcRollsId, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateProductionLineConfig_CreatesNewRecord()
    {
        var controller = CreateController(out var db);

        var newPlId = Guid.NewGuid();
        db.ProductionLines.Add(new MESv2.Api.Models.ProductionLine
        {
            Id = newPlId,
            Name = "New Test Line",
            PlantId = TestHelpers.PlantPlt1Id
        });
        await db.SaveChangesAsync();

        var dto = new CreateWorkCenterProductionLineDto
        {
            ProductionLineId = newPlId,
            DisplayName = "Rolls - New Line",
            NumberOfWelders = 3
        };

        var result = await controller.CreateProductionLineConfig(
            TestHelpers.wcRollsId, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminWorkCenterProductionLineDto>(ok.Value);
        Assert.Equal("Rolls - New Line", created.DisplayName);
        Assert.Equal(3, created.NumberOfWelders);
        Assert.Equal("New Test Line", created.ProductionLineName);
    }

    [Fact]
    public async Task CreateProductionLineConfig_ReturnsConflict_WhenDuplicate()
    {
        var controller = CreateController(out _);
        var dto = new CreateWorkCenterProductionLineDto
        {
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            DisplayName = "Duplicate",
            NumberOfWelders = 1
        };

        var result = await controller.CreateProductionLineConfig(
            TestHelpers.wcRollsId, dto, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProductionLineConfig_ModifiesDisplayNameAndWelders()
    {
        var controller = CreateController(out _);
        var dto = new UpdateWorkCenterProductionLineDto
        {
            DisplayName = "Updated Rolls",
            NumberOfWelders = 5
        };

        var result = await controller.UpdateProductionLineConfig(
            TestHelpers.wcRollsId, TestHelpers.ProductionLine1Plt1Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminWorkCenterProductionLineDto>(ok.Value);
        Assert.Equal("Updated Rolls", updated.DisplayName);
        Assert.Equal(5, updated.NumberOfWelders);
    }

    [Fact]
    public async Task UpdateProductionLineConfig_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateWorkCenterProductionLineDto
        {
            DisplayName = "X",
            NumberOfWelders = 1
        };

        var result = await controller.UpdateProductionLineConfig(
            TestHelpers.wcRollsId, Guid.NewGuid(), dto, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteProductionLineConfig_RemovesRecord()
    {
        var controller = CreateController(out var db);
        var countBefore = await db.WorkCenterProductionLines
            .CountAsync(x => x.WorkCenterId == TestHelpers.wcRollsId);

        var result = await controller.DeleteProductionLineConfig(
            TestHelpers.wcRollsId, TestHelpers.ProductionLine1Plt1Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var countAfter = await db.WorkCenterProductionLines
            .CountAsync(x => x.WorkCenterId == TestHelpers.wcRollsId);
        Assert.Equal(countBefore - 1, countAfter);
    }

    [Fact]
    public async Task DeleteProductionLineConfig_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var result = await controller.DeleteProductionLineConfig(
            TestHelpers.wcRollsId, Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
