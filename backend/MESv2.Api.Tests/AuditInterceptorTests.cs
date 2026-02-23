using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using MESv2.Api.Data;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

public class AuditInterceptorTests
{
    private static (MesDbContext db, AuditInterceptor interceptor) CreateContextWithAudit(Guid? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        var interceptor = new AuditInterceptor(accessor.Object);

        var dbName = "AuditTest_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        var db = new MesDbContext(options);
        db.Database.EnsureCreated();
        DbInitializer.Seed(db);

        return (db, interceptor);
    }

    [Fact]
    public async Task CreatedEntity_IsAudited()
    {
        var (db, _) = CreateContextWithAudit(TestHelpers.TestUserId);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Test Vendor",
            VendorType = "Mill",
            IsActive = true
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var log = db.AuditLogs.FirstOrDefault(a => a.EntityId == vendor.Id);
        Assert.NotNull(log);
        Assert.Equal("Created", log.Action);
        Assert.Equal("Vendor", log.EntityName);
        Assert.Equal(TestHelpers.TestUserId, log.ChangedByUserId);
        Assert.NotNull(log.Changes);

        var changes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.Changes!);
        Assert.NotNull(changes);
        Assert.True(changes!.ContainsKey("Name"));
    }

    [Fact]
    public async Task UpdatedEntity_CapturesOldAndNewValues()
    {
        var (db, _) = CreateContextWithAudit(TestHelpers.TestUserId);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            VendorType = "Mill",
            IsActive = true
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        vendor.Name = "Updated Name";
        await db.SaveChangesAsync();

        var logs = db.AuditLogs
            .Where(a => a.EntityId == vendor.Id)
            .OrderBy(a => a.Id)
            .ToList();

        Assert.Equal(2, logs.Count);

        var updateLog = logs[1];
        Assert.Equal("Updated", updateLog.Action);

        var changes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(updateLog.Changes!);
        Assert.NotNull(changes);
        Assert.True(changes!.ContainsKey("Name"));

        var nameChange = changes["Name"];
        Assert.Equal("Original Name", nameChange.GetProperty("old").GetString());
        Assert.Equal("Updated Name", nameChange.GetProperty("new").GetString());
    }

    [Fact]
    public async Task DeletedEntity_IsAudited()
    {
        var (db, _) = CreateContextWithAudit(TestHelpers.TestUserId);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            VendorType = "Mill",
            IsActive = true
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();

        var deleteLog = db.AuditLogs
            .Where(a => a.EntityId == vendor.Id && a.Action == "Deleted")
            .FirstOrDefault();

        Assert.NotNull(deleteLog);
        Assert.Equal("Vendor", deleteLog.EntityName);
    }

    [Fact]
    public async Task ExcludedEntity_ActiveSession_IsNotAudited()
    {
        var (db, _) = CreateContextWithAudit(TestHelpers.TestUserId);

        var session = new ActiveSession
        {
            Id = Guid.NewGuid(),
            UserId = TestHelpers.TestUserId,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            AssetId = TestHelpers.TestAssetId,
            PlantId = TestHelpers.PlantPlt1Id,
            LoginDateTime = DateTime.UtcNow,
            LastHeartbeatDateTime = DateTime.UtcNow,
        };
        db.ActiveSessions.Add(session);
        await db.SaveChangesAsync();

        var log = db.AuditLogs.FirstOrDefault(a => a.EntityId == session.Id);
        Assert.Null(log);
    }

    [Fact]
    public async Task SensitiveField_PinHash_IsExcluded()
    {
        var (db, _) = CreateContextWithAudit(TestHelpers.TestUserId);

        var user = db.Users.First(u => u.Id == TestHelpers.TestUserId);
        user.PinHash = "some-hash-value";
        await db.SaveChangesAsync();

        var updateLog = db.AuditLogs
            .Where(a => a.EntityId == user.Id && a.Action == "Updated")
            .FirstOrDefault();

        if (updateLog?.Changes != null)
        {
            var changes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(updateLog.Changes);
            Assert.False(changes!.ContainsKey("PinHash"));
        }
    }

    [Fact]
    public async Task NoUserContext_FallsBackToEmptyGuid()
    {
        var (db, _) = CreateContextWithAudit(null);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Anon Vendor",
            VendorType = "Mill",
            IsActive = true
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var log = db.AuditLogs.FirstOrDefault(a => a.EntityId == vendor.Id);
        Assert.NotNull(log);
        Assert.Null(log.ChangedByUserId);
    }

    [Fact]
    public async Task UpdateWithNoRealChanges_DoesNotCreateAuditEntry()
    {
        var (db, _) = CreateContextWithAudit(TestHelpers.TestUserId);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Same Name",
            VendorType = "Mill",
            IsActive = true
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var beforeCount = db.AuditLogs.Count();

        vendor.Name = "Same Name";
        await db.SaveChangesAsync();

        var afterCount = db.AuditLogs.Count();
        Assert.Equal(beforeCount, afterCount);
    }
}
