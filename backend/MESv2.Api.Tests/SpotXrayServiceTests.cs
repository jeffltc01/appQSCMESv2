using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class SpotXrayServiceTests
{
    private static readonly Guid RsAssetLane1Id = Guid.Parse("a1000001-0000-0000-0000-000000000001");
    private static readonly Guid RsAssetLane2Id = Guid.Parse("a1000001-0000-0000-0000-000000000002");
    private static readonly Guid WelderJeffId = Guid.Parse("b1000001-0000-0000-0000-000000000001");
    private static readonly Guid WelderJoeId = Guid.Parse("b1000001-0000-0000-0000-000000000002");
    private static readonly Guid ProductType500Id = Guid.Parse("c1000001-0000-0000-0000-000000000001");
    private static readonly Guid Product500Id = Guid.Parse("c2000001-0000-0000-0000-000000000001");
    private static readonly Guid CharRs1Id = Guid.Parse("d1000001-0000-0000-0000-000000000001");
    private static readonly Guid CharRs2Id = Guid.Parse("d1000001-0000-0000-0000-000000000002");

    private async Task<Data.MesDbContext> CreateSeededContext()
    {
        var db = TestHelpers.CreateInMemoryContext();

        // Lane-named assets for RS
        db.Assets.Add(new Asset { Id = RsAssetLane1Id, Name = "RS Lane 1", WorkCenterId = TestHelpers.wcRoundSeamId, ProductionLineId = TestHelpers.ProductionLine1Plt1Id, LaneName = "Lane 1" });
        db.Assets.Add(new Asset { Id = RsAssetLane2Id, Name = "RS Lane 2", WorkCenterId = TestHelpers.wcRoundSeamId, ProductionLineId = TestHelpers.ProductionLine1Plt1Id, LaneName = "Lane 2" });

        // Welders
        db.Users.Add(new User { Id = WelderJeffId, EmployeeNumber = "JEFF01", DisplayName = "Jeff", RoleTier = 1, RoleName = "Operator", DefaultSiteId = TestHelpers.PlantPlt1Id, UserType = UserType.Standard, IsCertifiedWelder = true });
        db.Users.Add(new User { Id = WelderJoeId, EmployeeNumber = "JOE01", DisplayName = "Joe", RoleTier = 1, RoleName = "Operator", DefaultSiteId = TestHelpers.PlantPlt1Id, UserType = UserType.Standard, IsCertifiedWelder = true });

        // Product type and 500gal product
        db.ProductTypes.Add(new ProductType { Id = ProductType500Id, Name = "Assembled", SystemTypeName = "assembled" });
        db.Products.Add(new Product { Id = Product500Id, ProductNumber = "500-GAL", TankSize = 500, TankType = "OX", ProductTypeId = ProductType500Id });

        // Characteristics for welding
        db.Characteristics.Add(new Characteristic { Id = CharRs1Id, Code = "RS1", Name = "RS1", ProductTypeId = ProductType500Id });
        db.Characteristics.Add(new Characteristic { Id = CharRs2Id, Code = "RS2", Name = "RS2", ProductTypeId = ProductType500Id });

        await db.SaveChangesAsync();
        return db;
    }

    private async Task<(Guid ShellId, Guid AssemblyId, Guid RecordId)> SeedTankAtRoundSeam(
        Data.MesDbContext db, string shellSerial, string alphaCode, Guid assetId, DateTime timestamp, Guid welderId)
    {
        var shellSn = new SerialNumber { Id = Guid.NewGuid(), Serial = shellSerial, PlantId = TestHelpers.PlantPlt1Id, ProductId = Product500Id, CreatedAt = timestamp };
        var assemblySn = new SerialNumber { Id = Guid.NewGuid(), Serial = alphaCode, PlantId = TestHelpers.PlantPlt1Id, ProductId = Product500Id, CreatedAt = timestamp };
        db.SerialNumbers.Add(shellSn);
        db.SerialNumbers.Add(assemblySn);

        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(), FromSerialNumberId = shellSn.Id, ToSerialNumberId = assemblySn.Id,
            Relationship = "ShellToAssembly", Timestamp = timestamp
        });

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(), SerialNumberId = shellSn.Id, WorkCenterId = TestHelpers.wcRoundSeamId,
            AssetId = assetId, ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId, Timestamp = timestamp
        };
        db.ProductionRecords.Add(record);

        db.WelderLogs.Add(new WelderLog { Id = Guid.NewGuid(), ProductionRecordId = record.Id, UserId = welderId, CharacteristicId = CharRs1Id });
        db.WelderLogs.Add(new WelderLog { Id = Guid.NewGuid(), ProductionRecordId = record.Id, UserId = welderId, CharacteristicId = CharRs2Id });

        await db.SaveChangesAsync();
        return (shellSn.Id, assemblySn.Id, record.Id);
    }

    [Fact]
    public async Task GetLaneQueues_ReturnsLanesWithTanks()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        await SeedTankAtRoundSeam(db, "SH-001", "ALPHA-001", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-002", "ALPHA-002", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-003", "ALPHA-003", RsAssetLane2Id, now.AddMinutes(-8), WelderJoeId);

        var result = await svc.GetLaneQueuesAsync("000");

        Assert.Equal(2, result.Lanes.Count);
        var lane1 = result.Lanes.First(l => l.LaneName == "Lane 1");
        Assert.Equal(2, lane1.Tanks.Count);
        Assert.Equal("ALPHA-001", lane1.Tanks[0].AlphaCode);
        Assert.Equal("ALPHA-002", lane1.Tanks[1].AlphaCode);
    }

    [Fact]
    public async Task CreateIncrements_ValidSelection_Succeeds()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        await SeedTankAtRoundSeam(db, "SH-101", "ALPHA-101", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-102", "ALPHA-102", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var result = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new List<LaneSelectionDto>
            {
                new() { LaneName = "Lane 1", SelectedPositions = new List<int> { 1, 2 } }
            }
        });

        Assert.Single(result.Increments);
        Assert.Equal("Pending", result.Increments[0].OverallStatus);
        Assert.Equal(500, result.Increments[0].TankSize);
    }

    [Fact]
    public async Task CreateIncrements_NonSequential_Throws()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        await SeedTankAtRoundSeam(db, "SH-201", "ALPHA-201", RsAssetLane1Id, now.AddMinutes(-15), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-202", "ALPHA-202", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-203", "ALPHA-203", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
            {
                WorkCenterId = TestHelpers.wcSpotXrayId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                SiteCode = "000",
                LaneSelections = new List<LaneSelectionDto>
                {
                    new() { LaneName = "Lane 1", SelectedPositions = new List<int> { 1, 3 } }
                }
            }));
    }

    [Fact]
    public async Task GetNextShotNumber_IncrementsDaily()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var r1 = await svc.GetNextShotNumberAsync(TestHelpers.PlantPlt1Id);
        Assert.Equal(1, r1.ShotNumber);

        var r2 = await svc.GetNextShotNumberAsync(TestHelpers.PlantPlt1Id);
        Assert.Equal(2, r2.ShotNumber);
    }

    [Fact]
    public async Task SaveResults_Draft_PreservesStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-301", "ALPHA-301", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        var (_, asmId2, _) = await SeedTankAtRoundSeam(db, "SH-302", "ALPHA-302", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var incId = created.Increments[0].Id;

        var saved = await svc.SaveResultsAsync(incId, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = true,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Accept" },
                new() { SeamNumber = 2, ShotNo = "2", Result = "Accept" }
            }
        });

        Assert.True(saved.IsDraft);
        Assert.Equal("Pending", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_AllAccept_SetsAcceptStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-401", "ALPHA-401", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-402", "ALPHA-402", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Accept" },
                new() { SeamNumber = 2, ShotNo = "2", Result = "Accept" }
            }
        });

        Assert.False(saved.IsDraft);
        Assert.Equal("Accept", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_TraceReject_SetsRejectStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-501", "ALPHA-501", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-502", "ALPHA-502", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject", Trace1ShotNo = "2", Trace1Result = "Reject" },
                new() { SeamNumber = 2, ShotNo = "3", Result = "Accept" }
            }
        });

        Assert.Equal("Reject", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_FinalReject_SetsAcceptScrapStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-601", "ALPHA-601", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-602", "ALPHA-602", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject", Trace1ShotNo = "2", Trace1Result = "Accept", Trace2ShotNo = "3", Trace2Result = "Accept", FinalShotNo = "4", FinalResult = "Reject" },
                new() { SeamNumber = 2, ShotNo = "5", Result = "Accept" }
            }
        });

        Assert.Equal("Accept-Scrap", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_AllSeamsEmpty_SetsPendingStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-701", "ALPHA-701", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-702", "ALPHA-702", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1 },
                new() { SeamNumber = 2 }
            }
        });

        Assert.Equal("Pending", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_RejectWithEmptyTrace1_SetsRejectStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-801", "ALPHA-801", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-802", "ALPHA-802", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject" },
                new() { SeamNumber = 2, ShotNo = "2", Result = "Accept" }
            }
        });

        Assert.Equal("Reject", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_Trace1AcceptTrace2Empty_SetsPendingStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-901", "ALPHA-901", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-902", "ALPHA-902", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject", Trace1ShotNo = "2", Trace1Result = "Accept" },
                new() { SeamNumber = 2, ShotNo = "3", Result = "Accept" }
            }
        });

        Assert.Equal("Pending", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_Trace2Reject_SetsRejectStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-A01", "ALPHA-A01", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-A02", "ALPHA-A02", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject", Trace1ShotNo = "2", Trace1Result = "Accept", Trace2ShotNo = "3", Trace2Result = "Reject" },
                new() { SeamNumber = 2, ShotNo = "4", Result = "Accept" }
            }
        });

        Assert.Equal("Reject", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_FinalEmpty_SetsPendingStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-B01", "ALPHA-B01", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-B02", "ALPHA-B02", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject", Trace1ShotNo = "2", Trace1Result = "Accept", Trace2ShotNo = "3", Trace2Result = "Accept" },
                new() { SeamNumber = 2, ShotNo = "4", Result = "Accept" }
            }
        });

        Assert.Equal("Pending", saved.OverallStatus);
    }

    [Fact]
    public async Task SaveResults_FinalAccept_SetsAcceptStatus()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        var (_, asmId, _) = await SeedTankAtRoundSeam(db, "SH-C01", "ALPHA-C01", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-C02", "ALPHA-C02", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        var created = await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var saved = await svc.SaveResultsAsync(created.Increments[0].Id, new SaveSpotXrayResultsRequest
        {
            InspectTankId = asmId,
            IsDraft = false,
            OperatorId = TestHelpers.TestUserId,
            Seams = new()
            {
                new() { SeamNumber = 1, ShotNo = "1", Result = "Reject", Trace1ShotNo = "2", Trace1Result = "Accept", Trace2ShotNo = "3", Trace2Result = "Accept", FinalShotNo = "4", FinalResult = "Accept" },
                new() { SeamNumber = 2, ShotNo = "5", Result = "Accept" }
            }
        });

        Assert.Equal("Accept", saved.OverallStatus);
    }

    [Fact]
    public async Task GetLaneQueues_NoRoundSeamRecords_ReturnsEmpty()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var result = await svc.GetLaneQueuesAsync("000");

        Assert.Empty(result.Lanes);
    }

    [Fact]
    public async Task GetLaneQueues_DuplicateShellTraceability_UsesLatestAssemblyMapping()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);
        var now = DateTime.UtcNow;

        var (shellId, _, _) = await SeedTankAtRoundSeam(
            db, "SH-DUP-01", "ALPHA-OLD", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);

        var latestAssembly = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "ALPHA-NEW",
            PlantId = TestHelpers.PlantPlt1Id,
            ProductId = Product500Id,
            CreatedAt = now.AddMinutes(-2)
        };
        db.SerialNumbers.Add(latestAssembly);
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shellId,
            ToSerialNumberId = latestAssembly.Id,
            Relationship = "ShellToAssembly",
            Timestamp = now.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var result = await svc.GetLaneQueuesAsync(TestHelpers.PlantPlt1Id, null);

        var lane1 = result.Lanes.First(l => l.LaneName == "Lane 1");
        Assert.Contains(lane1.Tanks, t => t.AlphaCode == "ALPHA-NEW");
    }

    [Fact]
    public async Task GetRecentIncrements_ReturnsCreatedIncrements()
    {
        await using var db = await CreateSeededContext();
        var svc = new SpotXrayService(db);

        var now = DateTime.UtcNow;
        await SeedTankAtRoundSeam(db, "SH-D01", "ALPHA-D01", RsAssetLane1Id, now.AddMinutes(-10), WelderJeffId);
        await SeedTankAtRoundSeam(db, "SH-D02", "ALPHA-D02", RsAssetLane1Id, now.AddMinutes(-5), WelderJeffId);

        await svc.CreateIncrementsAsync(new CreateSpotXrayIncrementsRequest
        {
            WorkCenterId = TestHelpers.wcSpotXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            SiteCode = "000",
            LaneSelections = new() { new() { LaneName = "Lane 1", SelectedPositions = new() { 1, 2 } } }
        });

        var recent = await svc.GetRecentIncrementsAsync("000");

        Assert.Single(recent);
        Assert.True(recent[0].IsDraft);
    }
}
