using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class RealTimeXrayServiceTests
{
    private static readonly Guid WcRealTimeXrayId = Guid.Parse("e0000001-0000-0000-0000-000000000001");
    private static readonly Guid WcplRealTimeXrayId = Guid.Parse("e0000002-0000-0000-0000-000000000001");
    private static readonly Guid CharacteristicId = Guid.Parse("e0000003-0000-0000-0000-000000000001");
    private static readonly Guid ControlPlanId = Guid.Parse("e0000004-0000-0000-0000-000000000001");
    private static readonly Guid DefectLocationId = Guid.Parse("e0000005-0000-0000-0000-000000000001");
    private static readonly Guid WctXrayId = Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");

    private static RealTimeXrayService CreateSut(MesDbContext db) =>
        new(db, NullLogger<RealTimeXrayService>.Instance);

    /// <summary>
    /// Seeds the RealTimeXray work center, junction, control plan, characteristic,
    /// and defect location needed by most tests.
    /// </summary>
    private static void SeedXrayInfrastructure(MesDbContext db)
    {
        db.WorkCenters.Add(new WorkCenter
        {
            Id = WcRealTimeXrayId,
            Name = "Real Time X-ray",
            WorkCenterTypeId = WctXrayId,
            DataEntryType = "RealTimeXray"
        });

        db.WorkCenterProductionLines.Add(new WorkCenterProductionLine
        {
            Id = WcplRealTimeXrayId,
            WorkCenterId = WcRealTimeXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            DisplayName = "RT X-ray - Line 1"
        });

        db.Characteristics.Add(new Characteristic
        {
            Id = CharacteristicId,
            Code = "XRAY",
            Name = "X-ray Weld Integrity"
        });

        db.ControlPlans.Add(new ControlPlan
        {
            Id = ControlPlanId,
            CharacteristicId = CharacteristicId,
            WorkCenterProductionLineId = WcplRealTimeXrayId,
            IsEnabled = true,
            IsGateCheck = true,
            ResultType = "AcceptReject"
        });

        db.DefectLocations.Add(new DefectLocation
        {
            Id = DefectLocationId,
            Code = "XRAY-LOC",
            Name = "X-ray Location",
            CharacteristicId = CharacteristicId
        });

        db.SaveChanges();
    }

    /// <summary>
    /// Seeds a serial number and an initial production record (upstream) for the given serial.
    /// </summary>
    private static Guid SeedSerialAndUpstreamRecord(MesDbContext db, string serial)
    {
        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = serial,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = snId,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        db.SaveChanges();
        return snId;
    }

    private static XrayInspectionRequestDto MakeDto(
        string serial = "SN-XRAY-001",
        string siteCode = "000",
        int inspectionResult = 1,
        List<XrayDefectDto>? defects = null)
    {
        return new XrayInspectionRequestDto
        {
            SerialNumber = serial,
            SiteCode = siteCode,
            InspectionResult = inspectionResult,
            UserID = TestHelpers.TestUserId,
            IsTest = 0,
            Defects = defects ?? new List<XrayDefectDto>()
        };
    }

    // ── Happy-path tests ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessInspection_Accept_NoDefects_ReturnsSuccess()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(inspectionResult: 1));

        Assert.Equal(1, result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ProcessInspection_Accept_CreatesProductionRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var sut = CreateSut(db);
        await sut.ProcessInspectionAsync(MakeDto(inspectionResult: 1));

        var prodRecords = await db.ProductionRecords
            .Where(r => r.WorkCenterId == WcRealTimeXrayId)
            .ToListAsync();
        Assert.Single(prodRecords);

        var pr = prodRecords[0];
        Assert.Equal(TestHelpers.ProductionLine1Plt1Id, pr.ProductionLineId);
        Assert.Equal(TestHelpers.TestUserId, pr.OperatorId);
        Assert.Null(pr.AssetId);
    }

    [Fact]
    public async Task ProcessInspection_Accept_CreatesInspectionRecord_WithAcceptResult()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var sut = CreateSut(db);
        await sut.ProcessInspectionAsync(MakeDto(inspectionResult: 1));

        var ir = await db.InspectionRecords.FirstOrDefaultAsync(r => r.WorkCenterId == WcRealTimeXrayId);
        Assert.NotNull(ir);
        Assert.Equal("Accept", ir.ResultText);
        Assert.Equal(ControlPlanId, ir.ControlPlanId);
    }

    [Fact]
    public async Task ProcessInspection_Reject_CreatesInspectionRecord_WithRejectResult()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var sut = CreateSut(db);
        await sut.ProcessInspectionAsync(MakeDto(inspectionResult: 0));

        var ir = await db.InspectionRecords.FirstOrDefaultAsync(r => r.WorkCenterId == WcRealTimeXrayId);
        Assert.NotNull(ir);
        Assert.Equal("Reject", ir.ResultText);
    }

    [Fact]
    public async Task ProcessInspection_WithDefects_CreatesDefectLogs()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var defectCodeId = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = defectCodeId, Code = "X01", Name = "Porosity" });
        await db.SaveChangesAsync();

        var defects = new List<XrayDefectDto>
        {
            new()
            {
                DefectID = defectCodeId,
                LocationDetails1 = 1.5m,
                LocationDetails2 = 2.5m,
                LocationDetailsCode = "L"
            }
        };

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(inspectionResult: 0, defects: defects));

        Assert.Equal(1, result.IsSuccess);

        var defectLogs = await db.DefectLogs
            .Where(d => d.DefectCodeId == defectCodeId)
            .ToListAsync();

        Assert.Single(defectLogs);
        var dl = defectLogs[0];
        Assert.Equal(CharacteristicId, dl.CharacteristicId);
        Assert.Equal(DefectLocationId, dl.LocationId);
        Assert.Equal(1.5m, dl.LocDetails1);
        Assert.Equal(2.5m, dl.LocDetails2);
        Assert.Equal("L", dl.LocDetailsCode);
    }

    [Fact]
    public async Task ProcessInspection_MultipleDefects_CreatesAll()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var dc1 = Guid.NewGuid();
        var dc2 = Guid.NewGuid();
        db.DefectCodes.Add(new DefectCode { Id = dc1, Code = "X01", Name = "Porosity" });
        db.DefectCodes.Add(new DefectCode { Id = dc2, Code = "X02", Name = "Slag Inclusion" });
        await db.SaveChangesAsync();

        var defects = new List<XrayDefectDto>
        {
            new() { DefectID = dc1, LocationDetails1 = 1m, LocationDetails2 = 0m, LocationDetailsCode = "L" },
            new() { DefectID = dc2, LocationDetails1 = 3m, LocationDetails2 = 4m, LocationDetailsCode = "R" }
        };

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(defects: defects));

        Assert.Equal(1, result.IsSuccess);
        Assert.Equal(2, await db.DefectLogs.CountAsync(d => d.DefectCodeId == dc1 || d.DefectCodeId == dc2));
    }

    // ── Validation failure tests ────────────────────────────────────────

    [Fact]
    public async Task ProcessInspection_UnknownSiteCode_ReturnsError()
    {
        await using var db = TestHelpers.CreateInMemoryContext();

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(siteCode: "999"));

        Assert.Equal(0, result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains("999", result.Errors[0].Description);
    }

    [Fact]
    public async Task ProcessInspection_UnknownSerialNumber_ReturnsError()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(serial: "NONEXISTENT"));

        Assert.Equal(0, result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains("NONEXISTENT", result.Errors[0].Description);
    }

    [Fact]
    public async Task ProcessInspection_NoPriorProductionRecord_ReturnsError()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "SN-ORPHAN",
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(serial: "SN-ORPHAN"));

        Assert.Equal(0, result.IsSuccess);
        Assert.Contains("production record", result.Errors[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessInspection_NoRealTimeXrayWorkCenter_ReturnsError()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto());

        Assert.Equal(0, result.IsSuccess);
        Assert.Contains("RealTimeXray", result.Errors[0].Description);
    }

    [Fact]
    public async Task ProcessInspection_NoControlPlan_ReturnsError()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        db.WorkCenters.Add(new WorkCenter
        {
            Id = WcRealTimeXrayId,
            Name = "Real Time X-ray",
            WorkCenterTypeId = WctXrayId,
            DataEntryType = "RealTimeXray"
        });
        db.WorkCenterProductionLines.Add(new WorkCenterProductionLine
        {
            Id = WcplRealTimeXrayId,
            WorkCenterId = WcRealTimeXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            DisplayName = "RT X-ray - Line 1"
        });
        db.SaveChanges();

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto());

        Assert.Equal(0, result.IsSuccess);
        Assert.Contains("control plan", result.Errors[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessInspection_NoDefectLocationForCharacteristic_ReturnsError()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        db.WorkCenters.Add(new WorkCenter
        {
            Id = WcRealTimeXrayId,
            Name = "Real Time X-ray",
            WorkCenterTypeId = WctXrayId,
            DataEntryType = "RealTimeXray"
        });
        db.WorkCenterProductionLines.Add(new WorkCenterProductionLine
        {
            Id = WcplRealTimeXrayId,
            WorkCenterId = WcRealTimeXrayId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            DisplayName = "RT X-ray - Line 1"
        });
        var charId = Guid.NewGuid();
        db.Characteristics.Add(new Characteristic { Id = charId, Code = "XRAY", Name = "X-ray" });
        db.ControlPlans.Add(new ControlPlan
        {
            Id = Guid.NewGuid(),
            CharacteristicId = charId,
            WorkCenterProductionLineId = WcplRealTimeXrayId,
            IsEnabled = true,
            IsGateCheck = true,
            ResultType = "AcceptReject"
        });
        db.SaveChanges();

        var defects = new List<XrayDefectDto>
        {
            new() { DefectID = Guid.NewGuid(), LocationDetails1 = 0, LocationDetails2 = 0, LocationDetailsCode = "L" }
        };

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(MakeDto(defects: defects));

        Assert.Equal(0, result.IsSuccess);
        Assert.Contains("defect location", result.Errors[0].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessInspection_IsTestIgnored_StillSaves()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var dto = MakeDto();
        dto.IsTest = 1;

        var sut = CreateSut(db);
        var result = await sut.ProcessInspectionAsync(dto);

        Assert.Equal(1, result.IsSuccess);

        var prodRecord = await db.ProductionRecords
            .FirstOrDefaultAsync(r => r.WorkCenterId == WcRealTimeXrayId);
        Assert.NotNull(prodRecord);
    }

    [Fact]
    public async Task ProcessInspection_PlantGearId_CopiedFromPlant()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedXrayInfrastructure(db);
        SeedSerialAndUpstreamRecord(db, "SN-XRAY-001");

        var gearId = Guid.NewGuid();
        db.PlantGears.Add(new PlantGear { Id = gearId, PlantId = TestHelpers.PlantPlt1Id, Name = "Gear1" });
        var plant = await db.Plants.FindAsync(TestHelpers.PlantPlt1Id);
        plant!.CurrentPlantGearId = gearId;
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.ProcessInspectionAsync(MakeDto());

        var pr = await db.ProductionRecords.FirstAsync(r => r.WorkCenterId == WcRealTimeXrayId);
        Assert.Equal(gearId, pr.PlantGearId);
    }
}
