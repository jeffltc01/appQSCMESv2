using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
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

    private static Guid SeedControlPlan(MESv2.Api.Data.MesDbContext db)
    {
        var charId = Guid.NewGuid();
        db.Characteristics.Add(new Characteristic { Id = charId, Code = "INSP", Name = "Inspection", ProductTypeId = null });
        var cpId = Guid.NewGuid();
        db.ControlPlans.Add(new ControlPlan
        {
            Id = cpId,
            CharacteristicId = charId,
            WorkCenterProductionLineId = TestHelpers.wcplLongSeamInspId,
            IsEnabled = true,
            ResultType = "PassFail",
            IsGateCheck = false,
            IsActive = true
        });
        db.SaveChanges();
        return cpId;
    }

    [Fact]
    public async Task Create_CreatesInspectionRecord_WithNoDefects()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-001", TestHelpers.wcLongSeamId);
        var cpId = SeedControlPlan(db);

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-001",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto> { new() { ControlPlanId = cpId, ResultText = "Pass" } },
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
        var cpId = SeedControlPlan(db);

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-PR-001",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto> { new() { ControlPlanId = cpId, ResultText = "Pass" } },
            Defects = new List<DefectEntryDto>()
        };

        var result = await sut.CreateAsync(dto);

        var prodRecord = await db.ProductionRecords
            .Where(r => r.WorkCenterId == TestHelpers.wcLongSeamInspId
                        && r.SerialNumber.Serial == "SN-INSP-PR-001")
            .FirstOrDefaultAsync();

        Assert.NotNull(prodRecord);
        Assert.Equal(TestHelpers.ProductionLine1Plt1Id, prodRecord.ProductionLineId);
    }

    [Fact]
    public async Task Create_ProductionRecord_HasFailResult_WhenDefectsExist()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-PR-002", TestHelpers.wcLongSeamId);
        var cpId = SeedControlPlan(db);

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
            Results = new List<InspectionResultEntryDto> { new() { ControlPlanId = cpId, ResultText = "Fail" } },
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
    }

    [Fact]
    public async Task Create_CreatesDefectLogs_WhenDefectsProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INSP-002", TestHelpers.wcLongSeamId);
        var cpId = SeedControlPlan(db);

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
            Results = new List<InspectionResultEntryDto> { new() { ControlPlanId = cpId, ResultText = "Fail" } },
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

        var prodRec = await db.ProductionRecords
            .Where(r => r.WorkCenterId == TestHelpers.wcLongSeamInspId && r.SerialNumber.Serial == "SN-INSP-002")
            .FirstAsync();
        var defectLogs = await db.DefectLogs.Where(d => d.ProductionRecordId == prodRec.Id).ToListAsync();
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

    private static Guid SeedControlPlanWithType(MesDbContext db, string resultType, string charName = "Test Char")
    {
        var charId = Guid.NewGuid();
        db.Characteristics.Add(new Characteristic { Id = charId, Code = charName[..3], Name = charName, ProductTypeId = null });
        var cpId = Guid.NewGuid();
        db.ControlPlans.Add(new ControlPlan
        {
            Id = cpId, CharacteristicId = charId,
            WorkCenterProductionLineId = TestHelpers.wcplLongSeamInspId,
            IsEnabled = true, ResultType = resultType, IsGateCheck = false, IsActive = true
        });
        db.SaveChanges();
        return cpId;
    }

    [Theory]
    [InlineData("PassFail", "Pass")]
    [InlineData("PassFail", "Fail")]
    [InlineData("AcceptReject", "Accept")]
    [InlineData("AcceptReject", "Reject")]
    [InlineData("GoNoGo", "Go")]
    [InlineData("GoNoGo", "NoGo")]
    public async Task Create_SetsResultText_MatchingResultTypeVocabulary(string resultType, string resultText)
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, $"SN-RT-{resultType}-{resultText}", TestHelpers.wcLongSeamId);
        var cpId = SeedControlPlanWithType(db, resultType, $"{resultType} Char");

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = $"SN-RT-{resultType}-{resultText}",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto> { new() { ControlPlanId = cpId, ResultText = resultText } }
        };

        await sut.CreateAsync(dto);

        var record = await db.InspectionRecords.FirstOrDefaultAsync(r => r.ControlPlanId == cpId);
        Assert.NotNull(record);
        Assert.Equal(resultText, record.ResultText);
        Assert.Equal(cpId, record.ControlPlanId);
    }

    [Fact]
    public async Task Create_WithMultipleControlPlans_CreatesOneRecordPerPlan()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-MULTI", TestHelpers.wcLongSeamId);
        var cp1 = SeedControlPlanWithType(db, "PassFail", "Thickness");
        var cp2 = SeedControlPlanWithType(db, "AcceptReject", "Visual");

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-MULTI",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto>
            {
                new() { ControlPlanId = cp1, ResultText = "Pass" },
                new() { ControlPlanId = cp2, ResultText = "Accept" }
            }
        };

        await sut.CreateAsync(dto);

        var records = await db.InspectionRecords
            .Where(r => r.SerialNumber.Serial == "SN-MULTI")
            .ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.Contains(records, r => r.ControlPlanId == cp1 && r.ResultText == "Pass");
        Assert.Contains(records, r => r.ControlPlanId == cp2 && r.ResultText == "Accept");
    }

    [Fact]
    public async Task Create_RejectsInvalidResultTextForResultType()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-INVALID", TestHelpers.wcLongSeamId);
        var cpId = SeedControlPlanWithType(db, "PassFail");

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INVALID",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto>
            {
                new() { ControlPlanId = cpId, ResultText = "Accept" }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_RejectsUnknownControlPlanId()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-UNKNOWN-CP", TestHelpers.wcLongSeamId);

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-UNKNOWN-CP",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto>
            {
                new() { ControlPlanId = Guid.NewGuid(), ResultText = "Pass" }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(dto));
    }

    [Fact]
    public async Task Create_TiesDefectsToProductionRecord_NotInspectionRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndProdRecord(db, "SN-DEF-PR", TestHelpers.wcLongSeamId);
        var cpId = SeedControlPlanWithType(db, "PassFail");

        var defectCodeId = Guid.NewGuid();
        var characteristicId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "DX", Name = "DefX" });
        db.Characteristics.Add(new Characteristic { Id = characteristicId, Code = "CX", Name = "CharX", ProductTypeId = null });
        db.DefectLocations.Add(new DefectLocation { Id = locationId, Code = "LX", Name = "LocX", CharacteristicId = characteristicId });
        await db.SaveChangesAsync();

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-DEF-PR",
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Results = new List<InspectionResultEntryDto> { new() { ControlPlanId = cpId, ResultText = "Fail" } },
            Defects = new List<DefectEntryDto>
            {
                new() { DefectCodeId = defectCodeId, CharacteristicId = characteristicId, LocationId = locationId }
            }
        };

        await sut.CreateAsync(dto);

        var defect = await db.DefectLogs.FirstAsync(d => d.DefectCodeId == defectCodeId);
        Assert.NotNull(defect.ProductionRecordId);

        var prodRecord = await db.ProductionRecords
            .Where(r => r.WorkCenterId == TestHelpers.wcLongSeamInspId && r.SerialNumber.Serial == "SN-DEF-PR")
            .FirstAsync();
        Assert.Equal(prodRecord.Id, defect.ProductionRecordId);
    }
}
