using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class ProductionRecordServiceTests
{
    [Fact]
    public async Task Create_CreatesRecord_WithExistingSerial()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var serial = new MESv2.Api.Models.SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-001",
            ProductId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(serial);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-001",
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-001", result.SerialNumber);
        Assert.Null(result.Warning);

        var record = await db.ProductionRecords.FirstOrDefaultAsync(r => r.Id == result.Id);
        Assert.NotNull(record);
        Assert.Equal(serial.Id, record.SerialNumberId);
    }

    [Fact]
    public async Task Create_CreatesSerial_WhenNotFound_CatchUpFlow()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-CATCHUP",
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-CATCHUP", result.SerialNumber);
        Assert.NotNull(result.Warning);
        Assert.Contains("catch-up", result.Warning, StringComparison.OrdinalIgnoreCase);

        var serial = await db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == "SN-CATCHUP");
        Assert.NotNull(serial);
    }

    [Fact]
    public async Task Create_CreatesWelderLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var serial = new MESv2.Api.Models.SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-WELDER",
            ProductId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(serial);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-WELDER",
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid> { TestHelpers.TestUserId }
        };

        var result = await sut.CreateAsync(dto);

        var welderLogs = await db.WelderLogs.Where(w => w.ProductionRecordId == result.Id).ToListAsync();
        Assert.Single(welderLogs);
        Assert.Equal(TestHelpers.TestUserId, welderLogs[0].UserId);
    }

    [Fact]
    public async Task Create_DetectsDuplicate_Within5Minutes()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var serial = new MESv2.Api.Models.SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-DUP",
            ProductId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(serial);
        db.ProductionRecords.Add(new MESv2.Api.Models.ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serial.Id,
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow.AddMinutes(-2)
        });
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-DUP",
            WorkCenterId = TestHelpers.WorkCenter1Plt1Id,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotNull(result.Warning);
        Assert.Contains("Duplicate", result.Warning, StringComparison.OrdinalIgnoreCase);
    }
}
