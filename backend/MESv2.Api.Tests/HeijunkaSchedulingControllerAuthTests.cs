using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class HeijunkaSchedulingControllerAuthTests
{
    [Fact]
    public async Task GetExceptions_Forbids_WhenSiteHeaderDoesNotMatch()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var service = new HeijunkaSchedulingService(db);
        var controller = new HeijunkaSchedulingController(service)
        {
            ControllerContext = BuildContext(roleTier: 5m, siteCode: "700", userId: TestHelpers.TestUserId)
        };

        var result = await controller.GetExceptions("000", CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetExceptions_ReturnsOk_WhenSiteHeaderMatches()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var service = new HeijunkaSchedulingService(db);
        var controller = new HeijunkaSchedulingController(service)
        {
            ControllerContext = BuildContext(roleTier: 5m, siteCode: "000", userId: TestHelpers.TestUserId)
        };

        var result = await controller.GetExceptions("000", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    private static ControllerContext BuildContext(decimal roleTier, string siteCode, Guid userId)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-User-Role-Tier"] = roleTier.ToString();
        http.Request.Headers["X-User-Site-Code"] = siteCode;
        http.Request.Headers["X-User-Id"] = userId.ToString();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth");
        http.User = new ClaimsPrincipal(identity);
        return new ControllerContext { HttpContext = http };
    }
}
