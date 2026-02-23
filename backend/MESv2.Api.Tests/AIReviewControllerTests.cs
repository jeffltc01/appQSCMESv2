using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AIReviewControllerTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestWcId = Guid.NewGuid();
    private static readonly Guid TestPlantId = Guid.NewGuid();

    private static AIReviewController CreateController(
        Mock<IAIReviewService> mockService,
        string? roleTier = null,
        Guid? userId = null)
    {
        var controller = new AIReviewController(mockService.Object);
        var httpContext = new DefaultHttpContext();

        if (roleTier != null)
            httpContext.Request.Headers["X-User-Role-Tier"] = roleTier;

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
    public async Task GetRecords_NoTierHeader_ReturnsForbid()
    {
        var mock = new Mock<IAIReviewService>();
        var controller = CreateController(mock, roleTier: null);

        var result = await controller.GetRecords(TestWcId, TestPlantId, "2026-01-01", CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        mock.Verify(s => s.GetRecordsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetRecords_InvalidTierHeader_ReturnsForbid()
    {
        var mock = new Mock<IAIReviewService>();
        var controller = CreateController(mock, roleTier: "not-a-number");

        var result = await controller.GetRecords(TestWcId, TestPlantId, "2026-01-01", CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("10")]
    public async Task GetRecords_UnauthorizedTier_ReturnsForbid(string tier)
    {
        var mock = new Mock<IAIReviewService>();
        var controller = CreateController(mock, roleTier: tier);

        var result = await controller.GetRecords(TestWcId, TestPlantId, "2026-01-01", CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("5.5")]
    public async Task GetRecords_AuthorizedTier_ReturnsOk(string tier)
    {
        var expected = new List<AIReviewRecordDto> { new() { Id = Guid.NewGuid() } };
        var mock = new Mock<IAIReviewService>();
        mock.Setup(s => s.GetRecordsAsync(TestWcId, TestPlantId, "2026-01-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(mock, roleTier: tier);

        var result = await controller.GetRecords(TestWcId, TestPlantId, "2026-01-01", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task SubmitReview_NoTierHeader_ReturnsForbid()
    {
        var mock = new Mock<IAIReviewService>();
        var controller = CreateController(mock, roleTier: null, userId: TestUserId);
        var request = new CreateAIReviewRequest { ProductionRecordIds = new List<Guid> { Guid.NewGuid() } };

        var result = await controller.SubmitReview(request, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task SubmitReview_AuthorizedButNoUserClaim_ReturnsUnauthorized()
    {
        var mock = new Mock<IAIReviewService>();
        var controller = CreateController(mock, roleTier: "1", userId: null);
        var request = new CreateAIReviewRequest { ProductionRecordIds = new List<Guid> { Guid.NewGuid() } };

        var result = await controller.SubmitReview(request, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task SubmitReview_AuthorizedWithUser_ReturnsOk()
    {
        var expected = new AIReviewResultDto { AnnotationsCreated = 3 };
        var mock = new Mock<IAIReviewService>();
        mock.Setup(s => s.SubmitReviewAsync(TestUserId, It.IsAny<CreateAIReviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(mock, roleTier: "2", userId: TestUserId);
        var request = new CreateAIReviewRequest { ProductionRecordIds = new List<Guid> { Guid.NewGuid() } };

        var result = await controller.SubmitReview(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task SubmitReview_InspectorTier_ReturnsOk()
    {
        var expected = new AIReviewResultDto { AnnotationsCreated = 1 };
        var mock = new Mock<IAIReviewService>();
        mock.Setup(s => s.SubmitReviewAsync(TestUserId, It.IsAny<CreateAIReviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(mock, roleTier: "5.5", userId: TestUserId);
        var request = new CreateAIReviewRequest { ProductionRecordIds = new List<Guid> { Guid.NewGuid() } };

        var result = await controller.SubmitReview(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }
}
