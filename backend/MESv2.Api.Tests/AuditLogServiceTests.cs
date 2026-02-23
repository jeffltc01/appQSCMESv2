using MESv2.Api.Data;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AuditLogServiceTests
{
    private static (AuditLogService service, MesDbContext db) CreateService()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var service = new AuditLogService(db);
        return (service, db);
    }

    private static void SeedAuditLogs(MesDbContext db)
    {
        var userId = TestHelpers.TestUserId;
        var now = DateTime.UtcNow;
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();

        db.AuditLogs.AddRange(
            new AuditLog
            {
                Action = "Created",
                EntityName = "Vendor",
                EntityId = entityId1,
                Changes = """{"Name":{"old":null,"new":"Acme"}}""",
                ChangedByUserId = userId,
                ChangedAtUtc = now.AddMinutes(-10),
            },
            new AuditLog
            {
                Action = "Updated",
                EntityName = "Vendor",
                EntityId = entityId1,
                Changes = """{"Name":{"old":"Acme","new":"Acme Corp"}}""",
                ChangedByUserId = userId,
                ChangedAtUtc = now.AddMinutes(-5),
            },
            new AuditLog
            {
                Action = "Created",
                EntityName = "Product",
                EntityId = entityId2,
                Changes = """{"ProductNumber":{"old":null,"new":"P100"}}""",
                ChangedByUserId = userId,
                ChangedAtUtc = now.AddMinutes(-3),
            }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetLogs_ReturnsAll_WhenNoFilters()
    {
        var (service, db) = CreateService();
        SeedAuditLogs(db);

        var result = await service.GetLogsAsync(null, null, null, null, null, null, 1, 50, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetLogs_FiltersByEntityName()
    {
        var (service, db) = CreateService();
        SeedAuditLogs(db);

        var result = await service.GetLogsAsync("Vendor", null, null, null, null, null, 1, 50, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("Vendor", i.EntityName));
    }

    [Fact]
    public async Task GetLogs_FiltersByAction()
    {
        var (service, db) = CreateService();
        SeedAuditLogs(db);

        var result = await service.GetLogsAsync(null, null, "Created", null, null, null, 1, 50, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("Created", i.Action));
    }

    [Fact]
    public async Task GetLogs_Pagination_Works()
    {
        var (service, db) = CreateService();
        SeedAuditLogs(db);

        var page1 = await service.GetLogsAsync(null, null, null, null, null, null, 1, 2, CancellationToken.None);
        var page2 = await service.GetLogsAsync(null, null, null, null, null, null, 2, 2, CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task GetLogs_OrderByMostRecentFirst()
    {
        var (service, db) = CreateService();
        SeedAuditLogs(db);

        var result = await service.GetLogsAsync(null, null, null, null, null, null, 1, 50, CancellationToken.None);

        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            Assert.True(result.Items[i].ChangedAtUtc >= result.Items[i + 1].ChangedAtUtc);
        }
    }

    [Fact]
    public async Task GetEntityNames_ReturnsDistinctSorted()
    {
        var (service, db) = CreateService();
        SeedAuditLogs(db);

        var names = await service.GetEntityNamesAsync(CancellationToken.None);

        Assert.Equal(2, names.Count);
        Assert.Equal("Product", names[0]);
        Assert.Equal("Vendor", names[1]);
    }
}
