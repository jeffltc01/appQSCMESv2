using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class DemoDataAdminServiceTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MESv2.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public async Task ResetAndSeedAsync_SeedsDeterministicOperationalData()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var sut = new DemoDataAdminService(
            db,
            Options.Create(new DemoDataAdminOptions { Enabled = true }),
            new FakeHostEnvironment(),
            NullLogger<DemoDataAdminService>.Instance);

        var result = await sut.ResetAndSeedAsync();

        Assert.NotNull(result);
        Assert.True(db.Plants.Any());
        Assert.True(db.ProductionRecords.Any());
        Assert.True(db.InspectionRecords.Any());
        Assert.True(db.DowntimeEvents.Any());
        Assert.Contains(result.Inserted, r => r.Table == "ProductionRecords" && r.Count > 0);
    }

    [Fact]
    public async Task RefreshDatesAsync_ShiftsProductionTimelineNearNow()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var sut = new DemoDataAdminService(
            db,
            Options.Create(new DemoDataAdminOptions { Enabled = true }),
            new FakeHostEnvironment(),
            NullLogger<DemoDataAdminService>.Instance);

        await sut.ResetAndSeedAsync();
        var before = db.ProductionRecords.Max(r => r.Timestamp);

        var refresh = await sut.RefreshDatesAsync();

        var after = db.ProductionRecords.Max(r => r.Timestamp);
        Assert.True(after > before);
        Assert.InRange(Math.Abs((DateTime.UtcNow - after).TotalMinutes), 0, 5);
        Assert.NotEmpty(refresh.Updated);
    }

    [Fact]
    public async Task ResetAndSeedAsync_ThrowsWhenDisabled()
    {
        var db = TestHelpers.CreateInMemoryContext();
        var sut = new DemoDataAdminService(
            db,
            Options.Create(new DemoDataAdminOptions { Enabled = false }),
            new FakeHostEnvironment(),
            NullLogger<DemoDataAdminService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ResetAndSeedAsync());
    }
}
