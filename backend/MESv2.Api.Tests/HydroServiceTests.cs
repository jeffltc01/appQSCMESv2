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

    [Fact]
    public async Task Create_SellableSerialNotFound_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new HydroService(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateAsync(new CreateHydroRecordDto
            {
                AssemblyAlphaCode = "AA",
                NameplateSerialNumber = "DOES_NOT_EXIST",
                Results = new List<InspectionResultEntryDto>(),
                WorkCenterId = TestHelpers.wcHydroId,
                OperatorId = TestHelpers.TestUserId,
                Defects = new List<DefectEntryDto>()
            }));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task Create_InvalidControlPlanId_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100020");

        var sut = new HydroService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateAsync(new CreateHydroRecordDto
            {
                AssemblyAlphaCode = "",
                NameplateSerialNumber = "W00100020",
                Results = new List<InspectionResultEntryDto>
                {
                    new() { ControlPlanId = Guid.NewGuid(), ResultText = "Pass" }
                },
                WorkCenterId = TestHelpers.wcHydroId,
                OperatorId = TestHelpers.TestUserId,
                Defects = new List<DefectEntryDto>()
            }));
    }

    [Fact]
    public async Task Create_InvalidResultTextForControlPlan_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100021");
        var cpId = SeedControlPlan(db, "PassFail");

        var sut = new HydroService(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateAsync(new CreateHydroRecordDto
            {
                AssemblyAlphaCode = "",
                NameplateSerialNumber = "W00100021",
                Results = new List<InspectionResultEntryDto>
                {
                    new() { ControlPlanId = cpId, ResultText = "BadValue" }
                },
                WorkCenterId = TestHelpers.wcHydroId,
                OperatorId = TestHelpers.TestUserId,
                Defects = new List<DefectEntryDto>()
            }));

        Assert.Contains("Invalid ResultText", ex.Message);
    }

    [Fact]
    public async Task Create_ValidResult_CreatesInspectionRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100022");
        var cpId = SeedControlPlan(db, "PassFail");

        var sut = new HydroService(db);

        var result = await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "",
            NameplateSerialNumber = "W00100022",
            Results = new List<InspectionResultEntryDto>
            {
                new() { ControlPlanId = cpId, ResultText = "Pass" }
            },
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        var inspections = db.InspectionRecords.Where(i => i.ProductionRecordId == result.Id).ToList();
        Assert.Single(inspections);
        Assert.Equal("Pass", inspections[0].ResultText);
    }

    [Fact]
    public async Task Create_NoAssembly_SkipsMarriageTraceCreatesNameplateTrace()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sellableSnId = SeedSellableSn(db, "W00100023");

        var sut = new HydroService(db);

        await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "NONEXISTENT",
            NameplateSerialNumber = "W00100023",
            Results = new List<InspectionResultEntryDto>(),
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        var marriageTraces = db.TraceabilityLogs.Where(t => t.Relationship == "hydro-marriage").ToList();
        Assert.Empty(marriageTraces);

        var nameplateTrace = db.TraceabilityLogs.FirstOrDefault(t =>
            t.FromSerialNumberId == sellableSnId && t.Relationship == "NameplateToAssembly");
        Assert.NotNull(nameplateTrace);
        Assert.Null(nameplateTrace.ToSerialNumberId);
    }

    [Fact]
    public async Task Create_WithDefects_CreatesDefectLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSellableSn(db, "W00100024");

        var defectCodeId = Guid.NewGuid();
        var charId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "TestPT" });
        db.Characteristics.Add(new Characteristic { Id = charId, Code = "C01", Name = "TestChar", ProductTypeId = ptId });
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "DC01", Name = "Scratch" });
        db.DefectLocations.Add(new DefectLocation { Id = locationId, Code = "DL01", Name = "Top", CharacteristicId = charId });
        await db.SaveChangesAsync();

        var sut = new HydroService(db);

        var result = await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "",
            NameplateSerialNumber = "W00100024",
            Results = new List<InspectionResultEntryDto>(),
            WorkCenterId = TestHelpers.wcHydroId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = defectCodeId, CharacteristicId = charId, LocationId = locationId }
            }
        });

        var defects = db.DefectLogs.Where(d => d.ProductionRecordId == result.Id).ToList();
        Assert.Single(defects);
        Assert.Equal(defectCodeId, defects[0].DefectCodeId);
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

    private static Guid SeedControlPlan(MESv2.Api.Data.MesDbContext db, string resultType)
    {
        var cpId = Guid.NewGuid();
        db.ControlPlans.Add(new ControlPlan
        {
            Id = cpId,
            CharacteristicId = db.Characteristics.First().Id,
            WorkCenterProductionLineId = TestHelpers.wcplHydroId,
            IsEnabled = true,
            ResultType = resultType,
            IsActive = true
        });
        db.SaveChanges();
        return cpId;
    }
}
