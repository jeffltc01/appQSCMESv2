using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;
using Moq;

namespace MESv2.Api.Tests;

public class DemoDataAdminControllerTests
{
    [Fact]
    public async Task ResetSeed_ReturnsForbid_WhenNotAdmin()
    {
        var service = new Mock<IDemoDataAdminService>();
        var controller = new DemoDataAdminController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.Request.Headers["X-User-Role-Tier"] = "3.0";
        var result = await controller.ResetSeed(CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task RefreshDates_ReturnsOk_WhenAdmin()
    {
        var service = new Mock<IDemoDataAdminService>();
        service.Setup(s => s.RefreshDatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DemoDataRefreshDatesResultDto
            {
                ExecutedAtUtc = DateTime.UtcNow,
                AppliedDeltaHours = 12.5,
                Updated = new List<DemoDataTableCountDto> { new() { Table = "ProductionRecords", Count = 5 } }
            });

        var controller = new DemoDataAdminController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.ControllerContext.HttpContext.Request.Headers["X-User-Role-Tier"] = "1.0";

        var result = await controller.RefreshDates(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DemoDataRefreshDatesResultDto>(ok.Value);
        Assert.Equal(12.5, dto.AppliedDeltaHours);
    }
}
