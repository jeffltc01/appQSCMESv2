using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class AdminAssetsControllerTests
{
    private AssetsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new AssetsController(db);
    }

    [Fact]
    public async Task GetAllAssets_ReturnsSeedAssets()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllAssets(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminAssetDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 1);
        Assert.All(list, a => Assert.False(string.IsNullOrEmpty(a.WorkCenterName)));
    }

    [Fact]
    public async Task CreateAsset_AddsToDatabase()
    {
        var controller = CreateController(out var db);
        var dto = new CreateAssetDto
        {
            Name = "New Asset",
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            LimbleIdentifier = "LMB-001"
        };

        var result = await controller.CreateAsset(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminAssetDto>(ok.Value);
        Assert.Equal("New Asset", created.Name);
        Assert.Equal("LMB-001", created.LimbleIdentifier);
        Assert.True(db.Assets.Any(a => a.Name == "New Asset"));
    }

    [Fact]
    public async Task UpdateAsset_ModifiesFields()
    {
        var controller = CreateController(out var db);
        var asset = new Asset { Id = Guid.NewGuid(), Name = "Old Asset", WorkCenterId = TestHelpers.WorkCenter1Plt1Id };
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var dto = new UpdateAssetDto { Name = "Updated Asset", WorkCenterId = TestHelpers.WorkCenter1Plt1Id, LimbleIdentifier = "LMB-002" };
        var result = await controller.UpdateAsset(asset.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminAssetDto>(ok.Value);
        Assert.Equal("Updated Asset", updated.Name);
        Assert.Equal("LMB-002", updated.LimbleIdentifier);
    }

    [Fact]
    public async Task UpdateAsset_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateAssetDto { Name = "X", WorkCenterId = TestHelpers.WorkCenter1Plt1Id };
        var result = await controller.UpdateAsset(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
