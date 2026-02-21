using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class ActiveSessionsControllerTests
{
    private ActiveSessionsController CreateController(out Data.MesDbContext db, Guid? userId = null)
    {
        db = TestHelpers.CreateInMemoryContext();
        var controller = new ActiveSessionsController(db);

        if (userId.HasValue)
        {
            var claims = new[] { new Claim("sub", userId.Value.ToString()) };
            var identity = new ClaimsIdentity(claims, "Test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        return controller;
    }

    [Fact]
    public async Task GetBySite_ReturnsEmptyWhenNoSessions()
    {
        var controller = CreateController(out _);
        var result = await controller.GetBySite("PLT1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ActiveSessionDto>>(ok.Value).ToList();
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetBySite_ReturnsSessionsForSite()
    {
        var userId = TestHelpers.TestUserId;
        var controller = CreateController(out var db, userId);

        db.ActiveSessions.Add(new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            LoginDateTime = DateTime.UtcNow,
            LastHeartbeatDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await controller.GetBySite("PLT1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ActiveSessionDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.Equal("EMP001", list[0].EmployeeNumber);
        Assert.False(list[0].IsStale);
    }

    [Fact]
    public async Task GetBySite_MarksStaleSession()
    {
        var userId = TestHelpers.TestUserId;
        var controller = CreateController(out var db, userId);

        db.ActiveSessions.Add(new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            LoginDateTime = DateTime.UtcNow.AddMinutes(-30),
            LastHeartbeatDateTime = DateTime.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        var result = await controller.GetBySite("PLT1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ActiveSessionDto>>(ok.Value).ToList();
        Assert.Single(list);
        Assert.True(list[0].IsStale);
    }

    [Fact]
    public async Task Upsert_CreatesNewSession()
    {
        var userId = TestHelpers.TestUserId;
        var controller = CreateController(out var db, userId);

        var dto = new CreateActiveSessionDto
        {
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId
        };

        var result = await controller.Upsert(dto, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.True(db.ActiveSessions.Any(s => s.UserId == userId));
    }

    [Fact]
    public async Task Upsert_UpdatesExistingSession()
    {
        var userId = TestHelpers.TestUserId;
        var controller = CreateController(out var db, userId);

        db.ActiveSessions.Add(new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            LoginDateTime = DateTime.UtcNow.AddHours(-1),
            LastHeartbeatDateTime = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var dto = new CreateActiveSessionDto
        {
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId
        };

        await controller.Upsert(dto, CancellationToken.None);

        Assert.Equal(1, db.ActiveSessions.Count(s => s.UserId == userId));
        var session = db.ActiveSessions.First(s => s.UserId == userId);
        Assert.True(session.LoginDateTime > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Upsert_ReturnsUnauthorized_WhenNoToken()
    {
        var controller = CreateController(out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var dto = new CreateActiveSessionDto { SiteCode = "PLT1", ProductionLineId = Guid.NewGuid(), WorkCenterId = Guid.NewGuid() };
        var result = await controller.Upsert(dto, CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Heartbeat_UpdatesTimestamp()
    {
        var userId = TestHelpers.TestUserId;
        var controller = CreateController(out var db, userId);
        var oldTime = DateTime.UtcNow.AddMinutes(-3);

        db.ActiveSessions.Add(new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            LoginDateTime = oldTime,
            LastHeartbeatDateTime = oldTime
        });
        await db.SaveChangesAsync();

        var result = await controller.Heartbeat(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var session = db.ActiveSessions.First(s => s.UserId == userId);
        Assert.True(session.LastHeartbeatDateTime > oldTime);
    }

    [Fact]
    public async Task Heartbeat_ReturnsNotFound_WhenNoSession()
    {
        var controller = CreateController(out _, TestHelpers.TestUserId);
        var result = await controller.Heartbeat(CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EndSession_RemovesSession()
    {
        var userId = TestHelpers.TestUserId;
        var controller = CreateController(out var db, userId);

        db.ActiveSessions.Add(new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            LoginDateTime = DateTime.UtcNow,
            LastHeartbeatDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await controller.EndSession(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.ActiveSessions.Any(s => s.UserId == userId));
    }

    [Fact]
    public async Task EndSession_ReturnsNoContent_WhenNoSession()
    {
        var controller = CreateController(out _, TestHelpers.TestUserId);
        var result = await controller.EndSession(CancellationToken.None);
        Assert.IsType<NoContentResult>(result);
    }
}
