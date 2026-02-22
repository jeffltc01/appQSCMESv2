using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
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
            WorkCenterId = TestHelpers.wcRollsId,
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
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-CATCHUP", result.SerialNumber);
        Assert.NotNull(result.Warning);
        Assert.Contains("annotation created", result.Warning, StringComparison.OrdinalIgnoreCase);

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
            WorkCenterId = TestHelpers.wcRollsId,
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
            WorkCenterId = TestHelpers.wcRollsId,
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
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotNull(result.Warning);
        Assert.Contains("Duplicate", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_CatchUpFlow_CreatesAnnotation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-CATCHUP-ANN",
            WorkCenterId = TestHelpers.wcLongSeamId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotNull(result.Warning);
        Assert.Contains("annotation created", result.Warning, StringComparison.OrdinalIgnoreCase);

        var annotation = await db.Annotations
            .Include(a => a.AnnotationType)
            .FirstOrDefaultAsync(a => a.ProductionRecordId == result.Id);
        Assert.NotNull(annotation);
        Assert.Equal("Correction Needed", annotation.AnnotationType.Name);
        Assert.True(annotation.Flag);
        Assert.Contains("SN-CATCHUP-ANN", annotation.Notes!);
        Assert.Contains("Rolls scan missed", annotation.Notes!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_ExistingSerial_NoAnnotation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var serial = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-EXISTS",
            ProductId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(serial);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-EXISTS",
            WorkCenterId = TestHelpers.wcLongSeamId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        var annotation = await db.Annotations
            .FirstOrDefaultAsync(a => a.ProductionRecordId == result.Id);
        Assert.Null(annotation);
    }

    [Fact]
    public async Task Create_WithShellSize_ResolvesShellProduct()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-SHELL-120",
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>(),
            ShellSize = "120"
        };

        await sut.CreateAsync(dto);

        var serial = await db.SerialNumbers
            .Include(s => s.Product).ThenInclude(p => p!.ProductType)
            .FirstAsync(s => s.Serial == "SN-SHELL-120");
        Assert.Equal("shell", serial.Product!.ProductType!.SystemTypeName);
    }

    [Fact]
    public async Task Create_WithHeatCoil_CreatesPlateTraceabilityLog()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var plateProduct = db.Products.First(p => p.ProductType!.SystemTypeName == "plate" && p.TankSize == 120);
        var plateSn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "Heat H999 Coil C999",
            ProductId = plateProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            HeatNumber = "H999",
            CoilNumber = "C999",
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(plateSn);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-SHELL-PLT",
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>(),
            ShellSize = "120",
            HeatNumber = "H999",
            CoilNumber = "C999"
        };

        await sut.CreateAsync(dto);

        var shellSn = await db.SerialNumbers.FirstAsync(s => s.Serial == "SN-SHELL-PLT");
        var traceLog = await db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.FromSerialNumberId == plateSn.Id
                && t.ToSerialNumberId == shellSn.Id
                && t.Relationship == "plate");
        Assert.NotNull(traceLog);
    }
}
