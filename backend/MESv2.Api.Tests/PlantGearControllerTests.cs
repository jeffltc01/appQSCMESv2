using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class PlantGearControllerTests
{
    private static readonly Guid GearLevel2Plt1Id = Guid.Parse("61111111-1111-1111-1111-111111111112");

    private PlantGearController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new PlantGearController(db);
    }

    [Fact]
    public async Task GetAll_ReturnsPlants()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<PlantWithGearDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 3);
    }

    [Fact]
    public async Task SetGear_UpdatesCurrentGear()
    {
        var controller = CreateController(out var db);
        var plant = db.Plants.First(p => p.Id == TestHelpers.PlantPlt1Id);

        var dto = new SetPlantGearDto { PlantGearId = GearLevel2Plt1Id };
        var result = await controller.SetGear(plant.Id, dto, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        await db.Entry(plant).ReloadAsync();
        Assert.Equal(GearLevel2Plt1Id, plant.CurrentPlantGearId);
    }

    [Fact]
    public async Task SetGear_ReturnsNotFound_WhenPlantMissing()
    {
        var controller = CreateController(out _);
        var dto = new SetPlantGearDto { PlantGearId = Guid.NewGuid() };
        var result = await controller.SetGear(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SetGear_ReturnsBadRequest_WhenGearDoesNotBelongToPlant()
    {
        var controller = CreateController(out var db);
        var otherPlantId = Guid.NewGuid();
        db.Plants.Add(new Plant { Id = otherPlantId, Code = "OTH", Name = "Other" });
        var wrongGear = new PlantGear { Id = Guid.NewGuid(), Name = "Wrong", Level = 1, PlantId = otherPlantId };
        db.PlantGears.Add(wrongGear);
        await db.SaveChangesAsync();

        var dto = new SetPlantGearDto { PlantGearId = wrongGear.Id };
        var result = await controller.SetGear(TestHelpers.PlantPlt1Id, dto, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
