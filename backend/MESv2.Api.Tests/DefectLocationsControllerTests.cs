using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class DefectLocationsControllerTests
{
    private DefectLocationsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new DefectLocationsController(db);
    }

    [Fact]
    public async Task GetAll_ReturnsSeedLocations()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminDefectLocationDto>>(ok.Value).ToList();
        Assert.Contains(list, d => d.Name == "T-Joint");
    }

    [Fact]
    public async Task Create_AddsLocation()
    {
        var controller = CreateController(out var db);
        var charId = db.Characteristics.First().Id;

        var dto = new CreateDefectLocationDto { Code = "99", Name = "New Loc", CharacteristicId = charId };
        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminDefectLocationDto>(ok.Value);
        Assert.Equal("New Loc", created.Name);
        Assert.NotNull(created.CharacteristicName);
        Assert.True(db.DefectLocations.Any(d => d.Code == "99"));
    }

    [Fact]
    public async Task Update_ModifiesLocation()
    {
        var controller = CreateController(out var db);
        var loc = db.DefectLocations.First();

        var dto = new UpdateDefectLocationDto { Code = loc.Code, Name = "Updated Name", CharacteristicId = loc.CharacteristicId };
        var result = await controller.Update(loc.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminDefectLocationDto>(ok.Value);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateDefectLocationDto { Code = "X", Name = "X" };
        var result = await controller.Update(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_CanDeactivate()
    {
        var controller = CreateController(out var db);
        var loc = db.DefectLocations.First();

        var dto = new UpdateDefectLocationDto
        {
            Code = loc.Code,
            Name = loc.Name,
            CharacteristicId = loc.CharacteristicId,
            IsActive = false
        };

        var result = await controller.Update(loc.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminDefectLocationDto>(ok.Value);
        Assert.False(updated.IsActive);
        var dbLoc = db.DefectLocations.Single(d => d.Id == loc.Id);
        Assert.False(dbLoc.IsActive);
    }

    [Fact]
    public async Task GetAll_IncludesIsActiveField()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminDefectLocationDto>>(ok.Value).ToList();
        Assert.All(list, d => Assert.True(d.IsActive));
    }

    [Fact]
    public async Task Delete_SoftDeletesSetsInactive()
    {
        var controller = CreateController(out var db);
        var loc = new DefectLocation { Id = Guid.NewGuid(), Code = "DEL", Name = "To Delete" };
        db.DefectLocations.Add(loc);
        await db.SaveChangesAsync();

        var result = await controller.Delete(loc.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminDefectLocationDto>(ok.Value);
        Assert.False(dto.IsActive);
        Assert.True(db.DefectLocations.Any(d => d.Id == loc.Id));
        var dbLoc = db.DefectLocations.Single(d => d.Id == loc.Id);
        Assert.False(dbLoc.IsActive);
    }
}
