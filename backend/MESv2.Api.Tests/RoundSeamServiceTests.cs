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

        var result = await sut.SaveSetupAsync(TestHelpers.wcRollsId, new CreateRoundSeamSetupDto
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

        var result = await sut.GetSetupAsync(TestHelpers.wcRollsId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSetup_ExistingSetup_ReturnsIt()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.RoundSeamSetups.Add(new RoundSeamSetup
        {
            Id = Guid.NewGuid(),
            WorkCenterId = TestHelpers.wcRollsId,
            TankSize = 1000,
            Rs1WelderId = TestHelpers.TestUserId,
            Rs2WelderId = TestHelpers.TestUserId,
            Rs3WelderId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new RoundSeamService(db);
        var result = await sut.GetSetupAsync(TestHelpers.wcRollsId);

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
    public async Task SaveSetup_IncompleteForLargeSize_IsNotComplete()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new RoundSeamService(db);

        var result = await sut.SaveSetupAsync(TestHelpers.wcRollsId, new CreateRoundSeamSetupDto
        {
            TankSize = 1500,
            Rs1WelderId = TestHelpers.TestUserId,
            Rs2WelderId = TestHelpers.TestUserId,
        });

        Assert.False(result.IsComplete);
    }
}
