using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;

namespace MESv2.Api.Tests;

public class CharacteristicsControllerTests
{
    private CharacteristicsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new CharacteristicsController(db);
    }

    [Fact]
    public async Task GetAll_ReturnsSeedCharacteristics()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminCharacteristicDto>>(ok.Value).ToList();
        Assert.Contains(list, c => c.Name == "Long Seam");
        Assert.Contains(list, c => c.Name == "RS1");
    }

    [Fact]
    public async Task GetAll_IncludesWorkCenterIds()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminCharacteristicDto>>(ok.Value).ToList();
        var longSeam = list.First(c => c.Name == "Long Seam");
        Assert.NotEmpty(longSeam.WorkCenterIds);
    }

    [Fact]
    public async Task Update_ModifiesSpecValues()
    {
        var controller = CreateController(out var db);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");

        var dto = new UpdateCharacteristicDto
        {
            Name = "Long Seam",
            SpecHigh = 10.5m,
            SpecLow = 1.0m,
            SpecTarget = 5.0m,
            WorkCenterIds = new List<Guid>()
        };

        var result = await controller.Update(char1.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminCharacteristicDto>(ok.Value);
        Assert.Equal(10.5m, updated.SpecHigh);
        Assert.Equal(1.0m, updated.SpecLow);
    }

    [Fact]
    public async Task Update_ReplacesWorkCenterAssignments()
    {
        var controller = CreateController(out var db);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");
        var initialCount = db.CharacteristicWorkCenters.Count(cw => cw.CharacteristicId == char1.Id);

        var dto = new UpdateCharacteristicDto
        {
            Name = "Long Seam",
            WorkCenterIds = new List<Guid>()
        };

        await controller.Update(char1.Id, dto, CancellationToken.None);

        Assert.Equal(0, db.CharacteristicWorkCenters.Count(cw => cw.CharacteristicId == char1.Id));
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateCharacteristicDto { Name = "X", WorkCenterIds = new List<Guid>() };
        var result = await controller.Update(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
