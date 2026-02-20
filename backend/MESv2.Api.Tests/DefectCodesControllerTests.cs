using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class DefectCodesControllerTests
{
    private DefectCodesController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new DefectCodesController(db);
    }

    [Fact]
    public async Task GetAll_ReturnsSeedDefectCodes()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminDefectCodeDto>>(ok.Value).ToList();
        Assert.Contains(list, d => d.Code == "101" && d.Name == "Burn Through");
    }

    [Fact]
    public async Task GetAll_IncludesWorkCenterIds()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminDefectCodeDto>>(ok.Value).ToList();
        var code101 = list.First(d => d.Code == "101");
        Assert.NotEmpty(code101.WorkCenterIds);
    }

    [Fact]
    public async Task Create_AddsCodeWithWorkCenters()
    {
        var controller = CreateController(out var db);
        var wcId = TestHelpers.WorkCenter1Plt1Id;

        var dto = new CreateDefectCodeDto
        {
            Code = "200",
            Name = "New Defect",
            Severity = "High",
            WorkCenterIds = new List<Guid> { wcId }
        };

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminDefectCodeDto>(ok.Value);
        Assert.Equal("200", created.Code);
        Assert.Single(created.WorkCenterIds);
        Assert.True(db.DefectCodes.Any(d => d.Code == "200"));
        Assert.True(db.DefectWorkCenters.Any(dw => dw.DefectCodeId == created.Id));
    }

    [Fact]
    public async Task Update_ModifiesCodeAndReplacesJunctions()
    {
        var controller = CreateController(out var db);
        var code = new DefectCode { Id = Guid.NewGuid(), Code = "300", Name = "Old Name" };
        db.DefectCodes.Add(code);
        db.DefectWorkCenters.Add(new DefectWorkCenter { Id = Guid.NewGuid(), DefectCodeId = code.Id, WorkCenterId = TestHelpers.WorkCenter1Plt1Id, EarliestDetectionWorkCenterId = TestHelpers.WorkCenter1Plt1Id });
        await db.SaveChangesAsync();

        var dto = new UpdateDefectCodeDto
        {
            Code = "300",
            Name = "Updated Name",
            WorkCenterIds = new List<Guid>()
        };

        var result = await controller.Update(code.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminDefectCodeDto>(ok.Value);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Empty(updated.WorkCenterIds);
    }

    [Fact]
    public async Task Delete_RemovesCodeAndJunctions()
    {
        var controller = CreateController(out var db);
        var code = new DefectCode { Id = Guid.NewGuid(), Code = "400", Name = "To Delete" };
        db.DefectCodes.Add(code);
        db.DefectWorkCenters.Add(new DefectWorkCenter { Id = Guid.NewGuid(), DefectCodeId = code.Id, WorkCenterId = TestHelpers.WorkCenter1Plt1Id, EarliestDetectionWorkCenterId = TestHelpers.WorkCenter1Plt1Id });
        await db.SaveChangesAsync();

        var result = await controller.Delete(code.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.DefectCodes.Any(d => d.Id == code.Id));
        Assert.False(db.DefectWorkCenters.Any(dw => dw.DefectCodeId == code.Id));
    }
}
