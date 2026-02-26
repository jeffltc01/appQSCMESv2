using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class DefectAnalyticsControllerTests
{
    private static DefectAnalyticsController CreateController(
        Mock<IDefectAnalyticsService> mockService,
        string? roleTier = "5")
    {
        var controller = new DefectAnalyticsController(mockService.Object);
        var httpContext = new DefaultHttpContext();
        if (roleTier != null)
            httpContext.Request.Headers["X-User-Role-Tier"] = roleTier;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task GetPareto_WhenCallerNotTeamLeadOrAbove_ReturnsForbid()
    {
        var mock = new Mock<IDefectAnalyticsService>();
        var controller = CreateController(mock, roleTier: "6");

        var result = await controller.GetPareto(
            TestHelpers.wcRollsId,
            TestHelpers.PlantPlt1Id,
            "2026-06-10",
            "day",
            null,
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetPareto_WhenAuthorized_ReturnsOkFromService()
    {
        var expected = new DefectParetoResponseDto
        {
            TotalDefects = 2,
            Items = new List<DefectParetoItemDto>
            {
                new() { DefectCode = "D001", DefectName = "Undercut", Count = 2, CumulativePercent = 100m },
            },
        };

        var mock = new Mock<IDefectAnalyticsService>();
        mock.Setup(s => s.GetDefectParetoAsync(
                TestHelpers.wcRollsId,
                TestHelpers.PlantPlt1Id,
                "2026-06-10",
                "day",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController(mock, roleTier: "5");
        var result = await controller.GetPareto(
            TestHelpers.wcRollsId,
            TestHelpers.PlantPlt1Id,
            "2026-06-10",
            "day",
            null,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }
}
