using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class SellableTankStatusServiceTests
{
    private static readonly Guid SellableProductId = Guid.Parse("b5011111-1111-1111-1111-111111111111");
    private static readonly Guid AssembledProductId = Guid.Parse("b4011111-1111-1111-1111-111111111111");
    private static readonly Guid ShellProductId = Guid.Parse("b3011111-1111-1111-1111-111111111111");

    private static readonly Guid RtXrayCharId = Guid.NewGuid();
    private static readonly Guid SpotXrayCharId = Guid.NewGuid();
    private static readonly Guid HydroCharId = Guid.NewGuid();

    private static readonly Guid RtXrayCpId = Guid.NewGuid();
    private static readonly Guid SpotXrayCpId = Guid.NewGuid();
    private static readonly Guid HydroCpId = Guid.NewGuid();

    private static Data.MesDbContext CreateDbWithGateChecks()
    {
        var db = TestHelpers.CreateInMemoryContext();

        db.Characteristics.AddRange(
            new Characteristic { Id = RtXrayCharId, Name = "RT X-ray" },
            new Characteristic { Id = SpotXrayCharId, Name = "Spot X-ray" },
            new Characteristic { Id = HydroCharId, Name = "Hydro" }
        );

        db.ControlPlans.AddRange(
            new ControlPlan { Id = RtXrayCpId, CharacteristicId = RtXrayCharId, WorkCenterId = TestHelpers.wcRtXrayQueueId, IsEnabled = true, ResultType = "passfail", IsGateCheck = true },
            new ControlPlan { Id = SpotXrayCpId, CharacteristicId = SpotXrayCharId, WorkCenterId = TestHelpers.wcSpotXrayId, IsEnabled = true, ResultType = "passfail", IsGateCheck = true },
            new ControlPlan { Id = HydroCpId, CharacteristicId = HydroCharId, WorkCenterId = TestHelpers.wcHydroId, IsEnabled = true, ResultType = "passfail", IsGateCheck = true }
        );

        db.SaveChanges();
        return db;
    }

    private record TankHierarchy(SerialNumber Sellable, SerialNumber Assembled, SerialNumber Shell);

    private static TankHierarchy SeedTank(
        Data.MesDbContext db, string sellableSerial, int tankSize,
        DateTime sellableCreatedAt, Guid plantId)
    {
        var shell = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = $"SH-{sellableSerial}",
            ProductId = ShellProductId, PlantId = plantId,
            CreatedAt = sellableCreatedAt.AddHours(-6)
        };
        var assembled = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = $"AS-{sellableSerial}",
            ProductId = AssembledProductId, PlantId = plantId,
            CreatedAt = sellableCreatedAt.AddHours(-3)
        };
        var sellable = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = sellableSerial,
            ProductId = SellableProductId, PlantId = plantId,
            CreatedAt = sellableCreatedAt
        };
        db.SerialNumbers.AddRange(shell, assembled, sellable);

        db.TraceabilityLogs.AddRange(
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = shell.Id, ToSerialNumberId = assembled.Id, Relationship = "shell", Timestamp = sellableCreatedAt.AddHours(-5) },
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = assembled.Id, ToSerialNumberId = sellable.Id, Relationship = "hydro-marriage", Timestamp = sellableCreatedAt }
        );

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(), SerialNumberId = sellable.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = sellableCreatedAt
        });

        return new TankHierarchy(sellable, assembled, shell);
    }

    private static void SeedInspection(
        Data.MesDbContext db, Guid serialNumberId, Guid controlPlanId,
        Guid workCenterId, string resultText, DateTime timestamp)
    {
        var prId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = prId, SerialNumberId = serialNumberId,
            WorkCenterId = workCenterId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp
        });
        db.InspectionRecords.Add(new InspectionRecord
        {
            Id = Guid.NewGuid(), SerialNumberId = serialNumberId,
            ProductionRecordId = prId, WorkCenterId = workCenterId,
            OperatorId = TestHelpers.TestUserId, ControlPlanId = controlPlanId,
            ResultText = resultText, Timestamp = timestamp
        });
    }

    [Fact]
    public async Task GetStatus_ReturnsSellableTanksForGivenDayAndSite()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        SeedTank(db, "TANK-001", 120, targetDate, TestHelpers.PlantPlt1Id);
        SeedTank(db, "TANK-002", 250, targetDate.AddHours(2), TestHelpers.PlantPlt1Id);
        SeedTank(db, "TANK-OTHER", 120, targetDate.AddDays(-1), TestHelpers.PlantPlt1Id);
        SeedTank(db, "TANK-SITE2", 120, targetDate, TestHelpers.PlantPlt2Id);
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.SerialNumber == "TANK-001");
        Assert.Contains(result, r => r.SerialNumber == "TANK-002");
        Assert.DoesNotContain(result, r => r.SerialNumber == "TANK-OTHER");
        Assert.DoesNotContain(result, r => r.SerialNumber == "TANK-SITE2");
    }

    [Fact]
    public async Task GetStatus_ReturnsProductNumberAndTankSize()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        SeedTank(db, "TANK-010", 120, targetDate, TestHelpers.PlantPlt1Id);
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Equal("TANK-010", result[0].SerialNumber);
        Assert.Equal("120 AG", result[0].ProductNumber);
        Assert.Equal(120, result[0].TankSize);
    }

    [Fact]
    public async Task GetStatus_MapsGateCheckResults_Accept()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var tank = SeedTank(db, "TANK-ACC", 120, targetDate, TestHelpers.PlantPlt1Id);

        SeedInspection(db, tank.Shell.Id, RtXrayCpId, TestHelpers.wcRtXrayQueueId, "Accept", targetDate.AddHours(-4));
        SeedInspection(db, tank.Assembled.Id, SpotXrayCpId, TestHelpers.wcSpotXrayId, "Accept", targetDate.AddHours(-2));
        SeedInspection(db, tank.Sellable.Id, HydroCpId, TestHelpers.wcHydroId, "Accept", targetDate.AddHours(1));
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Equal("Accept", result[0].RtXrayResult);
        Assert.Equal("Accept", result[0].SpotXrayResult);
        Assert.Equal("Accept", result[0].HydroResult);
    }

    [Fact]
    public async Task GetStatus_MapsGateCheckResults_Reject()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var tank = SeedTank(db, "TANK-REJ", 120, targetDate, TestHelpers.PlantPlt1Id);

        SeedInspection(db, tank.Shell.Id, RtXrayCpId, TestHelpers.wcRtXrayQueueId, "Reject", targetDate.AddHours(-4));
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Equal("Reject", result[0].RtXrayResult);
        Assert.Null(result[0].SpotXrayResult);
        Assert.Null(result[0].HydroResult);
    }

    [Fact]
    public async Task GetStatus_PicksUpResultFromProductionRecord_WhenNoInspectionRecord()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var tank = SeedTank(db, "TANK-PR", 120, targetDate, TestHelpers.PlantPlt1Id);

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = tank.Sellable.Id,
            WorkCenterId = TestHelpers.wcHydroId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = targetDate.AddHours(1),
            InspectionResult = "Acceptable"
        });
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Equal("Acceptable", result[0].HydroResult);
    }

    [Fact]
    public async Task GetStatus_MissingInspection_ReturnsNull()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        SeedTank(db, "TANK-NONE", 120, targetDate, TestHelpers.PlantPlt1Id);
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Null(result[0].RtXrayResult);
        Assert.Null(result[0].SpotXrayResult);
        Assert.Null(result[0].HydroResult);
    }
}
