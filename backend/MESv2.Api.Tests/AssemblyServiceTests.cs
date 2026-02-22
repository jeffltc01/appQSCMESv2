using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AssemblyServiceTests
{
    [Fact]
    public async Task GetNextAlphaCode_ReturnsAA_WhenNoExistingAssemblies()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new AssemblyService(db);

        var result = await sut.GetNextAlphaCodeAsync(TestHelpers.PlantPlt1Id);

        Assert.Equal("AA", result);
    }

    [Fact]
    public async Task GetNextAlphaCode_ReturnsAB_WhenAAExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "AA",
            ProductId = assembledProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestHelpers.TestUserId
        });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);

        var result = await sut.GetNextAlphaCodeAsync(TestHelpers.PlantPlt1Id);

        Assert.Equal("AB", result);
    }

    [Fact]
    public async Task Create_AssignsAlphaCode_And_CreatesTraceabilityLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellSerial = "SHELL-001";
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = shellSerial,
            ProductId = shellProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);
        var dto = new CreateAssemblyDto
        {
            Shells = new List<string> { shellSerial },
            LeftHeadLotId = "LOT-L",
            RightHeadLotId = "LOT-R",
            TankSize = 120,
            WorkCenterId = TestHelpers.wcFitupId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("AA", result.AlphaCode);

        var assemblySn = await db.SerialNumbers.FirstAsync(s => s.Serial == "AA");
        var shellLogs = await db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "ShellToAssembly")
            .ToListAsync();
        Assert.Single(shellLogs);
        Assert.NotNull(shellLogs[0].FromSerialNumberId);
        Assert.Equal("Shell 1", shellLogs[0].TankLocation);

        var prodRecord = await db.ProductionRecords.FirstOrDefaultAsync(r => r.SerialNumberId == assemblySn.Id);
        Assert.NotNull(prodRecord);
        Assert.Equal(prodRecord.Id, shellLogs[0].ProductionRecordId);

        var headLogs = await db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "HeadToAssembly")
            .OrderBy(t => t.TankLocation)
            .ToListAsync();
        Assert.Equal(2, headLogs.Count);
        Assert.Equal("Head 1", headLogs[0].TankLocation);
        Assert.Equal("Head 2", headLogs[1].TankLocation);
        Assert.Equal(prodRecord.Id, headLogs[0].ProductionRecordId);
    }

    [Fact]
    public async Task Create_CreatesWelderLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "SHELL-W1",
            ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);
        var dto = new CreateAssemblyDto
        {
            Shells = new List<string> { "SHELL-W1" },
            LeftHeadLotId = "LOT-L", RightHeadLotId = "LOT-R",
            TankSize = 120,
            WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid> { TestHelpers.TestUserId }
        };

        var result = await sut.CreateAsync(dto);

        var assemblySn = await db.SerialNumbers.FirstAsync(s => s.Serial == result.AlphaCode);
        var prodRecord = await db.ProductionRecords.FirstAsync(r => r.SerialNumberId == assemblySn.Id);
        var welderLogs = db.WelderLogs.Where(w => w.ProductionRecordId == prodRecord.Id).ToList();
        Assert.Single(welderLogs);
        Assert.Equal(TestHelpers.TestUserId, welderLogs[0].UserId);
    }

    [Fact]
    public async Task Create_StoresHeadCoilHeatData()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(), Serial = "SHELL-H1",
            ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);
        var dto = new CreateAssemblyDto
        {
            Shells = new List<string> { "SHELL-H1" },
            LeftHeadLotId = "LOT-L", RightHeadLotId = "LOT-R",
            LeftHeadHeatNumber = "HH1", LeftHeadCoilNumber = "HC1",
            RightHeadHeatNumber = "HH2", RightHeadCoilNumber = "HC2",
            TankSize = 120,
            WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        var assemblySn = await db.SerialNumbers.FirstAsync(s => s.Serial == result.AlphaCode);
        var headLogs = await db.TraceabilityLogs
            .Include(t => t.FromSerialNumber)
            .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "HeadToAssembly")
            .OrderBy(t => t.TankLocation)
            .ToListAsync();
        Assert.Equal(2, headLogs.Count);
        Assert.NotNull(headLogs[0].FromSerialNumber);
        Assert.Equal("HC1", headLogs[0].FromSerialNumber!.CoilNumber);
        Assert.Equal("HH1", headLogs[0].FromSerialNumber!.HeatNumber);
        Assert.Equal("HC2", headLogs[1].FromSerialNumber!.CoilNumber);
    }

    [Fact]
    public async Task Reassemble_UpdatesShellLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shell1 = "SHELL-R1";
        var shell2 = "SHELL-R2";
        var sn1Id = Guid.NewGuid();
        var sn2Id = Guid.NewGuid();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        db.SerialNumbers.Add(new SerialNumber { Id = sn1Id, Serial = shell1, ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.SerialNumbers.Add(new SerialNumber { Id = sn2Id, Serial = shell2, ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });

        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);
        var assemblySnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = assemblySnId,
            Serial = "BB",
            ProductId = assembledProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestHelpers.TestUserId
        });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = sn1Id,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);
        var dto = new ReassemblyDto { Shells = new List<string> { shell2 } };

        var result = await sut.ReassembleAsync("BB", dto);

        Assert.Equal("BB", result.AlphaCode);

        var shellLogs = await db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySnId && t.Relationship == "ShellToAssembly")
            .ToListAsync();
        Assert.Single(shellLogs);
        Assert.Equal(sn2Id, shellLogs[0].FromSerialNumberId);
    }
}
