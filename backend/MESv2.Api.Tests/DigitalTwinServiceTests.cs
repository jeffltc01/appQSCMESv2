using Microsoft.Extensions.Logging;
using Moq;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class DigitalTwinServiceTests
{
    private static readonly Guid CharLongSeamId = Guid.Parse("c1000001-0000-0000-0000-000000000001");
    private static readonly Guid DefectCodeId = Guid.Parse("d1010001-0000-0000-0000-000000000001");
    private static readonly Guid DefectLocationId = Guid.Parse("d1000001-0000-0000-0000-000000000001");

    // Cleveland plant uses America/Chicago (Central Time).
    // 2026-06-10 18:00 UTC = 12:00 CDT (solidly mid-day)
    private static readonly DateTime MidDay = new(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);

    private DigitalTwinService CreateService(Data.MesDbContext db) =>
        new(db, new Mock<ILogger<DigitalTwinService>>().Object);

    private static void SeedRecord(
        Data.MesDbContext db, Guid wcId, DateTime timestamp, Guid? snId = null, string? serial = null, Guid? plantGearId = null)
    {
        var id = snId ?? Guid.NewGuid();
        var tracked = db.ChangeTracker.Entries<SerialNumber>().Any(e => e.Entity.Id == id);
        if (!tracked && !db.SerialNumbers.Any(s => s.Id == id))
            db.SerialNumbers.Add(new SerialNumber { Id = id, Serial = serial ?? id.ToString("N")[..6] });

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = id,
            WorkCenterId = wcId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp,
            PlantGearId = plantGearId,
        });
    }

    private static void SeedCapacityTarget(Data.MesDbContext db, Guid plantGearId, decimal targetUnitsPerHour)
    {
        db.WorkCenterCapacityTargets.Add(new WorkCenterCapacityTarget
        {
            Id = Guid.NewGuid(),
            WorkCenterProductionLineId = TestHelpers.wcplHydroId,
            PlantGearId = plantGearId,
            TankSize = null,
            TargetUnitsPerHour = targetUnitsPerHour,
        });
    }

    private static void SeedDefectLog(
        Data.MesDbContext db,
        Guid serialNumberId,
        DateTime timestamp,
        Guid? characteristicId = null)
    {
        db.DefectLogs.Add(new DefectLog
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serialNumberId,
            DefectCodeId = DefectCodeId,
            CharacteristicId = characteristicId ?? CharLongSeamId,
            LocationId = DefectLocationId,
            Timestamp = timestamp,
            CreatedAt = timestamp,
        });
    }

    private static decimal ComputeExpectedLineEfficiency(Data.MesDbContext db, int hydroToday, decimal targetUnitsPerHour)
    {
        var tzId = db.Plants
            .Where(p => p.Id == TestHelpers.PlantPlt1Id)
            .Select(p => p.TimeZoneId)
            .FirstOrDefault();

        var tz = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(tzId))
        {
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            }
            catch (TimeZoneNotFoundException)
            {
                tz = TimeZoneInfo.Utc;
            }
        }

        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localNow.Date, tz);
        var hoursElapsed = (decimal)(utcNow - startOfDay).TotalHours;
        var theoreticalMax = hoursElapsed > 0 && targetUnitsPerHour > 0
            ? targetUnitsPerHour * hoursElapsed
            : 1m;

        return Math.Min(100, Math.Round(hydroToday / theoreticalMax * 100, 0));
    }

    [Fact]
    public async Task GetSnapshot_ReturnsAllProductionStations()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        Assert.Equal(9, result.Stations.Count);
        Assert.Equal("Rolls", result.Stations[0].Name);
        Assert.Equal("Hydro", result.Stations[^1].Name);
    }

    [Fact]
    public async Task GetSnapshot_StationsOrderedBySequence()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var sequences = result.Stations.Select(s => s.Sequence).ToList();
        Assert.Equal(sequences.OrderBy(s => s).ToList(), sequences);
    }

    [Fact]
    public async Task GetSnapshot_WipCount_ReflectsLatestStation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var sn1 = Guid.NewGuid();
        var sn2 = Guid.NewGuid();

        // sn1: Rolls then Long Seam (latest = Long Seam)
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-60), sn1, "SH0001");
        SeedRecord(db, TestHelpers.wcLongSeamId, MidDay.AddMinutes(-30), sn1, "SH0001");

        // sn2: only at Rolls (latest = Rolls)
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-45), sn2, "SH0002");

        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsStation = result.Stations.First(s => s.Name == "Rolls");
        var longSeamStation = result.Stations.First(s => s.Name == "Long Seam");

        Assert.Equal(1, rollsStation.WipCount);
        Assert.Equal(1, longSeamStation.WipCount);
    }

    [Fact]
    public async Task GetSnapshot_EdgeWipCounts_ReflectCurrentUpstreamStationCounts()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var shellAtRolls = Guid.NewGuid();
        var shellAtLongSeam = Guid.NewGuid();
        var shellAtLsInspect = Guid.NewGuid();

        // Latest station = Rolls
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-20), shellAtRolls, "SH1001");

        // Latest station = Long Seam
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-40), shellAtLongSeam, "SH1002");
        SeedRecord(db, TestHelpers.wcLongSeamId, MidDay.AddMinutes(-10), shellAtLongSeam, "SH1002");

        // Latest station = LS Inspect
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-50), shellAtLsInspect, "SH1003");
        SeedRecord(db, TestHelpers.wcLongSeamId, MidDay.AddMinutes(-30), shellAtLsInspect, "SH1003");
        SeedRecord(db, TestHelpers.wcLongSeamInspId, MidDay.AddMinutes(-5), shellAtLsInspect, "SH1003");

        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsToLongSeam = result.EdgeWipCounts.Single(e =>
            e.FromWorkCenterId == TestHelpers.wcRollsId
            && e.ToWorkCenterId == TestHelpers.wcLongSeamId);
        var longSeamToLsInspect = result.EdgeWipCounts.Single(e =>
            e.FromWorkCenterId == TestHelpers.wcLongSeamId
            && e.ToWorkCenterId == TestHelpers.wcLongSeamInspId);

        Assert.Equal(1, rollsToLongSeam.Count);
        Assert.Equal(1, longSeamToLsInspect.Count);
    }

    [Fact]
    public async Task GetSnapshot_BottleneckDetection_HighestWipStationFlagged()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        // Put 3 units at Long Seam (latest station), 1 at Rolls
        for (var i = 0; i < 3; i++)
        {
            var snId = Guid.NewGuid();
            SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-90 + i), snId);
            SeedRecord(db, TestHelpers.wcLongSeamId, MidDay.AddMinutes(-30 + i), snId);
        }

        var snRolls = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, MidDay.AddMinutes(-10), snRolls);
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var bottleneck = result.Stations.SingleOrDefault(s => s.IsBottleneck);
        Assert.NotNull(bottleneck);
        Assert.Equal("Long Seam", bottleneck.Name);
        Assert.Equal(3, bottleneck.WipCount);
    }

    [Fact]
    public async Task GetSnapshot_StationStatus_ActiveWhenRecentRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcRollsId, DateTime.UtcNow.AddMinutes(-5));
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsStation = result.Stations.First(s => s.Name == "Rolls");
        Assert.Equal("Active", rollsStation.Status);
    }

    [Fact]
    public async Task GetSnapshot_StationStatus_IdleWhenNoRecentRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        Assert.All(result.Stations, s => Assert.Equal("Idle", s.Status));
    }

    [Fact]
    public async Task GetSnapshot_Fpy_ComputedForEligibleStation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var snWithDefect = Guid.NewGuid();
        var snWithoutDefect = Guid.NewGuid();
        var now = DateTime.UtcNow;

        SeedRecord(db, TestHelpers.wcRollsId, now.AddMinutes(-30), snWithDefect, "FPY001");
        SeedRecord(db, TestHelpers.wcRollsId, now.AddMinutes(-20), snWithoutDefect, "FPY002");
        SeedDefectLog(db, snWithDefect, now.AddMinutes(-25));
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsStation = result.Stations.First(s => s.Name == "Rolls");
        Assert.Equal(50.0m, rollsStation.FirstPassYieldPercent);
    }

    [Fact]
    public async Task GetSnapshot_Fpy_NullForIneligibleStation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-10));
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var hydroStation = result.Stations.First(s => s.Name == "Hydro");
        Assert.Null(hydroStation.FirstPassYieldPercent);
    }

    [Fact]
    public async Task GetSnapshot_Fpy_NullWhenNoOpportunitiesAtEligibleStation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsStation = result.Stations.First(s => s.Name == "Rolls");
        Assert.Null(rollsStation.FirstPassYieldPercent);
    }

    [Fact]
    public async Task GetSnapshot_GateChecks_MarkedCorrectly()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var gateChecks = result.Stations.Where(s => s.IsGateCheck).Select(s => s.Name).ToList();
        Assert.Contains("RT X-ray", gateChecks);
        Assert.Contains("Spot X-ray", gateChecks);
        Assert.Contains("Hydro", gateChecks);
        Assert.Equal(3, gateChecks.Count);
    }

    [Fact]
    public async Task GetSnapshot_Throughput_CountsHydroRecordsToday()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-30));
        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-60));
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        Assert.Equal(2, result.Throughput.UnitsToday);
    }

    [Fact]
    public async Task GetSnapshot_LineEfficiency_UsesCapacityForMostRecentGear()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var gear1Id = Guid.Parse("61111111-1111-1111-1111-111111111111");
        var gear2Id = Guid.Parse("61111111-1111-1111-1111-111111111112");

        SeedCapacityTarget(db, gear1Id, 4m);
        SeedCapacityTarget(db, gear2Id, 10m);
        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-20), plantGearId: gear1Id);
        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-10), plantGearId: gear2Id);
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var expected = ComputeExpectedLineEfficiency(db, hydroToday: 2, targetUnitsPerHour: 10m);
        Assert.Equal(expected, result.LineEfficiencyPercent);
    }

    [Fact]
    public async Task GetSnapshot_LineEfficiency_FallsBackToAvailableTargetsWhenLatestGearMissing()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var gear1Id = Guid.Parse("61111111-1111-1111-1111-111111111111");
        var gear2Id = Guid.Parse("61111111-1111-1111-1111-111111111112");

        SeedCapacityTarget(db, gear1Id, 5m);
        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-10), plantGearId: gear2Id);
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var expected = ComputeExpectedLineEfficiency(db, hydroToday: 1, targetUnitsPerHour: 5m);
        Assert.Equal(expected, result.LineEfficiencyPercent);
    }

    [Fact]
    public async Task GetSnapshot_LineEfficiency_FallsBackToDefaultWhenNoCapacityTargets()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        SeedRecord(db, TestHelpers.wcHydroId, DateTime.UtcNow.AddMinutes(-10));
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var expected = ComputeExpectedLineEfficiency(db, hydroToday: 1, targetUnitsPerHour: 6m);
        Assert.Equal(expected, result.LineEfficiencyPercent);
    }

    [Fact]
    public async Task GetSnapshot_UnitTracker_ReturnsRecentUnits()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var snId = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, DateTime.UtcNow.AddMinutes(-20), snId, "SH0099");
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        Assert.Single(result.UnitTracker);
        Assert.Equal("SH0099", result.UnitTracker[0].SerialNumber);
        Assert.Equal("Rolls", result.UnitTracker[0].CurrentStationName);
    }

    [Fact]
    public async Task GetSnapshot_UnitTracker_LimitedTo15()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        for (var i = 0; i < 20; i++)
            SeedRecord(db, TestHelpers.wcRollsId, DateTime.UtcNow.AddMinutes(-i));
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        Assert.Equal(15, result.UnitTracker.Count);
    }

    [Fact]
    public async Task GetSnapshot_InspectionStation_ShowsDataWhenProductionRecordExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var snId = Guid.NewGuid();
        SeedRecord(db, TestHelpers.wcRollsId, DateTime.UtcNow.AddMinutes(-90), snId, "SH-INSP-01");
        SeedRecord(db, TestHelpers.wcLongSeamId, DateTime.UtcNow.AddMinutes(-60), snId, "SH-INSP-01");
        SeedRecord(db, TestHelpers.wcLongSeamInspId, DateTime.UtcNow.AddMinutes(-30), snId, "SH-INSP-01");
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var lsInspect = result.Stations.First(s => s.Name == "LS Inspect");
        Assert.Equal(1, lsInspect.WipCount);
        Assert.Equal(1, lsInspect.UnitsToday);
    }

    [Fact]
    public async Task GetSnapshot_MaterialFeeds_IncludesRollsAndFitup()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        Assert.Equal(2, result.MaterialFeeds.Count);
        Assert.Contains(result.MaterialFeeds, f => f.FeedsIntoStation == "Rolls");
        Assert.Contains(result.MaterialFeeds, f => f.FeedsIntoStation == "Fitup");
    }

    [Fact]
    public async Task GetSnapshot_MaterialFeeds_CountItemsOnProductionWC()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var sn1 = new SerialNumber { Id = Guid.NewGuid(), Serial = "PLATE01" };
        var sn2 = new SerialNumber { Id = Guid.NewGuid(), Serial = "LOT01" };
        db.SerialNumbers.AddRange(sn1, sn2);

        // Items are stored under the production WC, not the queue WC
        db.MaterialQueueItems.Add(new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRollsId,
            Position = 1,
            Status = "queued",
            Quantity = 5,
            QueueType = "rolls",
            SerialNumberId = sn1.Id,
            CreatedAt = DateTime.UtcNow,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
        });
        db.MaterialQueueItems.Add(new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcFitupId,
            Position = 1,
            Status = "queued",
            Quantity = 1,
            QueueType = "fitup",
            SerialNumberId = sn2.Id,
            CreatedAt = DateTime.UtcNow,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
        });
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsFeed = result.MaterialFeeds.First(f => f.FeedsIntoStation == "Rolls");
        var fitupFeed = result.MaterialFeeds.First(f => f.FeedsIntoStation == "Fitup");

        Assert.Equal(1, rollsFeed.ItemCount);
        Assert.Contains("1 lots", rollsFeed.QueueLabel);
        Assert.Equal(1, fitupFeed.ItemCount);
        Assert.Contains("1 lots", fitupFeed.QueueLabel);
    }

    [Fact]
    public async Task GetSnapshot_MaterialFeeds_ExcludesItemsFromOtherProductionLine()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var sn1 = new SerialNumber { Id = Guid.NewGuid(), Serial = "PLATE-OTHER" };
        db.SerialNumbers.Add(sn1);

        db.MaterialQueueItems.Add(new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRollsId,
            Position = 1,
            Status = "queued",
            Quantity = 3,
            QueueType = "rolls",
            SerialNumberId = sn1.Id,
            CreatedAt = DateTime.UtcNow,
            ProductionLineId = TestHelpers.ProductionLine1Plt2Id,
        });
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var rollsFeed = result.MaterialFeeds.First(f => f.FeedsIntoStation == "Rolls");
        Assert.Equal(0, rollsFeed.ItemCount);
    }

    [Fact]
    public async Task GetSnapshot_ConsumedShells_ExcludedFromWipAndUnitTracker()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var now = DateTime.UtcNow;
        var shell1 = Guid.NewGuid();
        var shell2 = Guid.NewGuid();
        var assemblySnId = Guid.NewGuid();

        SeedRecord(db, TestHelpers.wcRollsId, now.AddMinutes(-60), shell1, "SH001");
        SeedRecord(db, TestHelpers.wcLongSeamId, now.AddMinutes(-50), shell1, "SH001");
        SeedRecord(db, TestHelpers.wcLongSeamInspId, now.AddMinutes(-40), shell1, "SH001");
        SeedRecord(db, TestHelpers.wcRollsId, now.AddMinutes(-55), shell2, "SH002");
        SeedRecord(db, TestHelpers.wcLongSeamId, now.AddMinutes(-45), shell2, "SH002");
        SeedRecord(db, TestHelpers.wcLongSeamInspId, now.AddMinutes(-35), shell2, "SH002");

        SeedRecord(db, TestHelpers.wcFitupId, now.AddMinutes(-20), assemblySnId, "AE");

        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shell1,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = now.AddMinutes(-20),
        });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shell2,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = now.AddMinutes(-20),
        });
        await db.SaveChangesAsync();

        var result = await sut.GetSnapshotAsync(
            TestHelpers.PlantPlt1Id, TestHelpers.ProductionLine1Plt1Id);

        var lsInspect = result.Stations.First(s => s.Name == "LS Inspect");
        Assert.Equal(0, lsInspect.WipCount);

        var fitup = result.Stations.First(s => s.Name == "Fitup");
        Assert.Equal(1, fitup.WipCount);

        Assert.DoesNotContain(result.UnitTracker, u => u.SerialNumber == "SH001");
        Assert.DoesNotContain(result.UnitTracker, u => u.SerialNumber == "SH002");
        Assert.Contains(result.UnitTracker, u => u.SerialNumber == "AE");
    }
}
