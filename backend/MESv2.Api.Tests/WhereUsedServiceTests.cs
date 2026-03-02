using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class WhereUsedServiceTests
{
    private static readonly Guid SellableProductId = Guid.Parse("b5011111-1111-1111-1111-111111111111");
    private static readonly Guid AssembledProductId = Guid.Parse("b4011111-1111-1111-1111-111111111111");
    private static readonly Guid ShellProductId = Guid.Parse("b3011111-1111-1111-1111-111111111111");

    private static Data.MesDbContext CreateDb()
    {
        return TestHelpers.CreateInMemoryContext();
    }

    private static (SerialNumber material, SerialNumber shell, SerialNumber assembled, SerialNumber sellable) SeedChain(
        Data.MesDbContext db,
        string heatNumber,
        string coilNumber,
        string lotNumber,
        string sellableSerial,
        Guid plantId,
        Guid productionLineId,
        DateTime createdAtUtc)
    {
        var material = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = $"MAT-{sellableSerial}",
            ProductId = ShellProductId,
            PlantId = plantId,
            HeatNumber = heatNumber,
            CoilNumber = coilNumber,
            LotNumber = lotNumber,
            CreatedAt = createdAtUtc.AddHours(-4)
        };
        var shell = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = $"SH-{sellableSerial}",
            ProductId = ShellProductId,
            PlantId = plantId,
            CreatedAt = createdAtUtc.AddHours(-3)
        };
        var assembled = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = $"AS-{sellableSerial}",
            ProductId = AssembledProductId,
            PlantId = plantId,
            CreatedAt = createdAtUtc.AddHours(-2)
        };
        var sellable = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = sellableSerial,
            ProductId = SellableProductId,
            PlantId = plantId,
            CreatedAt = createdAtUtc
        };

        db.SerialNumbers.AddRange(material, shell, assembled, sellable);

        db.TraceabilityLogs.AddRange(
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = material.Id,
                ToSerialNumberId = shell.Id,
                Relationship = "plate",
                Timestamp = createdAtUtc.AddHours(-3)
            },
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shell.Id,
                ToSerialNumberId = assembled.Id,
                Relationship = "shell",
                Timestamp = createdAtUtc.AddHours(-2)
            },
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = assembled.Id,
                ToSerialNumberId = sellable.Id,
                Relationship = "hydro-marriage",
                Timestamp = createdAtUtc.AddHours(-1)
            });

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sellable.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = productionLineId,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = createdAtUtc
        });

        return (material, shell, assembled, sellable);
    }

    [Fact]
    public async Task SearchAsync_ReturnsLinkedSellable_WithHydroInspectionTimestamp()
    {
        await using var db = CreateDb();
        var timestamp = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var chain = SeedChain(
            db,
            heatNumber: "H-001",
            coilNumber: "C-001",
            lotNumber: "L-001",
            sellableSerial: "W00010001",
            plantId: TestHelpers.PlantPlt1Id,
            productionLineId: TestHelpers.ProductionLine1Plt1Id,
            createdAtUtc: timestamp);

        var hydroControlPlanId = Guid.NewGuid();
        var hydroCharacteristicId = Guid.NewGuid();
        db.Characteristics.Add(new Characteristic
        {
            Id = hydroCharacteristicId,
            Code = "200",
            Name = "Hydro"
        });
        db.ControlPlans.Add(new ControlPlan
        {
            Id = hydroControlPlanId,
            CharacteristicId = hydroCharacteristicId,
            WorkCenterProductionLineId = TestHelpers.wcplHydroId,
            IsEnabled = true,
            ResultType = "PassFail",
            IsGateCheck = true
        });

        var hydroProduction = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = chain.sellable.Id,
            WorkCenterId = TestHelpers.wcHydroId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp.AddHours(2)
        };
        db.ProductionRecords.Add(hydroProduction);
        db.InspectionRecords.Add(new InspectionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = chain.sellable.Id,
            ProductionRecordId = hydroProduction.Id,
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp.AddHours(3),
            ControlPlanId = hydroControlPlanId,
            ResultText = "Accept"
        });

        await db.SaveChangesAsync();

        var sut = new WhereUsedService(db);
        var result = await sut.SearchAsync("H-001", null, null, null);

        var row = Assert.Single(result);
        Assert.Equal("W00010001", row.SerialNumber);
        Assert.Equal("120 AG", row.ProductionNumber);
        Assert.Equal(120, row.TankSize);
        Assert.Equal(timestamp.AddHours(3), row.HydroCompletedAt);
    }

    [Fact]
    public async Task SearchAsync_FallsBackToHydroProductionTimestamp_WhenInspectionMissing()
    {
        await using var db = CreateDb();
        var timestamp = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var chain = SeedChain(
            db,
            heatNumber: "H-002",
            coilNumber: "C-002",
            lotNumber: "L-002",
            sellableSerial: "W00010002",
            plantId: TestHelpers.PlantPlt1Id,
            productionLineId: TestHelpers.ProductionLine1Plt1Id,
            createdAtUtc: timestamp);

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = chain.sellable.Id,
            WorkCenterId = TestHelpers.wcHydroId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = timestamp.AddHours(4)
        });

        await db.SaveChangesAsync();

        var sut = new WhereUsedService(db);
        var result = await sut.SearchAsync(null, "C-002", null, null);

        var row = Assert.Single(result);
        Assert.Equal("W00010002", row.SerialNumber);
        Assert.Equal(timestamp.AddHours(4), row.HydroCompletedAt);
    }

    [Fact]
    public async Task SearchAsync_RespectsSiteFilter()
    {
        await using var db = CreateDb();
        var timestamp = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        SeedChain(
            db,
            heatNumber: "H-003",
            coilNumber: "C-003",
            lotNumber: "L-003",
            sellableSerial: "W00010003",
            plantId: TestHelpers.PlantPlt1Id,
            productionLineId: TestHelpers.ProductionLine1Plt1Id,
            createdAtUtc: timestamp);

        SeedChain(
            db,
            heatNumber: "H-003",
            coilNumber: "C-999",
            lotNumber: "L-999",
            sellableSerial: "W00020003",
            plantId: TestHelpers.PlantPlt2Id,
            productionLineId: TestHelpers.ProductionLine1Plt2Id,
            createdAtUtc: timestamp.AddMinutes(30));

        await db.SaveChangesAsync();

        var sut = new WhereUsedService(db);
        var result = await sut.SearchAsync("H-003", null, null, TestHelpers.PlantPlt1Id);

        var row = Assert.Single(result);
        Assert.Equal("W00010003", row.SerialNumber);
        Assert.Contains("Cleveland", row.Plant);
    }
}
