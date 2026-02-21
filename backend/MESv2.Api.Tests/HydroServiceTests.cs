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
        var sut = new HydroService(db);

        var result = await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "AA",
            NameplateSerialNumber = "W00100001",
            Result = "ACCEPTED",
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        Assert.NotNull(result);
        Assert.Equal("ACCEPTED", result.Result);
        Assert.Equal("AA", result.AssemblyAlphaCode);
    }

    [Fact]
    public async Task Create_CreatesTraceabilityLog()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new HydroService(db);

        await sut.CreateAsync(new CreateHydroRecordDto
        {
            AssemblyAlphaCode = "AB",
            NameplateSerialNumber = "W00100002",
            Result = "ACCEPTED",
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        });

        var trace = db.TraceabilityLogs.FirstOrDefault(t => t.FromAlphaCode == "AB");
        Assert.NotNull(trace);
        Assert.Equal("hydro-marriage", trace.Relationship);
    }

    [Fact]
    public async Task GetLocationsByCharacteristic_ReturnsFiltered()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var charId = Guid.NewGuid();
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Test" });
        db.Characteristics.Add(new Characteristic { Id = charId, Name = "RS1", ProductTypeId = ptId });
        db.DefectLocations.Add(new DefectLocation { Id = Guid.NewGuid(), Code = "LOC1", Name = "Location 1", CharacteristicId = charId });
        db.DefectLocations.Add(new DefectLocation { Id = Guid.NewGuid(), Code = "LOC2", Name = "Location 2", CharacteristicId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var sut = new HydroService(db);
        var result = await sut.GetLocationsByCharacteristicAsync(charId);

        Assert.Single(result);
        Assert.Equal("LOC1", result[0].Code);
    }
}
