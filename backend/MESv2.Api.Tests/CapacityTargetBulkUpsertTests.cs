using Microsoft.Extensions.Logging;
using Moq;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class CapacityTargetBulkUpsertTests
{
    private static readonly Guid PlantId = TestHelpers.PlantPlt1Id;
    private static readonly Guid Line1Id = TestHelpers.ProductionLine1Plt1Id;
    private static readonly Guid WcplRollsLine1Id = Guid.Parse("d0010001-0000-0000-0000-000000000001");
    private static readonly Guid WcplLongSeamLine1Id = Guid.Parse("d0010001-0000-0000-0000-000000000002");
    private static readonly Guid Gear1Id = Guid.Parse("61111111-1111-1111-1111-111111111111");
    private static readonly Guid Gear2Id = Guid.Parse("61111111-1111-1111-1111-111111111112");

    private static OeeService CreateService(out MESv2.Api.Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var logger = new Mock<ILogger<OeeService>>().Object;
        return new OeeService(db, logger);
    }

    [Fact]
    public async Task BulkUpsert_CreatesDefaultTargets()
    {
        var sut = CreateService(out var db);

        var dto = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = null, TargetUnitsPerHour = 12m },
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear2Id, TankSize = null, TargetUnitsPerHour = 15m },
            }
        };

        var result = await sut.BulkUpsertCapacityTargetsAsync(dto);

        var rollsTargets = result.Where(t => t.WorkCenterProductionLineId == WcplRollsLine1Id).ToList();
        Assert.Equal(2, rollsTargets.Count);
        Assert.Contains(rollsTargets, t => t.PlantGearId == Gear1Id && t.TargetUnitsPerHour == 12m);
        Assert.Contains(rollsTargets, t => t.PlantGearId == Gear2Id && t.TargetUnitsPerHour == 15m);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task BulkUpsert_UpdatesExistingTargets()
    {
        var sut = CreateService(out var db);

        var initial = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = null, TargetUnitsPerHour = 10m },
            }
        };
        await sut.BulkUpsertCapacityTargetsAsync(initial);

        var updated = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = null, TargetUnitsPerHour = 20m },
            }
        };
        var result = await sut.BulkUpsertCapacityTargetsAsync(updated);

        var target = result.Single(t => t.WorkCenterProductionLineId == WcplRollsLine1Id && t.PlantGearId == Gear1Id && t.TankSize == null);
        Assert.Equal(20m, target.TargetUnitsPerHour);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task BulkUpsert_DeletesOmittedTargets()
    {
        var sut = CreateService(out var db);

        var initial = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = null, TargetUnitsPerHour = 10m },
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear2Id, TankSize = null, TargetUnitsPerHour = 15m },
            }
        };
        await sut.BulkUpsertCapacityTargetsAsync(initial);

        var withRemoval = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = null, TargetUnitsPerHour = 10m },
            }
        };
        var result = await sut.BulkUpsertCapacityTargetsAsync(withRemoval);

        Assert.DoesNotContain(result, t => t.WorkCenterProductionLineId == WcplRollsLine1Id && t.PlantGearId == Gear2Id);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task BulkUpsert_HandlesTankSizeTargets()
    {
        var sut = CreateService(out var db);

        var dto = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = 120, TargetUnitsPerHour = 8m },
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = 250, TargetUnitsPerHour = 6m },
            }
        };

        var result = await sut.BulkUpsertCapacityTargetsAsync(dto);

        var sizedTargets = result.Where(t =>
            t.WorkCenterProductionLineId == WcplRollsLine1Id &&
            t.PlantGearId == Gear1Id &&
            t.TankSize != null).ToList();
        Assert.Equal(2, sizedTargets.Count);
        Assert.Contains(sizedTargets, t => t.TankSize == 120 && t.TargetUnitsPerHour == 8m);
        Assert.Contains(sizedTargets, t => t.TankSize == 250 && t.TargetUnitsPerHour == 6m);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task BulkUpsert_MixedDefaultAndTankSizeTargets()
    {
        var sut = CreateService(out var db);

        var dto = new BulkUpsertCapacityTargetsDto
        {
            ProductionLineId = Line1Id,
            Targets = new()
            {
                new() { WorkCenterProductionLineId = WcplRollsLine1Id, PlantGearId = Gear1Id, TankSize = null, TargetUnitsPerHour = 12m },
                new() { WorkCenterProductionLineId = WcplLongSeamLine1Id, PlantGearId = Gear1Id, TankSize = 120, TargetUnitsPerHour = 5m },
                new() { WorkCenterProductionLineId = WcplLongSeamLine1Id, PlantGearId = Gear1Id, TankSize = 250, TargetUnitsPerHour = 4m },
            }
        };

        var result = await sut.BulkUpsertCapacityTargetsAsync(dto);

        var rollsDefault = result.Single(t => t.WorkCenterProductionLineId == WcplRollsLine1Id && t.PlantGearId == Gear1Id && t.TankSize == null);
        Assert.Equal(12m, rollsDefault.TargetUnitsPerHour);

        var lsSized = result.Where(t => t.WorkCenterProductionLineId == WcplLongSeamLine1Id && t.PlantGearId == Gear1Id && t.TankSize != null).ToList();
        Assert.Equal(2, lsSized.Count);

        await db.DisposeAsync();
    }

    [Fact]
    public async Task GetDistinctTankSizes_ReturnsOrderedSizes()
    {
        var sut = CreateService(out var db);

        var sizes = await sut.GetDistinctTankSizesAsync(PlantId);

        Assert.True(sizes.Count > 0);
        for (int i = 1; i < sizes.Count; i++)
        {
            Assert.True(sizes[i] > sizes[i - 1], "Tank sizes should be in ascending order");
        }

        await db.DisposeAsync();
    }
}
