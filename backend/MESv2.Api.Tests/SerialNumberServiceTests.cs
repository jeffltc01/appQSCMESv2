using Microsoft.EntityFrameworkCore;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class SerialNumberServiceTests
{
    [Fact]
    public async Task GetContext_UnknownSerial_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new SerialNumberService(db);

        var result = await sut.GetContextAsync("UNKNOWN");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetContext_KnownSerial_ReturnsContext()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        db.SerialNumbers.Add(new SerialNumber { Id = Guid.NewGuid(), Serial = "SH001", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetContextAsync("SH001");

        Assert.NotNull(result);
        Assert.Equal("SH001", result.SerialNumber);
        Assert.Equal(120, result.TankSize);
    }

    [Fact]
    public async Task GetContext_WithAssembly_ReturnsAssemblyInfo()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 500);
        var assembledProduct = db.Products.First(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 500);

        var snId = Guid.NewGuid();
        var assemblySnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber { Id = snId, Serial = "SH100", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.SerialNumbers.Add(new SerialNumber { Id = assemblySnId, Serial = "AA", ProductId = assembledProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = snId,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetContextAsync("SH100");

        Assert.NotNull(result);
        Assert.NotNull(result.ExistingAssembly);
        Assert.Equal("AA", result.ExistingAssembly!.AlphaCode);
    }

    #region Full Hierarchy Setup

    private static readonly Guid SellableProductId = Guid.Parse("b5011111-1111-1111-1111-111111111111");
    private static readonly Guid AssembledProductId = Guid.Parse("b4011111-1111-1111-1111-111111111111");
    private static readonly Guid ShellProductId = Guid.Parse("b3011111-1111-1111-1111-111111111111");

    private record FullHierarchy(
        SerialNumber SellableSn, SerialNumber AssemblySn,
        SerialNumber Shell1Sn, SerialNumber Shell2Sn,
        ProductionRecord ShellPr, ProductionRecord AssemblyPr, ProductionRecord SellablePr);

    private static FullHierarchy SeedFullHierarchy(Data.MesDbContext db)
    {
        var shell1 = new SerialNumber { Id = Guid.NewGuid(), Serial = "SH-001", ProductId = ShellProductId, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var shell2 = new SerialNumber { Id = Guid.NewGuid(), Serial = "SH-002", ProductId = ShellProductId, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var assembled = new SerialNumber { Id = Guid.NewGuid(), Serial = "AB", ProductId = AssembledProductId, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var sellable = new SerialNumber { Id = Guid.NewGuid(), Serial = "SELL-001", ProductId = SellableProductId, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        db.SerialNumbers.AddRange(shell1, shell2, assembled, sellable);

        db.TraceabilityLogs.AddRange(
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = shell1.Id, ToSerialNumberId = assembled.Id, Relationship = "shell", Timestamp = DateTime.UtcNow },
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = shell2.Id, ToSerialNumberId = assembled.Id, Relationship = "shell", Timestamp = DateTime.UtcNow },
            new TraceabilityLog { Id = Guid.NewGuid(), ToSerialNumberId = assembled.Id, Relationship = "leftHead", TankLocation = "LOT-H1", Timestamp = DateTime.UtcNow },
            new TraceabilityLog { Id = Guid.NewGuid(), ToSerialNumberId = assembled.Id, Relationship = "rightHead", TankLocation = "LOT-H2", Timestamp = DateTime.UtcNow },
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = assembled.Id, ToSerialNumberId = sellable.Id, Relationship = "hydro-marriage", Timestamp = DateTime.UtcNow }
        );

        var shellPr = new ProductionRecord
        {
            Id = Guid.NewGuid(), SerialNumberId = shell1.Id, WorkCenterId = TestHelpers.wcLongSeamId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id, OperatorId = TestHelpers.TestUserId,
            Timestamp = new DateTime(2026, 2, 15, 8, 0, 0, DateTimeKind.Utc)
        };
        var assemblyPr = new ProductionRecord
        {
            Id = Guid.NewGuid(), SerialNumberId = assembled.Id, WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id, OperatorId = TestHelpers.TestUserId,
            Timestamp = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc)
        };
        var sellablePr = new ProductionRecord
        {
            Id = Guid.NewGuid(), SerialNumberId = sellable.Id, WorkCenterId = TestHelpers.wcHydroId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id, OperatorId = TestHelpers.TestUserId,
            Timestamp = new DateTime(2026, 2, 15, 14, 0, 0, DateTimeKind.Utc),
            InspectionResult = "pass"
        };
        db.ProductionRecords.AddRange(shellPr, assemblyPr, sellablePr);

        return new FullHierarchy(sellable, assembled, shell1, shell2, shellPr, assemblyPr, sellablePr);
    }

    #endregion

    [Fact]
    public async Task GetLookup_SellableSN_BuildsFullTree()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var h = SeedFullHierarchy(db);
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("SELL-001");

        Assert.NotNull(result);
        Assert.Single(result.TreeNodes);

        var root = result.TreeNodes[0];
        Assert.Contains("SELL-001", root.Label);
        Assert.Equal("sellable", root.NodeType);
        Assert.Single(root.Children);

        var assemblyNode = root.Children[0];
        Assert.Contains("AB", assemblyNode.Label);
        Assert.Equal("assembled", assemblyNode.NodeType);
        Assert.True(assemblyNode.Children.Count >= 3, "Should have 2 shells + at least 1 head");
    }

    [Fact]
    public async Task GetLookup_SellableSN_CollectsEventsFromAllRelatedSNs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var h = SeedFullHierarchy(db);
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("SELL-001");

        Assert.NotNull(result);
        Assert.True(result.Events.Count >= 3, "Should include events from shell, assembly, and sellable");

        var workCenterNames = result.Events.Select(e => e.WorkCenterName).ToList();
        Assert.Contains("Long Seam", workCenterNames);
        Assert.Contains("Fitup", workCenterNames);
        Assert.Contains("Hydro", workCenterNames);
    }

    [Fact]
    public async Task GetLookup_IncludesInspectionRecords()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var h = SeedFullHierarchy(db);

        var charId = Guid.Parse("c1000001-0000-0000-0000-000000000001");
        var cpId = Guid.NewGuid();
        db.ControlPlans.Add(new ControlPlan
        {
            Id = cpId, CharacteristicId = charId,
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            IsEnabled = true, ResultType = "passfail", IsGateCheck = false
        });
        db.InspectionRecords.Add(new InspectionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = h.Shell1Sn.Id,
            ProductionRecordId = h.ShellPr.Id,
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            ControlPlanId = cpId,
            ResultText = "Accept",
            Timestamp = new DateTime(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("SELL-001");

        Assert.NotNull(result);
        var inspEvent = result.Events.FirstOrDefault(e => e.Type.Contains("Long Seam"));
        Assert.NotNull(inspEvent);
        Assert.Equal("Accept", inspEvent.InspectionResult);
    }

    [Fact]
    public async Task GetLookup_ShellSN_StillShowsEvents()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var h = SeedFullHierarchy(db);
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("SH-001");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Events);
    }

    [Fact]
    public async Task GetLookup_StandaloneSN_ReturnsSimpleTree()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "LONE-001",
            ProductId = ShellProductId, PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("LONE-001");

        Assert.NotNull(result);
        Assert.Single(result.TreeNodes);
        Assert.Contains("LONE-001", result.TreeNodes[0].Label);
    }

    private static readonly Guid PlateProductId = Guid.Parse("b1011111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetLookup_ShellWithPlate_ShowsPlateChild()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var plateSn = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "Heat H123 Coil C456",
            ProductId = PlateProductId, PlantId = TestHelpers.PlantPlt1Id,
            HeatNumber = "H123", CoilNumber = "C456", CreatedAt = DateTime.UtcNow
        };
        var shellSn = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "SH-PLT-001",
            ProductId = ShellProductId, PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.AddRange(plateSn, shellSn);
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = plateSn.Id,
            ToSerialNumberId = shellSn.Id,
            Relationship = "plate",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("SH-PLT-001");

        Assert.NotNull(result);
        Assert.Single(result.TreeNodes);

        var shellNode = result.TreeNodes[0];
        Assert.Contains("SH-PLT-001", shellNode.Label);
        Assert.Single(shellNode.Children);

        var plateNode = shellNode.Children[0];
        Assert.Contains("Heat H123 Coil C456", plateNode.Label);
        Assert.Equal("plate", plateNode.NodeType);
    }

    [Fact]
    public async Task GetLookup_SellableTree_ShellsShowPlateChildren()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var h = SeedFullHierarchy(db);

        var plateSn = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "Heat HX Coil CX",
            ProductId = PlateProductId, PlantId = TestHelpers.PlantPlt1Id,
            HeatNumber = "HX", CoilNumber = "CX", CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(plateSn);
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = plateSn.Id,
            ToSerialNumberId = h.Shell1Sn.Id,
            Relationship = "plate",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetLookupAsync("SELL-001");

        Assert.NotNull(result);
        var assemblyNode = result.TreeNodes[0].Children[0];
        var shellWithPlate = assemblyNode.Children.FirstOrDefault(c =>
            c.NodeType == "shell" && c.Label.Contains("SH-001"));
        Assert.NotNull(shellWithPlate);
        Assert.Single(shellWithPlate.Children);
        Assert.Contains("Heat HX Coil CX", shellWithPlate.Children[0].Label);
    }
}
