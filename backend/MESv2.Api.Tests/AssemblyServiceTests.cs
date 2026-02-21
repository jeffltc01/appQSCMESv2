using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
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
        db.Assemblies.Add(new MESv2.Api.Models.Assembly
        {
            Id = Guid.NewGuid(),
            AlphaCode = "AA",
            TankSize = 100,
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
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
        db.SerialNumbers.Add(new MESv2.Api.Models.SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = shellSerial,
            ProductId = null,
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
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            WelderIds = new List<Guid>()
        };

        var result = await sut.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("AA", result.AlphaCode);

        var shellLogs = await db.TraceabilityLogs
            .Where(t => t.ToAlphaCode == "AA" && t.Relationship == "shell")
            .ToListAsync();
        Assert.Single(shellLogs);
        Assert.NotNull(shellLogs[0].FromSerialNumberId);

        var leftHead = await db.TraceabilityLogs
            .Where(t => t.ToAlphaCode == "AA" && t.Relationship == "leftHead")
            .ToListAsync();
        Assert.Single(leftHead);
        Assert.Equal("LOT-L", leftHead[0].TankLocation);

        var rightHead = await db.TraceabilityLogs
            .Where(t => t.ToAlphaCode == "AA" && t.Relationship == "rightHead")
            .ToListAsync();
        Assert.Single(rightHead);
    }

    [Fact]
    public async Task Reassemble_UpdatesShellLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shell1 = "SHELL-R1";
        var shell2 = "SHELL-R2";
        var sn1Id = Guid.NewGuid();
        var sn2Id = Guid.NewGuid();
        db.SerialNumbers.Add(new MESv2.Api.Models.SerialNumber { Id = sn1Id, Serial = shell1, CreatedAt = DateTime.UtcNow });
        db.SerialNumbers.Add(new MESv2.Api.Models.SerialNumber { Id = sn2Id, Serial = shell2, CreatedAt = DateTime.UtcNow });
        var assembly = new MESv2.Api.Models.Assembly
        {
            Id = Guid.NewGuid(),
            AlphaCode = "BB",
            TankSize = 100,
            WorkCenterId = TestHelpers.wcRollsId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        };
        db.Assemblies.Add(assembly);
        db.TraceabilityLogs.Add(new MESv2.Api.Models.TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = sn1Id,
            ToAlphaCode = "BB",
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
            .Where(t => t.ToAlphaCode == "BB" && t.Relationship == "shell")
            .ToListAsync();
        Assert.Single(shellLogs);
        Assert.Equal(sn2Id, shellLogs[0].FromSerialNumberId);
    }
}
