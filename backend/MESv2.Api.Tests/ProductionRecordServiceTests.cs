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
    public async Task Create_CreatesSerial_WhenNotFound_AtRolls_NoCatchUp()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-NEW-ROLLS",
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("SN-NEW-ROLLS", result.SerialNumber);
        Assert.Null(result.Warning);

        var serial = await db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == "SN-NEW-ROLLS");
        Assert.NotNull(serial);

        var annotation = await db.Annotations.FirstOrDefaultAsync(a => a.ProductionRecordId == result.Id);
        Assert.Null(annotation);
    }

    [Fact]
    public async Task Create_CreatesSerial_WhenNotFound_AtDownstream_CatchUpFlow()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-CATCHUP",
            WorkCenterId = TestHelpers.wcLongSeamId,
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
        Assert.Equal(AnnotationStatus.Open, annotation.Status);
        Assert.Contains("SN-CATCHUP-ANN", annotation.Notes!);
        Assert.Contains("Rolls scan missed", annotation.Notes!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_CatchUp_MatchesBySerialNumber_InheritsProductAndTraceability()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shell120 = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var shell500 = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 500);

        var plate100 = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "Heat H100 Coil C100",
            HeatNumber = "H100", CoilNumber = "C100", CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var plate200 = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "Heat H200 Coil C200",
            HeatNumber = "H200", CoilNumber = "C200", CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        // Shell 012743 on lot H100 (120-gal), recorded first
        var rolls743 = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "012743", ProductId = shell120.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-12)
        };
        // Shell 012745 on lot H200 (500-gal), recorded second (later timestamp)
        var rolls745 = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "012745", ProductId = shell500.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-8)
        };
        db.SerialNumbers.AddRange(plate100, plate200, rolls743, rolls745);

        var rec743Id = Guid.NewGuid();
        var rec745Id = Guid.NewGuid();
        db.ProductionRecords.AddRange(
            new ProductionRecord
            {
                Id = rec743Id, SerialNumberId = rolls743.Id,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId, Timestamp = DateTime.UtcNow.AddMinutes(-12)
            },
            new ProductionRecord
            {
                Id = rec745Id, SerialNumberId = rolls745.Id,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId, Timestamp = DateTime.UtcNow.AddMinutes(-8)
            });
        db.TraceabilityLogs.AddRange(
            new TraceabilityLog
            {
                Id = Guid.NewGuid(), FromSerialNumberId = plate100.Id, ToSerialNumberId = rolls743.Id,
                ProductionRecordId = rec743Id, Relationship = "plate", Quantity = 1,
                Timestamp = DateTime.UtcNow.AddMinutes(-12)
            },
            new TraceabilityLog
            {
                Id = Guid.NewGuid(), FromSerialNumberId = plate200.Id, ToSerialNumberId = rolls745.Id,
                ProductionRecordId = rec745Id, Relationship = "plate", Quantity = 1,
                Timestamp = DateTime.UtcNow.AddMinutes(-8)
            });
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);

        // Shell 012744 missed Rolls — should match 012743 (nearest predecessor), NOT 012745 (most recent)
        var result = await sut.CreateAsync(new CreateProductionRecordDto
        {
            SerialNumber = "012744",
            WorkCenterId = TestHelpers.wcLongSeamId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        });

        var serial = await db.SerialNumbers.FirstAsync(s => s.Serial == "012744");
        Assert.Equal(shell120.Id, serial.ProductId);

        var traceLink = await db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.ToSerialNumberId == serial.Id && t.Relationship == "plate");
        Assert.NotNull(traceLink);
        Assert.Equal(plate100.Id, traceLink!.FromSerialNumberId);

        var annotation = await db.Annotations.FirstOrDefaultAsync(a => a.ProductionRecordId == result.Id);
        Assert.NotNull(annotation);
        Assert.Contains("inherited from previous Rolls record", annotation!.Notes!);
        Assert.Contains("verify correct product and material lot", annotation.Notes!);
    }

    [Fact]
    public async Task Create_CatchUp_NoRollsHistory_NoTraceabilityOrProduct()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new ProductionRecordService(db);

        var result = await sut.CreateAsync(new CreateProductionRecordDto
        {
            SerialNumber = "000001",
            WorkCenterId = TestHelpers.wcLongSeamId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        });

        var serial = await db.SerialNumbers.FirstAsync(s => s.Serial == "000001");
        Assert.Null(serial.ProductId);

        var traceLink = await db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.ToSerialNumberId == serial.Id && t.Relationship == "plate");
        Assert.Null(traceLink);

        var annotation = await db.Annotations.FirstOrDefaultAsync(a => a.ProductionRecordId == result.Id);
        Assert.NotNull(annotation);
        Assert.Contains("No previous Rolls record found", annotation!.Notes!);
    }

    [Fact]
    public async Task Create_CatchUp_WithShellSize_UsesShellSizeOverRollsHistory()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shell120 = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var shell500 = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 500);

        var rollsSerial = new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "012800", ProductId = shell120.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        db.SerialNumbers.Add(rollsSerial);
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = rollsSerial.Id,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        await sut.CreateAsync(new CreateProductionRecordDto
        {
            SerialNumber = "012801",
            WorkCenterId = TestHelpers.wcLongSeamId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>(),
            ShellSize = "500"
        });

        var serial = await db.SerialNumbers.FirstAsync(s => s.Serial == "012801");
        Assert.Equal(shell500.Id, serial.ProductId);
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

    [Fact]
    public async Task Create_AtRolls_IncrementsActiveQueueItemQuantityCompleted()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var queueItem = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRollsId,
            Position = 1,
            Status = "active",
            Quantity = 10,
            QuantityCompleted = 0,
            QueueType = "rolls",
            CreatedAt = DateTime.UtcNow
        };
        db.MaterialQueueItems.Add(queueItem);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        await sut.CreateAsync(new CreateProductionRecordDto
        {
            SerialNumber = "SN-QUEUE-DEC",
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        });

        var updated = await db.MaterialQueueItems.FindAsync(queueItem.Id);
        Assert.Equal(1, updated!.QuantityCompleted);
        Assert.Equal("active", updated.Status);
    }

    [Fact]
    public async Task Create_StampsPlantGearId_FromPlantCurrentGear()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var gearId = Guid.Parse("61111111-1111-1111-1111-111111111113");
        var plant = await db.Plants.FindAsync(TestHelpers.PlantPlt1Id);
        plant!.CurrentPlantGearId = gearId;
        await db.SaveChangesAsync();

        var serial = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "SN-GEAR-STAMP",
            ProductId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.SerialNumbers.Add(serial);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        var dto = new CreateProductionRecordDto
        {
            SerialNumber = "SN-GEAR-STAMP",
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        var record = await db.ProductionRecords.FindAsync(result.Id);
        Assert.NotNull(record);
        Assert.Equal(gearId, record.PlantGearId);
    }

    [Fact]
    public async Task Create_AtRolls_CompletesQueueItemWhenQuantityExhausted()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var queueItem = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRollsId,
            Position = 1,
            Status = "active",
            Quantity = 1,
            QuantityCompleted = 0,
            QueueType = "rolls",
            CreatedAt = DateTime.UtcNow
        };
        db.MaterialQueueItems.Add(queueItem);
        await db.SaveChangesAsync();

        var sut = new ProductionRecordService(db);
        await sut.CreateAsync(new CreateProductionRecordDto
        {
            SerialNumber = "SN-QUEUE-LAST",
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        });

        var updated = await db.MaterialQueueItems.FindAsync(queueItem.Id);
        Assert.Equal(1, updated!.QuantityCompleted);
        Assert.Equal("completed", updated.Status);
    }
}
