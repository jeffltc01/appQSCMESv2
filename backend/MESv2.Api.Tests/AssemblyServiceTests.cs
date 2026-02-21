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
            .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "shell")
            .ToListAsync();
        Assert.Single(shellLogs);
        Assert.NotNull(shellLogs[0].FromSerialNumberId);

        var leftHead = await db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "leftHead")
            .ToListAsync();
        Assert.Single(leftHead);
        Assert.Equal("LOT-L", leftHead[0].TankLocation);

        var rightHead = await db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "rightHead")
            .ToListAsync();
        Assert.Single(rightHead);

        var prodRecord = await db.ProductionRecords.FirstOrDefaultAsync(r => r.SerialNumberId == assemblySn.Id);
        Assert.NotNull(prodRecord);
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
            .Where(t => t.ToSerialNumberId == assemblySnId && t.Relationship == "shell")
            .ToListAsync();
        Assert.Single(shellLogs);
        Assert.Equal(sn2Id, shellLogs[0].FromSerialNumberId);
    }
}
