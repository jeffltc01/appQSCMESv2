using Microsoft.AspNetCore.Mvc;
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
