using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class HydroServiceTests
{
    [Fact]
    public async Task Create_AcceptedWithNoDefects_Succeeds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100001");
        SeedAssemblySn(db, "AA");

        var sut = new HydroService(db);

        var result = await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "AA",
            NameplateSerialNumber = "W00100001",
            Results = new List<InspectionResultEntryDto>(),
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        Assert.NotNull(result);
        Assert.Equal("AA", result.AssemblyAlphaCode);
    }

    [Fact]
    public async Task Create_CreatesTraceabilityLog()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sellableSnId = SeedSellableSn(db, "W00100002");
        var assemblySnId = SeedAssemblySn(db, "AB");

        var sut = new HydroService(db);

        await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "AB",
            NameplateSerialNumber = "W00100002",
            Results = new List<InspectionResultEntryDto>(),
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        var marriageTrace = db.TraceabilityLogs.FirstOrDefault(t =>
            t.FromSerialNumberId == assemblySnId &&
            t.ToSerialNumberId == sellableSnId &&
            t.Relationship == "hydro-marriage");
        Assert.NotNull(marriageTrace);
        Assert.NotNull(marriageTrace.ProductionRecordId);

        var nameplateTrace = db.TraceabilityLogs.FirstOrDefault(t =>
            t.FromSerialNumberId == sellableSnId &&
            t.Relationship == "NameplateToAssembly");
        Assert.NotNull(nameplateTrace);
        Assert.NotNull(nameplateTrace.ProductionRecordId);
        Assert.Equal(marriageTrace.ProductionRecordId, nameplateTrace.ProductionRecordId);
    }

    [Fact]
    public async Task GetLocationsByCharacteristic_ReturnsFiltered()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var charId = Guid.NewGuid();
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Test" });
        db.Characteristics.Add(new Characteristic { Id = charId, Code = "T01", Name = "RS1", ProductTypeId = ptId });
        db.DefectLocations.Add(new DefectLocation { Id = Guid.NewGuid(), Code = "LOC1", Name = "Location 1", CharacteristicId = charId });
        db.DefectLocations.Add(new DefectLocation { Id = Guid.NewGuid(), Code = "LOC2", Name = "Location 2", CharacteristicId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var sut = new HydroService(db);
        var result = await sut.GetLocationsByCharacteristicAsync(charId);

        Assert.Single(result);
        Assert.Equal("LOC1", result[0].Code);
    }

    [Fact]
    public async Task Create_TankSizeMismatch_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100010", tankSize: 250);
        SeedAssemblySn(db, "AC", tankSize: 120);

        var sut = new HydroService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateAsync(new CreateHydroRecordDto
            {
                AssemblyAlphaCode = "AC",
                NameplateSerialNumber = "W00100010",
                Results = new List<InspectionResultEntryDto>(),
                WorkCenterId = TestHelpers.wcHydroId,
                OperatorId = TestHelpers.TestUserId,
                Defects = new List<DefectEntryDto>()
            }));

        Assert.Contains("Tank size mismatch", ex.Message);
    }

    [Fact]
    public async Task Create_MatchingTankSizes_Succeeds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100011", tankSize: 250);
        SeedAssemblySn(db, "AD", tankSize: 250);

        var sut = new HydroService(db);

        var result = await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "AD",
            NameplateSerialNumber = "W00100011",
            Results = new List<InspectionResultEntryDto>(),
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        Assert.NotNull(result);
    }

    private static Guid SeedSellableSn(MESv2.Api.Data.MesDbContext db, string serial, int tankSize = 120)
    {
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == tankSize);
        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = serial,
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return snId;
    }

    private static Guid SeedAssemblySn(MESv2.Api.Data.MesDbContext db, string alphaCode, int tankSize = 120)
    {
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == tankSize);
        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = alphaCode,
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return snId;
    }
}
