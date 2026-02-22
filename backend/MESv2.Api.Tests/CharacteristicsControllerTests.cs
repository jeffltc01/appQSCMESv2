using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;

namespace MESv2.Api.Tests;

public class CharacteristicsControllerTests
{
    private CharacteristicsController CreateController(out Data.MesDbContext db, decimal roleTier = 1m)
    {
        db = TestHelpers.CreateInMemoryContext();
        var controller = new CharacteristicsController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["X-User-Role-Tier"] = roleTier.ToString();
        return controller;
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
    public async Task GetAll_IncludesCodeAndMinTankSize()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminCharacteristicDto>>(ok.Value).ToList();
        var longSeam = list.First(c => c.Name == "Long Seam");
        Assert.Equal("001", longSeam.Code);

        var rs1 = list.First(c => c.Name == "RS1");
        Assert.Equal(0, rs1.MinTankSize);
        Assert.Equal("002", rs1.Code);
    }

    [Fact]
    public async Task Create_AddsCharacteristic()
    {
        var controller = CreateController(out var db);
        var dto = new CreateCharacteristicDto
        {
            Code = "999",
            Name = "Test Char",
            SpecHigh = 5.0m,
            WorkCenterIds = new List<Guid>()
        };

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminCharacteristicDto>(ok.Value);
        Assert.Equal("999", created.Code);
        Assert.Equal("Test Char", created.Name);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_RequiresAdmin()
    {
        var controller = CreateController(out _, roleTier: 3m);
        var dto = new CreateCharacteristicDto { Code = "999", Name = "X", WorkCenterIds = new() };
        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_ModifiesSpecValues()
    {
        var controller = CreateController(out var db);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");

        var dto = new UpdateCharacteristicDto
        {
            Code = char1.Code,
            Name = "Long Seam",
            SpecHigh = 10.5m,
            SpecLow = 1.0m,
            SpecTarget = 5.0m,
            IsActive = true,
            WorkCenterIds = new List<Guid>()
        };

        var result = await controller.Update(char1.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminCharacteristicDto>(ok.Value);
        Assert.Equal(10.5m, updated.SpecHigh);
        Assert.Equal(1.0m, updated.SpecLow);
    }

    [Fact]
    public async Task Update_IncludesMinTankSizeAndCode()
    {
        var controller = CreateController(out var db);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");

        var dto = new UpdateCharacteristicDto
        {
            Code = "010",
            Name = "Long Seam",
            MinTankSize = 500,
            IsActive = true,
            WorkCenterIds = new List<Guid>()
        };

        var result = await controller.Update(char1.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminCharacteristicDto>(ok.Value);
        Assert.Equal("010", updated.Code);
        Assert.Equal(500, updated.MinTankSize);
    }

    [Fact]
    public async Task Update_ReplacesWorkCenterAssignments()
    {
        var controller = CreateController(out var db);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");

        var dto = new UpdateCharacteristicDto
        {
            Code = char1.Code,
            Name = "Long Seam",
            IsActive = true,
            WorkCenterIds = new List<Guid>()
        };

        await controller.Update(char1.Id, dto, CancellationToken.None);

        Assert.Equal(0, db.CharacteristicWorkCenters.Count(cw => cw.CharacteristicId == char1.Id));
    }

    [Fact]
    public async Task Update_RequiresAdmin()
    {
        var controller = CreateController(out var db, roleTier: 3m);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");
        var dto = new UpdateCharacteristicDto { Code = "001", Name = "X", IsActive = true, WorkCenterIds = new() };
        var result = await controller.Update(char1.Id, dto, CancellationToken.None);
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateCharacteristicDto { Code = "X", Name = "X", IsActive = true, WorkCenterIds = new List<Guid>() };
        var result = await controller.Update(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_SoftDeletesCharacteristic()
    {
        var controller = CreateController(out var db);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");

        var result = await controller.Delete(char1.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deleted = Assert.IsType<AdminCharacteristicDto>(ok.Value);
        Assert.False(deleted.IsActive);
    }

    [Fact]
    public async Task Delete_RequiresAdmin()
    {
        var controller = CreateController(out var db, roleTier: 3m);
        var char1 = db.Characteristics.First(c => c.Name == "Long Seam");
        var result = await controller.Delete(char1.Id, CancellationToken.None);
        Assert.IsType<ForbidResult>(result.Result);
    }
}
