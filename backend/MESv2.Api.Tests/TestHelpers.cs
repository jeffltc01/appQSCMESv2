using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.Models;

namespace MESv2.Api.Tests;

/// <summary>
/// Creates an in-memory database with MesDbContext and seeds test data:
/// Plant PLT1, WorkCenterType "Rolls", WorkCenter, ProductionLine, Asset, User EMP001.
/// </summary>
public static class TestHelpers
{
    public static readonly Guid PlantPlt1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid WorkCenterTypeRollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ProductionLine1Plt1Id = Guid.Parse("e1111111-1111-1111-1111-111111111111");
    public static readonly Guid WorkCenter1Plt1Id = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    public static readonly Guid TestUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid TestAssetId = Guid.Parse("a0000001-0000-0000-0000-000000000001");

    /// <summary>
    /// Creates a new MesDbContext using an in-memory database with a unique name.
    /// Database is created and seeded (DbContext seed data + one Asset for the first work center).
    /// </summary>
    public static MesDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var name = databaseName ?? "TestDb_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var context = new MesDbContext(options);
        context.Database.EnsureCreated();

        if (!context.Assets.Any())
        {
            context.Assets.Add(new Asset
            {
                Id = TestAssetId,
                Name = "Test Asset 1",
                WorkCenterId = WorkCenter1Plt1Id
            });
            context.SaveChanges();
        }

        return context;
    }
}
