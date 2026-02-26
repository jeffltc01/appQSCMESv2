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
            new Characteristic { Id = RtXrayCharId, Code = "100", Name = "RT X-ray" },
            new Characteristic { Id = SpotXrayCharId, Code = "101", Name = "Spot X-ray" },
            new Characteristic { Id = HydroCharId, Code = "102", Name = "Hydro" }
        );

        db.ControlPlans.AddRange(
            new ControlPlan { Id = RtXrayCpId, CharacteristicId = RtXrayCharId, WorkCenterProductionLineId = TestHelpers.wcplRtXrayQueueId, IsEnabled = true, ResultType = "passfail", IsGateCheck = true },
            new ControlPlan { Id = SpotXrayCpId, CharacteristicId = SpotXrayCharId, WorkCenterProductionLineId = TestHelpers.wcplSpotXrayId, IsEnabled = true, ResultType = "passfail", IsGateCheck = true },
            new ControlPlan { Id = HydroCpId, CharacteristicId = HydroCharId, WorkCenterProductionLineId = TestHelpers.wcplHydroId, IsEnabled = true, ResultType = "passfail", IsGateCheck = true }
        );

        db.SaveChanges();
        return db;
    }

    private static Data.MesDbContext CreateDbWithLegacyRtGateCheck()
    {
        var db = TestHelpers.CreateInMemoryContext();

        var longSeamCharId = Guid.NewGuid();
        var legacyRtCpId = Guid.NewGuid();

        db.Characteristics.Add(new Characteristic { Id = longSeamCharId, Code = "100", Name = "Long Seam" });
        db.ControlPlans.Add(new ControlPlan
        {
            Id = legacyRtCpId,
            CharacteristicId = longSeamCharId,
            WorkCenterProductionLineId = TestHelpers.wcplRtXrayQueueId,
            IsEnabled = true,
            ResultType = "Pass/Fail",
            IsGateCheck = true
        });
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
    public async Task GetStatus_PicksUpResultFromInspectionRecord()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var tank = SeedTank(db, "TANK-PR", 120, targetDate, TestHelpers.PlantPlt1Id);

        SeedInspection(db, tank.Sellable.Id, HydroCpId, TestHelpers.wcHydroId, "Accept", targetDate.AddHours(1));
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Equal("Accept", result[0].HydroResult);
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

    [Fact]
    public async Task GetStatus_UsesNumericResultAndWorkCenterFallbackForLegacyRtData()
    {
        await using var db = CreateDbWithLegacyRtGateCheck();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var tank = SeedTank(db, "TANK-LEGACY-RT", 120, targetDate, TestHelpers.PlantPlt1Id);

        var legacyRtCpId = db.ControlPlans.Single().Id;
        var prId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = prId,
            SerialNumberId = tank.Shell.Id,
            WorkCenterId = TestHelpers.wcRtXrayQueueId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = targetDate.AddHours(-2)
        });
        db.InspectionRecords.Add(new InspectionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = tank.Shell.Id,
            ProductionRecordId = prId,
            WorkCenterId = TestHelpers.wcRtXrayQueueId,
            OperatorId = TestHelpers.TestUserId,
            ControlPlanId = legacyRtCpId,
            ResultText = null,
            ResultNumeric = 1m,
            Timestamp = targetDate.AddHours(-2)
        });
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        Assert.Single(result);
        Assert.Equal("Accept", result[0].RtXrayResult);
    }

    [Fact]
    public async Task GetStatus_UsesDataEntryTypeForRealtimeClassification()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);
        var tank = SeedTank(db, "TANK-DET-RT", 120, targetDate, TestHelpers.PlantPlt1Id);

        var genericCharId = Guid.NewGuid();
        var realtimeWcId = Guid.NewGuid();
        var realtimeWcplId = Guid.NewGuid();
        var realtimeCpId = Guid.NewGuid();
        var xrayTypeId = db.WorkCenters
            .Where(w => w.Id == TestHelpers.wcRtXrayQueueId)
            .Select(w => w.WorkCenterTypeId)
            .Single();

        db.Characteristics.Add(new Characteristic
        {
            Id = genericCharId,
            Code = "DET",
            Name = "Generic Gate"
        });
        db.WorkCenters.Add(new WorkCenter
        {
            Id = realtimeWcId,
            Name = "Gate Station 1",
            WorkCenterTypeId = xrayTypeId,
            NumberOfWelders = 0,
            DataEntryType = "RealTimeXray"
        });
        db.WorkCenterProductionLines.Add(new WorkCenterProductionLine
        {
            Id = realtimeWcplId,
            WorkCenterId = realtimeWcId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            DisplayName = "Gate Station 1"
        });
        db.ControlPlans.Add(new ControlPlan
        {
            Id = realtimeCpId,
            CharacteristicId = genericCharId,
            WorkCenterProductionLineId = realtimeWcplId,
            IsEnabled = true,
            IsGateCheck = true,
            ResultType = "Pass/Fail"
        });

        SeedInspection(db, tank.Shell.Id, realtimeCpId, realtimeWcId, "Accept", targetDate.AddHours(-2));
        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        var row = Assert.Single(result, r => r.SerialNumber == "TANK-DET-RT");
        Assert.Equal("Accept", row.RtXrayResult);
    }

    [Fact]
    public async Task GetStatus_MigratedTraceabilityVariants_StillMapsAssemblyShellsAndGateChecks()
    {
        await using var db = CreateDbWithGateChecks();
        var targetDate = new DateTime(2026, 2, 20, 12, 0, 0, DateTimeKind.Utc);

        var shell = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SH-MIG-001",
            ProductId = ShellProductId,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = targetDate.AddHours(-6)
        };
        var assembled = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "AS-MIG-001",
            ProductId = AssembledProductId,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = targetDate.AddHours(-3)
        };
        var sellable = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "TANK-MIG-001",
            ProductId = SellableProductId,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = targetDate
        };

        // Simulate migrated/cased product type data.
        assembled.Product = db.Products.First(p => p.Id == AssembledProductId);
        assembled.Product.ProductType!.SystemTypeName = "Assembeled";
        sellable.Product = db.Products.First(p => p.Id == SellableProductId);
        sellable.Product.ProductType!.SystemTypeName = "Sellable";

        db.SerialNumbers.AddRange(shell, assembled, sellable);

        // Reversed + component-style links seen in migrated data.
        db.TraceabilityLogs.AddRange(
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = assembled.Id,
                ToSerialNumberId = shell.Id,
                Relationship = "component",
                TankLocation = "Shell 1",
                Timestamp = targetDate.AddHours(-5)
            },
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = sellable.Id,
                ToSerialNumberId = assembled.Id,
                Relationship = "component",
                Timestamp = targetDate
            }
        );

        SeedInspection(db, shell.Id, RtXrayCpId, TestHelpers.wcRtXrayQueueId, "Accept", targetDate.AddHours(-4));
        SeedInspection(db, assembled.Id, SpotXrayCpId, TestHelpers.wcSpotXrayId, "Accept", targetDate.AddHours(-2));
        SeedInspection(db, sellable.Id, HydroCpId, TestHelpers.wcHydroId, "Accept", targetDate.AddHours(1));

        await db.SaveChangesAsync();

        var sut = new SellableTankStatusService(db);
        var result = await sut.GetStatusAsync(TestHelpers.PlantPlt1Id, new DateOnly(2026, 2, 20));

        var row = Assert.Single(result, r => r.SerialNumber == "TANK-MIG-001");
        Assert.Equal("AS-MIG-001", row.AlphaCode);
        Assert.Contains("SH-MIG-001", row.ShellSerials);
        Assert.Equal("Accept", row.RtXrayResult);
        Assert.Equal("Accept", row.SpotXrayResult);
        Assert.Equal("Accept", row.HydroResult);
    }
}
