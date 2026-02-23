using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MESv2.Api.Controllers;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class FrontendTelemetryControllerTests
{
    private static FrontendTelemetryController CreateController(
        IFrontendTelemetryService service,
        MesDbContext db,
        Guid? userId = null)
    {
        var controller = new FrontendTelemetryController(service, db, NullLogger<FrontendTelemetryController>.Instance);
        var http = new DefaultHttpContext();
        if (userId.HasValue)
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            ], "test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    [Fact]
    public async Task GetEvents_ForbidsWhenRoleTierAboveThree()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            EmployeeNumber = "EMP200",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            RoleTier = 5m,
            RoleName = "Team Lead",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
            IsCertifiedWelder = false,
            RequirePinForLogin = false,
            UserType = 0,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new Mock<IFrontendTelemetryService>();
        var controller = CreateController(service.Object, db, userId);

        var result = await controller.GetEvents(
            category: null,
            source: null,
            severity: null,
            userId: null,
            workCenterId: null,
            from: null,
            to: null,
            reactRuntimeOnly: false,
            page: 1,
            pageSize: 50,
            ct: CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Ingest_ReturnsAccepted_WhenServiceThrows()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var service = new Mock<IFrontendTelemetryService>();
        service.Setup(s => s.IngestAsync(It.IsAny<FrontendTelemetryIngestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));
        var controller = CreateController(service.Object, db, null);

        var result = await controller.Ingest(new FrontendTelemetryIngestDto
        {
            Category = "runtime_error",
            Source = "window_error",
            Severity = "error",
            Message = "Oops"
        }, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
    }
}
