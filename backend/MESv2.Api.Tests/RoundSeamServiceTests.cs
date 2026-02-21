using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class RoundSeamServiceTests
{
    [Fact]
    public async Task SaveSetup_CreatesSetup()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new RoundSeamService(db);

        var result = await sut.SaveSetupAsync(TestHelpers.wcRoundSeamId, new CreateRoundSeamSetupDto
        {
            TankSize = 500,
            Rs1WelderId = TestHelpers.TestUserId,
            Rs2WelderId = TestHelpers.TestUserId,
        });

        Assert.NotNull(result);
        Assert.Equal(500, result.TankSize);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public async Task GetSetup_NoSetup_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new RoundSeamService(db);

        var result = await sut.GetSetupAsync(TestHelpers.wcRoundSeamId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSetup_ExistingSetup_ReturnsIt()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.RoundSeamSetups.Add(new RoundSeamSetup
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRoundSeamId,
            TankSize = 1000,
            Rs1WelderId = TestHelpers.TestUserId,
            Rs2WelderId = TestHelpers.TestUserId,
            Rs3WelderId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new RoundSeamService(db);
        var result = await sut.GetSetupAsync(TestHelpers.wcRoundSeamId);

        Assert.NotNull(result);
        Assert.Equal(1000, result.TankSize);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public async Task GetAssemblyByShell_NoShell_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new RoundSeamService(db);

        var result = await sut.GetAssemblyByShellAsync("UNKNOWN");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssemblyByShell_ReturnsShellSerials()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sn1Id = Guid.NewGuid();
        var sn2Id = Guid.NewGuid();
        var assemblySnId = Guid.NewGuid();
        var shellProduct = db.Products.First(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 1000);
        var assembledProduct = db.Products.First(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 1000);

        db.SerialNumbers.Add(new SerialNumber { Id = sn1Id, Serial = "SH-001", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.SerialNumbers.Add(new SerialNumber { Id = sn2Id, Serial = "SH-002", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.SerialNumbers.Add(new SerialNumber { Id = assemblySnId, Serial = "AA", ProductId = assembledProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = sn1Id,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = DateTime.UtcNow
        });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = sn2Id,
            ToSerialNumberId = assemblySnId,
            Relationship = "shell",
            Quantity = 1,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new RoundSeamService(db);
        var result = await sut.GetAssemblyByShellAsync("SH-001");

        Assert.NotNull(result);
        Assert.Equal("AA", result.AlphaCode);
        Assert.NotNull(result.Shells);
        Assert.Equal(2, result.Shells.Count);
        Assert.Contains("SH-001", result.Shells);
        Assert.Contains("SH-002", result.Shells);
    }

    [Fact]
    public async Task SaveSetup_IncompleteForLargeSize_IsNotComplete()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new RoundSeamService(db);

        var result = await sut.SaveSetupAsync(TestHelpers.wcRoundSeamId, new CreateRoundSeamSetupDto
        {
            TankSize = 1500,
            Rs1WelderId = TestHelpers.TestUserId,
            Rs2WelderId = TestHelpers.TestUserId,
        });

        Assert.False(result.IsComplete);
    }
}
