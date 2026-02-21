using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class ControlPlansControllerTests
{
    private ControlPlansController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        return new ControlPlansController(db);
    }

    private ControlPlan SeedControlPlan(Data.MesDbContext db)
    {
        var charId = db.Characteristics.First().Id;
        var wcId = TestHelpers.wcRollsId;
        var cp = new ControlPlan
        {
            Id = Guid.NewGuid(),
            CharacteristicId = charId,
            WorkCenterId = wcId,
            IsEnabled = true,
            ResultType = "PassFail",
            IsGateCheck = false
        };
        db.ControlPlans.Add(cp);
        db.SaveChanges();
        return cp;
    }

    [Fact]
    public async Task GetAll_ReturnsControlPlans()
    {
        var controller = CreateController(out var db);
        SeedControlPlan(db);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminControlPlanDto>>(ok.Value).ToList();
        Assert.NotEmpty(list);
        Assert.All(list, cp => Assert.False(string.IsNullOrEmpty(cp.CharacteristicName)));
    }

    [Fact]
    public async Task Update_TogglesEnabledAndGateCheck()
    {
        var controller = CreateController(out var db);
        var cp = SeedControlPlan(db);

        var dto = new UpdateControlPlanDto { IsEnabled = false, ResultType = "PassFail", IsGateCheck = true };
        var result = await controller.Update(cp.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminControlPlanDto>(ok.Value);
        Assert.False(updated.IsEnabled);
        Assert.True(updated.IsGateCheck);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateControlPlanDto { IsEnabled = true, ResultType = "PassFail" };
        var result = await controller.Update(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }
}
