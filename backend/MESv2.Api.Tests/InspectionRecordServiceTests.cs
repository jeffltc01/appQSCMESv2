using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class InspectionRecordServiceTests
{
    [Fact]
    public async Task Create_CreatesInspectionRecord_WithNoDefects()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-001",
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId,
            Defects = new List<DefectEntryDto>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-INSP-001", result.SerialNumber);
        Assert.Equal(TestHelpers.wcRollsId, result.WorkCenterId);
        Assert.Equal(TestHelpers.TestUserId, result.OperatorId);
        Assert.Empty(result.Defects);

        var record = await db.InspectionRecords.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(record);
    }

    [Fact]
    public async Task Create_CreatesDefectLogs_WhenDefectsProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var defectCodeId = Guid.NewGuid();
        var characteristicId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "D1", Name = "Defect 1" });
        db.Characteristics.Add(new Characteristic { Id = characteristicId, Name = "Char 1", ProductTypeId = null });
        db.DefectLocations.Add(new DefectLocation { Id = locationId, Code = "L1", Name = "Location 1", CharacteristicId = characteristicId });
        await db.SaveChangesAsync();

        var sut = new InspectionRecordService(db);
        var dto = new CreateInspectionRecordDto
        {
            SerialNumber = "SN-INSP-002",
            WorkCenterId = TestHelpers.wcRollsId,
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
}
