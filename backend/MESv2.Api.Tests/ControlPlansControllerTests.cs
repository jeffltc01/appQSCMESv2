using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class ControlPlansControllerTests
{
    private ControlPlansController CreateController(out Data.MesDbContext db, decimal roleTier = 1m)
    {
        db = TestHelpers.CreateInMemoryContext();
        var controller = new ControlPlansController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["X-User-Role-Tier"] = roleTier.ToString();
        return controller;
    }

    private ControlPlan SeedControlPlan(Data.MesDbContext db)
    {
        var charId = db.Characteristics.First().Id;
        var cp = new ControlPlan
        {
            Id = Guid.NewGuid(),
            CharacteristicId = charId,
            WorkCenterProductionLineId = TestHelpers.wcplRollsId,
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
    public async Task GetAll_IncludesProductionLineName()
    {
        var controller = CreateController(out var db);
        SeedControlPlan(db);

        var result = await controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminControlPlanDto>>(ok.Value).ToList();
        var first = list.First();
        Assert.False(string.IsNullOrEmpty(first.ProductionLineName));
        Assert.False(string.IsNullOrEmpty(first.WorkCenterName));
    }

    [Fact]
    public async Task Create_AddsControlPlan()
    {
        var controller = CreateController(out var db);
        var charId = db.Characteristics.First().Id;
        var dto = new CreateControlPlanDto
        {
            CharacteristicId = charId,
            WorkCenterProductionLineId = TestHelpers.wcplHydroId,
            IsEnabled = true,
            ResultType = "PassFail",
            IsGateCheck = true,
            CodeRequired = true
        };

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminControlPlanDto>(ok.Value);
        Assert.True(created.IsGateCheck);
        Assert.True(created.CodeRequired);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_RequiresAdmin()
    {
        var controller = CreateController(out var db, roleTier: 3m);
        var dto = new CreateControlPlanDto
        {
            CharacteristicId = db.Characteristics.First().Id,
            WorkCenterProductionLineId = TestHelpers.wcplRollsId,
            ResultType = "PassFail"
        };
        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Create_CodeRequired_ForcesGateCheck()
    {
        var controller = CreateController(out var db);
        var dto = new CreateControlPlanDto
        {
            CharacteristicId = db.Characteristics.First().Id,
            WorkCenterProductionLineId = TestHelpers.wcplRollsId,
            IsEnabled = true,
            ResultType = "PassFail",
            IsGateCheck = false,
            CodeRequired = true
        };

        var result = await controller.Create(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_TogglesEnabledAndGateCheck()
    {
        var controller = CreateController(out var db);
        var cp = SeedControlPlan(db);

        var dto = new UpdateControlPlanDto { IsEnabled = false, ResultType = "PassFail", IsGateCheck = true, IsActive = true };
        var result = await controller.Update(cp.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminControlPlanDto>(ok.Value);
        Assert.False(updated.IsEnabled);
        Assert.True(updated.IsGateCheck);
    }

    [Theory]
    [InlineData("PassFail")]
    [InlineData("AcceptReject")]
    [InlineData("GoNoGo")]
    [InlineData("NumericInt")]
    [InlineData("NumericDecimal")]
    [InlineData("Text")]
    public async Task Update_WithNewResultTypes(string resultType)
    {
        var controller = CreateController(out var db);
        var cp = SeedControlPlan(db);

        var dto = new UpdateControlPlanDto { IsEnabled = true, ResultType = resultType, IsActive = true };
        var result = await controller.Update(cp.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminControlPlanDto>(ok.Value);
        Assert.Equal(resultType, updated.ResultType);
    }

    [Fact]
    public async Task Update_InvalidResultType_Returns400()
    {
        var controller = CreateController(out var db);
        var cp = SeedControlPlan(db);

        var dto = new UpdateControlPlanDto { IsEnabled = true, ResultType = "Invalid", IsActive = true };
        var result = await controller.Update(cp.Id, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateControlPlanDto { IsEnabled = true, ResultType = "PassFail", IsActive = true };
        var result = await controller.Update(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_SoftDeletesControlPlan()
    {
        var controller = CreateController(out var db);
        var cp = SeedControlPlan(db);

        var result = await controller.Delete(cp.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var deleted = Assert.IsType<AdminControlPlanDto>(ok.Value);
        Assert.False(deleted.IsActive);
    }

    [Fact]
    public async Task Delete_RequiresAdmin()
    {
        var controller = CreateController(out var db, roleTier: 3m);
        var cp = SeedControlPlan(db);
        var result = await controller.Delete(cp.Id, CancellationToken.None);
        Assert.IsType<ForbidResult>(result.Result);
    }
}
