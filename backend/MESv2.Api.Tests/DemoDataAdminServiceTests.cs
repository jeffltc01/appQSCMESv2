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
        Assert.True(db.ProductionRecords.Count() >= 500);
        Assert.True(db.InspectionRecords.Any());
        Assert.True(db.DowntimeEvents.Any());
        var plantIds = db.Plants.Select(p => p.Id).ToList();
        Assert.Equal(3, plantIds.Count);
        foreach (var plantId in plantIds)
        {
            var categories = db.DowntimeReasonCategories.Where(c => c.PlantId == plantId).ToList();
            Assert.Equal(6, categories.Count);
            Assert.Contains(categories, c => c.Name == "Machine");
            Assert.Contains(categories, c => c.Name == "Material");
            Assert.Contains(categories, c => c.Name == "Environment");
        }

        var reasonNames = db.DowntimeReasons.Select(r => r.Name).ToList();
        Assert.Contains("Welder Setup Delay", reasonNames);
        Assert.Contains("Equipment Down", reasonNames);

        var expectedCurrentGears = new Dictionary<string, int>
        {
            ["000"] = 4,
            ["600"] = 2,
            ["700"] = 1,
        };
        var plantsWithGear = db.Plants
            .Join(db.PlantGears, p => p.CurrentPlantGearId, pg => pg.Id, (p, pg) => new { p.Code, pg.Level })
            .ToDictionary(x => x.Code, x => x.Level);
        foreach (var kvp in expectedCurrentGears)
            Assert.True(plantsWithGear.TryGetValue(kvp.Key, out var level) && level == kvp.Value);

        var shiftSchedules = db.ShiftSchedules
            .Join(db.Plants, s => s.PlantId, p => p.Id, (s, p) => new { p.Code, s.EffectiveDate })
            .OrderBy(x => x.Code)
            .ThenBy(x => x.EffectiveDate)
            .ToList();
        Assert.Equal(3, shiftSchedules.Count);
        Assert.Contains(shiftSchedules, x => x.Code == "000" && x.EffectiveDate == new DateOnly(2026, 2, 23));
        Assert.Contains(shiftSchedules, x => x.Code == "700" && x.EffectiveDate == new DateOnly(2026, 2, 22));
        Assert.Contains(shiftSchedules, x => x.Code == "700" && x.EffectiveDate == new DateOnly(2026, 3, 1));

        var westJordanId = db.Plants.Single(p => p.Code == "700").Id;
        var mainLineId = db.ProductionLines
            .Where(l => l.PlantId == westJordanId && l.Name == "Main Line")
            .Select(l => (Guid?)l.Id)
            .FirstOrDefault();
        if (mainLineId.HasValue)
        {
            Assert.True(db.ProductionRecords.Any(r => r.ProductionLineId == mainLineId.Value));
        }
        else
        {
            var westJordanLineIds = db.ProductionLines
                .Where(l => l.PlantId == westJordanId)
                .Select(l => l.Id)
                .ToHashSet();
            Assert.True(db.ProductionRecords.Any(r => westJordanLineIds.Contains(r.ProductionLineId)));
        }
        Assert.True(db.TraceabilityLogs.Any(t => t.Relationship == "hydro-marriage"));
        Assert.True(db.TraceabilityLogs.Any(t => t.Relationship == "leftHead" && t.TankLocation == "Head 1"));
        Assert.True(db.TraceabilityLogs.Any(t => t.Relationship == "rightHead" && t.TankLocation == "Head 2"));
        Assert.True(db.TraceabilityLogs.Any(t => t.Relationship == "plate"));

        var rollsSerialIds = db.ProductionRecords
            .Join(db.WorkCenters, r => r.WorkCenterId, w => w.Id, (r, w) => new { r.SerialNumberId, w.DataEntryType })
            .Where(x => x.DataEntryType == "Rolls")
            .Select(x => x.SerialNumberId)
            .Distinct()
            .Take(10)
            .ToList();
        var rollsSerials = db.SerialNumbers
            .Where(sn => rollsSerialIds.Contains(sn.Id))
            .ToList();
        Assert.NotEmpty(rollsSerials);
        Assert.All(rollsSerials, sn =>
        {
            Assert.False(string.IsNullOrWhiteSpace(sn.HeatNumber));
            Assert.False(string.IsNullOrWhiteSpace(sn.CoilNumber));
            Assert.False(string.IsNullOrWhiteSpace(sn.LotNumber));
        });
        var plateLinks = db.TraceabilityLogs
            .Where(t => t.Relationship == "plate" && t.FromSerialNumberId.HasValue && t.ToSerialNumberId.HasValue)
            .ToList();
        Assert.NotEmpty(plateLinks);
        var sampledRollsSerialIds = rollsSerialIds.Take(10).ToList();
        foreach (var serialId in sampledRollsSerialIds)
            Assert.Contains(plateLinks, link => link.ToSerialNumberId == serialId);
        var plateProductTypeIds = db.ProductTypes
            .Where(pt => pt.SystemTypeName == "plate")
            .Select(pt => pt.Id)
            .ToHashSet();
        var plateProductIds = db.Products
            .Where(p => plateProductTypeIds.Contains(p.ProductTypeId))
            .Select(p => p.Id)
            .ToHashSet();
        var linkedPlateSerialIds = plateLinks
            .Select(link => link.FromSerialNumberId!.Value)
            .Distinct()
            .Take(20)
            .ToList();
        var linkedPlateSerials = db.SerialNumbers
            .Where(sn => linkedPlateSerialIds.Contains(sn.Id))
            .ToList();
        Assert.NotEmpty(linkedPlateSerials);
        Assert.All(linkedPlateSerials, sn =>
            Assert.True(sn.ProductId.HasValue && plateProductIds.Contains(sn.ProductId.Value)));

        var headSerialIds = db.TraceabilityLogs
            .Where(t => t.Relationship == "leftHead" || t.Relationship == "rightHead")
            .Where(t => t.FromSerialNumberId.HasValue)
            .Select(t => t.FromSerialNumberId!.Value)
            .Distinct()
            .Take(10)
            .ToList();
        var headSerials = db.SerialNumbers
            .Where(sn => headSerialIds.Contains(sn.Id))
            .ToList();
        Assert.NotEmpty(headSerials);
        Assert.All(headSerials, sn =>
        {
            Assert.False(string.IsNullOrWhiteSpace(sn.HeatNumber));
            Assert.False(string.IsNullOrWhiteSpace(sn.CoilNumber));
            Assert.False(string.IsNullOrWhiteSpace(sn.LotNumber));
        });

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
