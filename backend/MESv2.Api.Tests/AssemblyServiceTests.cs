using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AssemblyServiceTests
{
    [Fact]
    public async Task GetNextAlphaCode_ReturnsPlantNextTankAlphaCode()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var plant = await db.Plants.FirstAsync(p => p.Id == TestHelpers.PlantPlt1Id);
        plant.NextTankAlphaCode = "GT";
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);

        var result = await sut.GetNextAlphaCodeAsync(TestHelpers.PlantPlt1Id);
        await db.SaveChangesAsync();

        Assert.Equal("GT", result);
        Assert.Equal("GU", plant.NextTankAlphaCode);
    }

    [Fact]
    public async Task GetNextAlphaCode_WrapsZZToAA()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var plant = await db.Plants.FirstAsync(p => p.Id == TestHelpers.PlantPlt1Id);
        plant.NextTankAlphaCode = "ZZ";
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);

        var result = await sut.GetNextAlphaCodeAsync(TestHelpers.PlantPlt1Id);
        await db.SaveChangesAsync();

        Assert.Equal("ZZ", result);
        Assert.Equal("AA", plant.NextTankAlphaCode);
    }

    [Fact]
    public void AdvanceAlphaCode_SequentialCases()
    {
        Assert.Equal("AB", AssemblyService.AdvanceAlphaCode("AA"));
        Assert.Equal("BA", AssemblyService.AdvanceAlphaCode("AZ"));
        Assert.Equal("AA", AssemblyService.AdvanceAlphaCode("ZZ"));
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

        var plant = await db.Plants.FirstAsync(p => p.Id == TestHelpers.PlantPlt1Id);
        Assert.Equal("AB", plant.NextTankAlphaCode);

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
    public async Task Reassemble_Replace_CreatesNewAssembly_AndObsoletesSource()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);

        var shellOld = new SerialNumber { Id = Guid.NewGuid(), Serial = "SHELL-OLD", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var shellNew = new SerialNumber { Id = Guid.NewGuid(), Serial = "SHELL-NEW", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var oldAssembly = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "BB",
            ProductId = assembledProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestHelpers.TestUserId
        };

        db.SerialNumbers.AddRange(shellOld, shellNew, oldAssembly);
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shellOld.Id,
            ToSerialNumberId = oldAssembly.Id,
            Relationship = "ShellToAssembly",
            TankLocation = "Shell 1",
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);
        var result = await sut.ReassembleAsync("BB", new ReassemblyDto
        {
            OperationType = "replace",
            WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            PrimaryAssembly = new ReassemblyAssemblyDto
            {
                TankSize = 120,
                Shells = new List<string> { "SHELL-NEW" }
            }
        });

        Assert.Single(result.CreatedAssemblies);
        var created = result.CreatedAssemblies[0];
        Assert.NotEqual("BB", created.AlphaCode);

        var source = await db.SerialNumbers.FirstAsync(s => s.Id == oldAssembly.Id);
        Assert.True(source.IsObsolete);
        Assert.Equal(created.Id, source.ReplaceBySNId);

        var lineage = await db.TraceabilityLogs
            .Where(t => t.FromSerialNumberId == oldAssembly.Id && t.Relationship == "ReassembledTo")
            .ToListAsync();
        Assert.Single(lineage);
        Assert.Equal(created.Id, lineage[0].ToSerialNumberId);
    }

    [Fact]
    public async Task Reassemble_Split_CreatesTwoAssemblies_AndLineageToBoth()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);

        var shell1 = new SerialNumber { Id = Guid.NewGuid(), Serial = "SPLIT-S1", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var shell2 = new SerialNumber { Id = Guid.NewGuid(), Serial = "SPLIT-S2", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow };
        var oldAssembly = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "CC",
            ProductId = assembledProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestHelpers.TestUserId
        };
        db.SerialNumbers.AddRange(shell1, shell2, oldAssembly);
        db.TraceabilityLogs.AddRange(
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shell1.Id,
                ToSerialNumberId = oldAssembly.Id,
                Relationship = "ShellToAssembly",
                TankLocation = "Shell 1",
                Timestamp = DateTime.UtcNow
            },
            new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shell2.Id,
                ToSerialNumberId = oldAssembly.Id,
                Relationship = "ShellToAssembly",
                TankLocation = "Shell 2",
                Timestamp = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var sut = new AssemblyService(db);
        var result = await sut.ReassembleAsync("CC", new ReassemblyDto
        {
            OperationType = "split",
            WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            PrimaryAssembly = new ReassemblyAssemblyDto
            {
                TankSize = 120,
                Shells = new List<string> { "SPLIT-S1" }
            },
            SecondaryAssembly = new ReassemblyAssemblyDto
            {
                TankSize = 120,
                Shells = new List<string> { "SPLIT-S2" }
            }
        });

        Assert.Equal(2, result.CreatedAssemblies.Count);

        var source = await db.SerialNumbers.FirstAsync(s => s.Id == oldAssembly.Id);
        Assert.True(source.IsObsolete);
        Assert.NotNull(source.ReplaceBySNId);

        var lineage = await db.TraceabilityLogs
            .Where(t => t.FromSerialNumberId == oldAssembly.Id && t.Relationship == "ReassembledTo")
            .ToListAsync();
        Assert.Equal(2, lineage.Count);
    }
}
