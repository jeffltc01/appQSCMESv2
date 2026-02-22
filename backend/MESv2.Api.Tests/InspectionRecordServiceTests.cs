using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class InspectionRecordServiceTests
{
    private static (Guid snId, Guid prodRecordId) SeedSerialAndProdRecord(MESv2.Api.Data.MesDbContext db, string serial, Guid wcId)
    {
        var snId = Guid.NewGuid();
        var prodId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber { Id = snId, Serial = serial, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = prodId,
            SerialNumberId = snId,
            WorkCenterId = wcId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        db.SaveChanges();
        return (snId, prodId);
    }

    [Fact]
    public async Task Create_CreatesInspectionRecord_WithNoDefects()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-001", TestHelpers.wcLongSeamId);

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-001",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-INSP-001", result.SerialNumber);
        Assert.Equal(TestHelpers.wcLongSeamInspId, result.WorkCenterId);
        Assert.Equal(TestHelpers.TestUserId, result.OperatorId);
        Assert.Empty(result.Defects);

        var record = await db.InspectionRecords.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(record);
    }

    [Fact]
    public async Task Create_CreatesProductionRecord_AtInspectionWorkCenter()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-PR-001", TestHelpers.wcLongSeamId);

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-PR-001",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        };

        var result = await sut.CreateAsync(dto);

        var prodRecord = await db.ProductionRecords
            .Where(r => r.WorkCenterId == TestHelpers.wcLongSeamInspId
                        && r.SerialNumber.Serial == "SN-INSP-PR-001")
            .FirstOrDefaultAsync();

        Assert.NotNull(prodRecord);
        Assert.Equal(TestHelpers.ProductionLine1Plt1Id, prodRecord.ProductionLineId);
        Assert.Equal("Pass", prodRecord.InspectionResult);
    }

    [Fact]
    public async Task Create_ProductionRecord_HasFailResult_WhenDefectsExist()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-PR-002", TestHelpers.wcLongSeamId);

        var defectCodeId = Guid.NewGuid();
        var characteristicId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "D1", Name = "Defect 1" });
        db.Characteristics.Add(new Characteristic { Id = characteristicId, Code = "T01", Name = "Char 1", ProductTypeId = null });
        db.DefectLocations.Add(new DefectLocation { Id = locationId, Code = "L1", Name = "Location 1", CharacteristicId = characteristicId });
        await db.SaveChangesAsync();

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-PR-002",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = defectCodeId, CharacteristicId = characteristicId, LocationId = locationId }
            }
        };

        await sut.CreateAsync(dto);

        var prodRecord = await db.ProductionRecords
            .Where(r => r.WorkCenterId == TestHelpers.wcLongSeamInspId
                        && r.SerialNumber.Serial == "SN-INSP-PR-002")
            .FirstOrDefaultAsync();

        Assert.NotNull(prodRecord);
        Assert.Equal("Fail", prodRecord.InspectionResult);
    }

    [Fact]
    public async Task Create_CreatesDefectLogs_WhenDefectsProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-002", TestHelpers.wcLongSeamId);

        var defectCodeId = Guid.NewGuid();
        var characteristicId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "D1", Name = "Defect 1" });
        db.Characteristics.Add(new Characteristic { Id = characteristicId, Code = "T01", Name = "Char 1", ProductTypeId = null });
        db.DefectLocations.Add(new DefectLocation { Id = locationId, Code = "L1", Name = "Location 1", CharacteristicId = characteristicId });
        await db.SaveChangesAsync();

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-002",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = defectCodeId, CharacteristicId = characteristicId, LocationId = locationId }
            }
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(result.Defects);
        Assert.Equal(defectCodeId, result.Defects[0].DefectCodeId);
        Assert.Equal(characteristicId, result.Defects[0].CharacteristicId);
        Assert.Equal(locationId, result.Defects[0].LocationId);

        var defectLogs = await db.DefectLogs.Where(d => d.InspectionRecordId == result.Id).ToListAsync();
        Assert.Single(defectLogs);
    }

    [Fact]
    public async Task Create_ThrowsArgumentException_WhenDefectHasEmptyDefectCodeId()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-003",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = Guid.Empty, CharacteristicId = Guid.NewGuid(), LocationId = Guid.NewGuid() }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_ThrowsArgumentException_WhenDefectHasEmptyCharacteristicId()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-004",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = Guid.NewGuid(), CharacteristicId = Guid.Empty, LocationId = Guid.NewGuid() }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_ThrowsArgumentException_WhenDefectHasEmptyLocationId()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-005",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = Guid.NewGuid(), CharacteristicId = Guid.NewGuid(), LocationId = Guid.Empty }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(dto));
    }
}
