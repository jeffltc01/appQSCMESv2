using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class NlqControllerTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestSiteId = Guid.NewGuid();

    private static NlqController CreateController(
        Mock<INaturalLanguageQueryService> mockService,
        string? roleTier = "5",
        Guid? userId = null,
        Guid? siteId = null)
    {
        var controller = new NlqController(mockService.Object);
        var httpContext = new DefaultHttpContext();

        if (roleTier != null)
            httpContext.Request.Headers["X-User-Role-Tier"] = roleTier;
        if (siteId.HasValue)
            httpContext.Request.Headers["X-User-Site-Id"] = siteId.Value.ToString();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) },
                "TestAuth"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task Ask_MissingUserClaim_ReturnsUnauthorized()
    {
        var mock = new Mock<INaturalLanguageQueryService>();
        var controller = CreateController(mock, userId: null, siteId: TestSiteId);

        var result = await controller.Ask(new NaturalLanguageQueryRequestDto { Question = "how many tanks today?" }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Ask_MissingSiteHeader_ReturnsForbid()
    {
        var mock = new Mock<INaturalLanguageQueryService>();
        var controller = CreateController(mock, userId: TestUserId, siteId: null);

        var result = await controller.Ask(new NaturalLanguageQueryRequestDto { Question = "how many tanks today?" }, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Ask_ValidRequest_ReturnsOk()
    {
        var mock = new Mock<INaturalLanguageQueryService>();
        var expected = new NaturalLanguageQueryResponseDto
        {
            AnswerText = "42",
            ScopeUsed = "plant-wide",
        };
        mock.Setup(s => s.AskAsync(
                TestUserId,
                5m,
                TestSiteId,
                It.IsAny<NaturalLanguageQueryRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController(mock, userId: TestUserId, siteId: TestSiteId);
        var result = await controller.Ask(new NaturalLanguageQueryRequestDto { Question = "how many tanks today?" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }
}
