using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;

namespace MESv2.Api.Tests;

/// <summary>
/// Creates an in-memory database with MesDbContext and seeds test data:
/// Plant PLT1, WorkCenterType "Rolls", WorkCenter, ProductionLine, Asset, User EMP001.
/// Work centers are shared across all plants (canonical IDs).
/// </summary>
public static class TestHelpers
{
    public static readonly Guid PlantPlt1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid WorkCenterTypeRollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ProductionLine1Plt1Id = Guid.Parse("e1111111-1111-1111-1111-111111111111");
    public static readonly Guid TestUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid TestAssetId = Guid.Parse("a0000001-0000-0000-0000-000000000001");

    // Canonical work center IDs (shared across all plants)
    public static readonly Guid wcRollsId = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    public static readonly Guid wcLongSeamId = Guid.Parse("f2111111-1111-1111-1111-111111111111");
    public static readonly Guid wcLongSeamInspId = Guid.Parse("f3111111-1111-1111-1111-111111111111");
    public static readonly Guid wcRtXrayQueueId = Guid.Parse("f4111111-1111-1111-1111-111111111111");
    public static readonly Guid wcFitupId = Guid.Parse("f5111111-1111-1111-1111-111111111111");
    public static readonly Guid wcRoundSeamId = Guid.Parse("f6111111-1111-1111-1111-111111111111");
    public static readonly Guid wcRoundSeamInspId = Guid.Parse("f7111111-1111-1111-1111-111111111111");
    public static readonly Guid wcSpotXrayId = Guid.Parse("f8111111-1111-1111-1111-111111111111");
    public static readonly Guid wcNameplateId = Guid.Parse("f9111111-1111-1111-1111-111111111111");
    public static readonly Guid wcHydroId = Guid.Parse("fa111111-1111-1111-1111-111111111111");
    public static readonly Guid wcRollsMaterialId = Guid.Parse("fb111111-1111-1111-1111-111111111111");
    public static readonly Guid wcFitupQueueId = Guid.Parse("fc111111-1111-1111-1111-111111111111");

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
        DbInitializer.Seed(context);

        return context;
    }
}
