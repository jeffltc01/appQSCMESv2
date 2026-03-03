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
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        Assert.Equal("AA (20301)", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Equal(120, result.RecentRecords[0].TankSize);
    }

    /// <summary>
    /// Rolls history must always show the shell serial, even if that shell
    /// is later linked to an assembly alpha code downstream.
    /// </summary>
    [Fact]
    public async Task Rolls_ShowsShellSerialEvenWhenAssemblyExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 500);
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 500);

        var shellSnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = shellSnId,
            Serial = "0301101",
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

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = shellSnId,
            WorkCenterId = TestHelpers.wcRollsId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId, TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.Equal("0301101", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Equal(500, result.RecentRecords[0].TankSize);
    }

    /// <summary>
    /// Long Seam history must always show the shell serial, even if that shell
    /// is linked to an assembly alpha code downstream.
    /// </summary>
    [Fact]
    public async Task LongSeam_ShowsShellSerialEvenWhenAssemblyExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);

        var shellSnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = shellSnId,
            Serial = "0301201",
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

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = shellSnId,
            WorkCenterId = TestHelpers.wcLongSeamId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcLongSeamId, TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.Equal("0301201", result.RecentRecords[0].SerialOrIdentifier);
        Assert.Equal(120, result.RecentRecords[0].TankSize);
    }

    /// <summary>
    /// Long Seam Inspection history must always show the shell serial, even if
    /// that shell is linked to an assembly alpha code downstream.
    /// </summary>
    [Fact]
    public async Task LongSeamInspection_ShowsShellSerialEvenWhenAssemblyExists()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 120);

        var shellSnId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = shellSnId,
            Serial = "0301301",
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

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = shellSnId,
            WorkCenterId = TestHelpers.wcLongSeamInspId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoryAsync(
            TestHelpers.wcLongSeamInspId, TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.Equal("0301301", result.RecentRecords[0].SerialOrIdentifier);
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
            TestHelpers.ProductionLine1Plt1Id,
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
    /// For Round Seam Inspection, history should include shell code(s) with the
    /// assembly alpha code, because the station records at assembly level.
    /// </summary>
    [Fact]
    public async Task RoundSeamInspection_ShowsAlphaCodeWithShellList()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var assembledProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == 500);
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 500);

        var assemblySnId = Guid.NewGuid();
        var shellSnId = Guid.NewGuid();
        db.SerialNumbers.AddRange(
            new SerialNumber
            {
                Id = assemblySnId,
                Serial = "RSA1",
                ProductId = assembledProduct.Id,
                PlantId = TestHelpers.PlantPlt1Id,
                CreatedAt = DateTime.UtcNow
            },
            new SerialNumber
            {
                Id = shellSnId,
                Serial = "RS-SHELL-001",
                ProductId = shellProduct.Id,
                PlantId = TestHelpers.PlantPlt1Id,
                CreatedAt = DateTime.UtcNow
            }
        );

        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = shellSnId,
            ToSerialNumberId = assemblySnId,
            Relationship = "ShellToAssembly",
            Quantity = 1,
            Timestamp = DateTime.UtcNow
        });

        db.ProductionRecords.Add(new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = assemblySnId,
            WorkCenterId = TestHelpers.wcRoundSeamInspId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRoundSeamInspId,
            TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null,
            limit: 10);

        Assert.Single(result.RecentRecords);
        var entry = result.RecentRecords[0];
        Assert.StartsWith("RSA1 (", entry.SerialOrIdentifier);
        Assert.Contains("RS-SHELL-001", entry.SerialOrIdentifier);
        Assert.Equal(500, entry.TankSize);
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
            TestHelpers.ProductionLine1Plt1Id,
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
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Equal(2, result.RecentRecords.Count);
        Assert.Single(result.TankSizeCounts);
        Assert.Equal(120, result.TankSizeCounts[0].TankSize);
        Assert.Equal(1, result.TankSizeCounts[0].Count);
    }

    [Fact]
    public async Task TankSizeCounts_AreGroupedByTodayProduction()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product120 = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var product500 = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 500);

        var serials = new[]
        {
            new SerialNumber { Id = Guid.NewGuid(), Serial = "TS-120-A", ProductId = product120.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow },
            new SerialNumber { Id = Guid.NewGuid(), Serial = "TS-120-B", ProductId = product120.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow },
            new SerialNumber { Id = Guid.NewGuid(), Serial = "TS-500-A", ProductId = product500.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow }
        };
        db.SerialNumbers.AddRange(serials);

        db.ProductionRecords.AddRange(
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = serials[0].Id,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            },
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = serials[1].Id,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            },
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = serials[2].Id,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId,
            TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null,
            limit: 10);

        Assert.Equal(3, result.DayCount);
        Assert.Equal(2, result.TankSizeCounts.Count);
        Assert.Contains(result.TankSizeCounts, item => item.TankSize == 120 && item.Count == 2);
        Assert.Contains(result.TankSizeCounts, item => item.TankSize == 500 && item.Count == 1);
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

        var annotationTypeId = Guid.Parse("a1000005-0000-0000-0000-000000000005"); // Correction Needed - yellow
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
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.True(result.RecentRecords[0].HasAnnotation);
        Assert.Equal("#ffff00", result.RecentRecords[0].AnnotationColor);
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
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Single(result.RecentRecords);
        Assert.False(result.RecentRecords[0].HasAnnotation);
        Assert.Null(result.RecentRecords[0].AnnotationColor);
    }

    /// <summary>
    /// Last 5 Transactions is strict to the configured WorkCenter + ProductionLine (+ optional Asset).
    /// If no production records exist in scope, the history should be empty.
    /// </summary>
    [Fact]
    public async Task ReturnsEmpty_WhenNoProductionRecordsExistInScope()
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
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10);

        Assert.Equal(0, result.DayCount);
        Assert.Empty(result.RecentRecords);
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
            TestHelpers.ProductionLine1Plt1Id,
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
            TestHelpers.ProductionLine1Plt1Id,
            date: null, limit: 10, assetId: TestHelpers.TestAssetId);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        Assert.Equal("ASSET-A", result.RecentRecords[0].SerialOrIdentifier);
    }

    [Fact]
    public async Task ProductionLineScope_ExcludesRecordsFromOtherLines()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var shellProduct = await db.Products.FirstAsync(p => p.ProductType!.SystemTypeName == "shell" && p.TankSize == 120);
        var otherLineId = Guid.Parse("e2111111-1111-1111-1111-111111111111");

        var snLine1 = Guid.NewGuid();
        var snLine2 = Guid.NewGuid();
        db.SerialNumbers.AddRange(
            new SerialNumber { Id = snLine1, Serial = "LINE1", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow },
            new SerialNumber { Id = snLine2, Serial = "LINE2", ProductId = shellProduct.Id, PlantId = TestHelpers.PlantPlt1Id, CreatedAt = DateTime.UtcNow }
        );

        db.ProductionRecords.AddRange(
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = snLine1,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            },
            new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = snLine2,
                WorkCenterId = TestHelpers.wcRollsId,
                ProductionLineId = otherLineId,
                OperatorId = TestHelpers.TestUserId,
                Timestamp = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.GetHistoryAsync(
            TestHelpers.wcRollsId,
            TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null,
            limit: 10);

        Assert.Equal(1, result.DayCount);
        Assert.Single(result.RecentRecords);
        Assert.Equal("LINE1", result.RecentRecords[0].SerialOrIdentifier);
    }
}
