using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        return new WorkCentersController(mockService.Object, db);
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
                Assert.False(string.IsNullOrEmpty(sc.PlantName));
            });
        }
    }

    [Fact]
    public async Task UpdateGroup_ModifiesDataEntryTypeAndSiteNames()
    {
        var controller = CreateController(out var db);
        var firstWc = await db.WorkCenters.FirstAsync(w => w.Name == "Rolls 1");
        var groupId = firstWc.WorkCenterGroupId;

        var groupWcs = await db.WorkCenters.Where(w => w.WorkCenterGroupId == groupId).ToListAsync();
        var siteConfigs = groupWcs.Select(w => new UpdateSiteConfigDto
        {
            WorkCenterId = w.Id,
            SiteName = "Modified " + w.Name,
            NumberOfWelders = 10,
        }).ToList();

        var dto = new UpdateWorkCenterGroupDto
        {
            BaseName = "Modified Rolls 1",
            DataEntryType = "Barcode",
            SiteConfigs = siteConfigs
        };

        var result = await controller.UpdateGroup(groupId, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminWorkCenterGroupDto>(ok.Value);
        Assert.Equal("Barcode", updated.DataEntryType);
        Assert.All(updated.SiteConfigs, sc => Assert.Equal(10, sc.NumberOfWelders));
    }

    [Fact]
    public async Task UpdateGroup_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateWorkCenterGroupDto
        {
            BaseName = "X",
            DataEntryType = "standard",
            SiteConfigs = new List<UpdateSiteConfigDto>()
        };
        var result = await controller.UpdateGroup(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateConfig_ModifiesNumberOfWelders()
    {
        var controller = CreateController(out var db);
        var wc = db.WorkCenters.First(w => w.Name == "Rolls 1");

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
}
