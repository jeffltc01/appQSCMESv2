using Microsoft.EntityFrameworkCore;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class DemoShellFlowServiceTests
{
    [Fact]
    public async Task GetCurrent_Rolls_AutoCreatesFirstShell()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var caller = await db.Users.FirstAsync(u => u.Id == TestHelpers.TestUserId);
        caller.RoleTier = 1.0m;
        caller.DemoMode = true;
        await db.SaveChangesAsync();

        var sut = new DemoShellFlowService(db);
        var current = await sut.GetCurrentAsync(TestHelpers.wcRollsId, caller.Id);

        Assert.True(current.HasCurrent);
        Assert.Equal("Rolls", current.Stage);
        Assert.Equal("000001", current.SerialNumber);
        Assert.Equal("SC;000001", current.BarcodeRaw);
    }

    [Fact]
    public async Task Advance_Rolls_MovesCurrentToLongSeam_AndCreatesNextRollsShell()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var caller = await db.Users.FirstAsync(u => u.Id == TestHelpers.TestUserId);
        caller.RoleTier = 1.0m;
        caller.DemoMode = true;
        await db.SaveChangesAsync();

        var sut = new DemoShellFlowService(db);
        _ = await sut.GetCurrentAsync(TestHelpers.wcRollsId, caller.Id);
        var nextRolls = await sut.AdvanceAsync(TestHelpers.wcRollsId, caller.Id);

        Assert.Equal("000002", nextRolls.SerialNumber);
        var longSeamQueue = await db.DemoShellFlows
            .Where(x => x.CurrentStage == DemoShellStage.LongSeam)
            .OrderBy(x => x.ShellNumber)
            .ToListAsync();
        Assert.Single(longSeamQueue);
        Assert.Equal("000001", longSeamQueue[0].SerialNumber);
    }

    [Fact]
    public async Task Advance_LongSeam_MovesNextItemToLongSeamInspection()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var caller = await db.Users.FirstAsync(u => u.Id == TestHelpers.TestUserId);
        caller.RoleTier = 1.0m;
        caller.DemoMode = true;
        await db.SaveChangesAsync();

        var sut = new DemoShellFlowService(db);
        _ = await sut.GetCurrentAsync(TestHelpers.wcRollsId, caller.Id);
        _ = await sut.AdvanceAsync(TestHelpers.wcRollsId, caller.Id);

        var result = await sut.AdvanceAsync(TestHelpers.wcLongSeamId, caller.Id);
        Assert.False(result.HasCurrent);

        var inspQueue = await db.DemoShellFlows
            .Where(x => x.CurrentStage == DemoShellStage.LongSeamInspection)
            .OrderBy(x => x.ShellNumber)
            .ToListAsync();

        Assert.Single(inspQueue);
        Assert.Equal("000001", inspQueue[0].SerialNumber);
    }
}
