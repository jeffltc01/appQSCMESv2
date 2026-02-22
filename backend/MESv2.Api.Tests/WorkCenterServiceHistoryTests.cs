using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MESv2.Api.Data;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class WorkCenterServiceHistoryTests
{
    private static WorkCenterService CreateSut(MesDbContext db) =>
        new(db, Mock.Of<ILogger<WorkCenterService>>());

    /// <summary>
    /// For a non-fitup work center (e.g. Round Seam), when a shell has a
    /// traceability log pointing to an assembly alpha code, the history entry
    /// should read "AA (20301)" — assembly alpha code wrapping the shell serial.
    /// </summary>
    [Fact]
    public async Task NonFitup_ShowsAlphaCodeWrappingShellSerial()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);

        var shellSnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = shellSnId,
            Serial = "20301",
            ProductId = shellProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });

        var assemblySnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = assemblySnId,
            Serial = "AA",
            ProductId = assembledProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });

        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shellSnId,
            ToSerialNumberId = assemblySnId,
            Relationship = "ShellToAssembly",
            Quantity = 1,
            Timestamp = DateTime.UtcNow
        });

        var recordId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = recordId,
            SerialNumberId = shellSnId,
            WorkCenterId = TestHelpers.wcRoundSeamId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRoundSeamId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        Assert.Equal("AA (20301)", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Equal(120, result.RecentRecords[0].TankSize);
    }

    /// <summary>
    /// For a fitup work center, the history entry should read
    /// "AA (SHELL-001, SHELL-002)" — alpha code with shells listed inside.
    /// </summary>
    [Fact]
    public async Task Fitup_ShowsAlphaCodeWithShellList()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 250);

        var assemblySnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = assemblySnId,
            Serial = "BB",
            ProductId = assembledProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });

        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 250);
        var shell1Id = Guid.NewGuid();
        var shell2Id = Guid.NewGuid();
        db.SerialNumbers.AddRange(
            new SerialNumber { Id = shell1Id, Serial = "SHELL-001", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow },
            new SerialNumber { Id = shell2Id, Serial = "SHELL-002", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow }
        );

        db.TraceabilityLogs.AddRange(
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = shell1Id, ToSerialNumberId = assemblySnId, Relationship = "ShellToAssembly", Quantity = 1, Timestamp = DateTime.UtcNow },
            new TraceabilityLog { Id = Guid.NewGuid(), FromSerialNumberId = shell2Id, ToSerialNumberId = assemblySnId, Relationship = "ShellToAssembly", Quantity = 1, Timestamp = DateTime.UtcNow }
        );

        var recordId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = recordId,
            SerialNumberId = assemblySnId,
            WorkCenterId = TestHelpers.wcFitupId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcFitupId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        var entry = result.RecentRecords[0];
        Assert.StartsWith("BB (", entry.SerialOrIdentifier);
        Assert.Contains("SHELL-001", entry.SerialOrIdentifier);
        Assert.Contains("SHELL-002", entry.SerialOrIdentifier);
        Assert.Equal(250, entry.TankSize);
    }

    /// <summary>
    /// When no traceability data exists, the serial string is displayed as-is.
    /// </summary>
    [Fact]
    public async Task NoTraceability_FallsBackToSerialString()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "SH-99999",
            ProductId = shellProduct.Id,
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
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.Equal("SH-99999", result.RecentRecords[0].SerialOrIdentifier);
    }

    [Fact]
    public async Task DayCount_OnlyCountsRecordsFromToday()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        var snToday = Guid.NewGuid();
        var snYesterday = Guid.NewGuid();
        db.SerialNumbers.AddRange(
            new SerialNumber { Id = snToday, Serial = "TODAY-1", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow },
            new SerialNumber { Id = snYesterday, Serial = "YEST-1", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow }
        );

        db.ProductionRecords.AddRange(
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = snToday,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            },
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = snYesterday,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow.AddDays(-1)
            }
        );
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Equal(2, result.RecentRecords.Count);
    }

    [Fact]
    public async Task AnnotationColor_IncludedInHistory()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "ANNOT-001",
            ProductId = shellProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });

        var recordId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = recordId,
            SerialNumberId = snId,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });

        var annotationTypeId = Guid.Parse("a1000003-0000-0000-0000-000000000003"); // Defect - red
        db.Annotations.Add(new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = recordId,
            AnnotationTypeId = annotationTypeId,
            InitiatedByUserId = TestHelpers.TestUserId,
            CreatedAt = DateTime.UtcNow,
            Notes = "Test defect"
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.True(result.RecentRecords[0].HasAnnotation);
        Assert.Equal("#ff0000", result.RecentRecords[0].AnnotationColor);
    }

    [Fact]
    public async Task NoAnnotation_HasAnnotationIsFalse()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "CLEAN-001",
            ProductId = shellProduct.Id,
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
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.False(result.RecentRecords[0].HasAnnotation);
        Assert.Null(result.RecentRecords[0].AnnotationColor);
    }

    /// <summary>
    /// When production records are empty, the service falls through to
    /// the inspection records path and returns those entries instead.
    /// </summary>
    [Fact]
    public async Task InspectionRecords_ReturnedWhenNoProductionRecords()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "INSP-001",
            ProductId = shellProduct.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });

        var prodRecordId = Guid.NewGuid();
        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = prodRecordId,
            SerialNumberId = snId,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });

        var controlPlan = new ControlPlan
        {
            Id = Guid.NewGuid(),
            CharacteristicId = Guid.Parse("c1000001-0000-0000-0000-000000000001"),
            WorkCenterProductionLineId = TestHelpers.wcplLongSeamInspId,
            ResultType = "PassFail",
            IsEnabled = true,
            IsActive = true
        };
        db.ControlPlans.Add(controlPlan);

        db.InspectionRecords.Add(new InspectionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = snId,
            ProductionRecordId = prodRecordId,
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow,
            ControlPlanId = controlPlan.Id,
            ResultText = "Pass"
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcLongSeamInspId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        Assert.Equal("INSP-001", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Equal(120, result.RecentRecords[0].TankSize);
    }

    [Fact]
    public async Task Limit_RestrictsRecentRecordsCount()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        for (var i = 0; i < 5; i++)
        {
            var snId = Guid.NewGuid();
            db.SerialNumbers.Add(new SerialNumber
            {
                Id = snId,
                Serial = $"LIM-{i:000}",
                ProductId = shellProduct.Id,
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
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            date: null, limit: 3);

        Assert.Equal(5, result.DayCount);
        Assert.Equal(3, result.RecentRecords.Count);
    }

    [Fact]
    public async Task AssetFilter_ReturnsOnlyRecordsForThatAsset()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);

        var sn1 = Guid.NewGuid();
        var sn2 = Guid.NewGuid();
        db.SerialNumbers.AddRange(
            new SerialNumber { Id = sn1, Serial = "ASSET-A", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow },
            new SerialNumber { Id = sn2, Serial = "ASSET-B", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow }
        );

        db.ProductionRecords.AddRange(
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = sn1,
                WorkCenterId = TestHelpers.wcRollsId,
                AssetId = TestHelpers.TestAssetId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            },
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = sn2,
                WorkCenterId = TestHelpers.wcRollsId,
                AssetId = Guid.NewGuid(),
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            date: null, limit: 10, assetId: TestHelpers.TestAssetId);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        Assert.Equal("ASSET-A", result.RecentRecords[0].SerialOrIdentifier);
    }
}
