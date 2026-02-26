using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;
using Moq;

namespace MESv2.Api.Tests;

public class ChecklistsControllerTests
{
    [Fact]
    public async Task GetReviewSummary_ReturnsForbid_WhenSiteHeaderMismatches()
    {
        var service = new Mock<IChecklistService>();
        var controller = CreateController(service.Object, TestHelpers.PlantPlt2Id);

        var result = await controller.GetReviewSummary(
            TestHelpers.PlantPlt1Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            null,
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetReviewSummary_AllowsCrossSite_ForDirectorRole()
    {
        var service = new Mock<IChecklistService>();
        service.Setup(s => s.GetReviewSummaryAsync(
                TestHelpers.PlantPlt2Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChecklistReviewSummaryDto
            {
                SiteId = TestHelpers.PlantPlt2Id,
                FromUtc = DateTime.UtcNow.AddDays(-1),
                ToUtc = DateTime.UtcNow,
            });

        var controller = CreateController(service.Object, TestHelpers.PlantPlt1Id, 2m);
        var result = await controller.GetReviewSummary(
            TestHelpers.PlantPlt2Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            null,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetReviewSummary_ReturnsOk_WhenServiceSucceeds()
    {
        var service = new Mock<IChecklistService>();
        var dto = new ChecklistReviewSummaryDto
        {
            SiteId = TestHelpers.PlantPlt1Id,
            FromUtc = DateTime.UtcNow.AddDays(-1),
            ToUtc = DateTime.UtcNow,
            TotalEntries = 1,
            TotalResponses = 1,
            ChecklistTypesFound = ["SafetyPreShift"],
            Questions =
            [
                new ChecklistQuestionSummaryDto
                {
                    ChecklistTemplateItemId = Guid.NewGuid(),
                    Prompt = "Guard check",
                    ResponseType = "Checkbox",
                    ResponseCount = 1,
                    ResponseBuckets = [new ChecklistResponseBucketDto { Value = "true", Label = "Pass", Count = 1 }]
                }
            ]
        };
        service.Setup(s => s.GetReviewSummaryAsync(
                TestHelpers.PlantPlt1Id,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateController(service.Object, TestHelpers.PlantPlt1Id);
        var result = await controller.GetReviewSummary(
            TestHelpers.PlantPlt1Id,
            dto.FromUtc,
            dto.ToUtc,
            null,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsType<ChecklistReviewSummaryDto>(ok.Value);
        Assert.Equal(1, value.TotalEntries);
    }

    [Fact]
    public async Task GetQuestionResponses_ReturnsNotFound_WhenQuestionMissing()
    {
        var service = new Mock<IChecklistService>();
        service.Setup(s => s.GetQuestionResponsesAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Checklist question not found."));

        var controller = CreateController(service.Object, TestHelpers.PlantPlt1Id);
        var result = await controller.GetQuestionResponses(
            TestHelpers.PlantPlt1Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            Guid.NewGuid(),
            null,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFound.Value);
    }

    private static ChecklistsController CreateController(IChecklistService checklistService, Guid callerSiteId, decimal roleTier = 5m)
    {
        var controller = new ChecklistsController(checklistService);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Site-Id"] = callerSiteId.ToString();
        context.Request.Headers["X-User-Role-Tier"] = roleTier.ToString(System.Globalization.CultureInfo.InvariantCulture);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        return controller;
    }
}
