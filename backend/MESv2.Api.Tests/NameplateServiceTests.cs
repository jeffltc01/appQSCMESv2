using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;
using Moq;

namespace MESv2.Api.Tests;

public class NameplateServiceTests
{
    private static NameplateService CreateService(
        Data.MesDbContext db,
        Mock<INiceLabelService>? mockNiceLabel = null,
        bool allowLivePrintInNonProd = false,
        string environmentName = "Production")
    {
        var mock = mockNiceLabel ?? new Mock<INiceLabelService>();
        var mockHostEnvironment = new Mock<IHostEnvironment>();
        mockHostEnvironment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

        var options = Options.Create(new NiceLabelOptions
        {
            AllowLivePrintInNonProd = allowLivePrintInNonProd
        });

        return new NameplateService(
            db,
            NullLogger<NameplateService>.Instance,
            mock.Object,
            mockHostEnvironment.Object,
            options);
    }

    [Fact]
    public async Task Create_ValidRecord_Succeeds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        var sut = CreateService(db);
        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00100001",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.NotNull(result);
        Assert.Equal("W00100001", result.SerialNumber);

        var sn = db.SerialNumbers.FirstOrDefault(s => s.Serial == "W00100001");
        Assert.NotNull(sn);
        Assert.Equal(product.Id, sn.ProductId);
    }

    [Fact]
    public async Task Create_DuplicateSerial_ReturnsExistingRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "W00100002",
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(db);

        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00100002",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.Equal("W00100002", result.SerialNumber);
        Assert.False(result.PrintSucceeded);
        Assert.Contains("ignored", result.PrintMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, db.SerialNumbers.Count(s => s.Serial == "W00100002"));
    }

    [Fact]
    public async Task GetBySerial_Found_ReturnsRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "W00100003",
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateService(db);
        var result = await sut.GetBySerialAsync("W00100003");

        Assert.NotNull(result);
        Assert.Equal("W00100003", result.SerialNumber);
    }

    [Fact]
    public async Task GetBySerial_NotFound_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);
        var result = await sut.GetBySerialAsync("NONEXISTENT");
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_WithPrinter_CallsNiceLabelAndWritesPrintLog()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-Printer-1",
            DocumentPath = "/Solutions/MES/DataPlateFoilLabel.nlbl",
            PrintLocation = "Nameplate",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        mockNiceLabel
            .Setup(x => x.PrintNameplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((true, (string?)null));

        var sut = CreateService(db, mockNiceLabel);
        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00200001",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.True(result.PrintSucceeded);

        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            "NP-Printer-1", "/Solutions/MES/DataPlateFoilLabel.nlbl", 1, It.IsAny<string>(),
            product.TankType, product.TankSize, "W00200001"), Times.Once);

        var printLog = db.PrintLogs.FirstOrDefault(pl => pl.PrinterName == "NP-Printer-1");
        Assert.NotNull(printLog);
        Assert.True(printLog.Succeeded);
    }

    [Fact]
    public async Task Create_NoPrinterConfigured_SkipsPrintAndReturnsFalse()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        var mockNiceLabel = new Mock<INiceLabelService>();
        var sut = CreateService(db, mockNiceLabel);

        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00200002",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.False(result.PrintSucceeded);
        Assert.Contains("No printer configured", result.PrintMessage);

        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);

        Assert.Empty(db.PrintLogs.ToList());
    }

    [Fact]
    public async Task Create_NoDocumentConfigured_SkipsPrintAndReturnsFalse()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-NoDoc",
            PrintLocation = "Nameplate",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        var sut = CreateService(db, mockNiceLabel);

        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00200009",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.False(result.PrintSucceeded);
        Assert.Contains("No document configured", result.PrintMessage);

        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_NonProdLivePrintDisabled_SuppressesPrint()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-NonProd",
            DocumentPath = "/Solutions/MES/DataPlateFoilLabel.nlbl",
            PrintLocation = "Nameplate",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        var sut = CreateService(
            db,
            mockNiceLabel,
            allowLivePrintInNonProd: false,
            environmentName: Environments.Development);

        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00200010",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.False(result.PrintSucceeded);
        Assert.Contains("suppressed", result.PrintMessage, StringComparison.OrdinalIgnoreCase);

        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_PrintFails_RecordSavedAndPrintLogWritten()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-Printer-Fail",
            DocumentPath = "/Solutions/MES/DataPlateFoilLabel.nlbl",
            PrintLocation = "Nameplate",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        mockNiceLabel
            .Setup(x => x.PrintNameplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((false, (string?)"Printer offline"));

        var sut = CreateService(db, mockNiceLabel);
        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00200003",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.Equal("W00200003", result.SerialNumber);
        Assert.False(result.PrintSucceeded);
        Assert.Contains("Printer offline", result.PrintMessage);

        var sn = db.SerialNumbers.FirstOrDefault(s => s.Serial == "W00200003");
        Assert.NotNull(sn);

        var printLog = db.PrintLogs.FirstOrDefault(pl => pl.PrinterName == "NP-Printer-Fail");
        Assert.NotNull(printLog);
        Assert.False(printLog.Succeeded);
        Assert.Equal("Printer offline", printLog.ErrorMessage);
    }

    [Fact]
    public async Task Create_DisabledPrinter_SkipsPrint()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-Disabled",
            DocumentPath = "/Solutions/MES/DataPlateFoilLabel.nlbl",
            PrintLocation = "Nameplate",
            Enabled = false
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        var sut = CreateService(db, mockNiceLabel);

        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00200004",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.False(result.PrintSucceeded);
        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Reprint_ExistingSerial_CallsNiceLabel()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "W00300001",
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestHelpers.TestUserId
        });
        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-Reprint",
            DocumentPath = "/Solutions/MES/DataPlateFoilLabel.nlbl",
            PrintLocation = "Nameplate",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        mockNiceLabel
            .Setup(x => x.PrintNameplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((true, (string?)null));

        var sut = CreateService(db, mockNiceLabel);
        var result = await sut.ReprintAsync(snId);

        Assert.True(result.PrintSucceeded);
        Assert.Equal("W00300001", result.SerialNumber);

        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            "NP-Reprint", "/Solutions/MES/DataPlateFoilLabel.nlbl", 1, It.IsAny<string>(),
            product.TankType, product.TankSize, "W00300001"), Times.Once);
    }

    [Fact]
    public async Task Reprint_NotFound_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ReprintAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesProductAndReprints()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product120 = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);
        var product250 = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 250);

        var snId = Guid.NewGuid();
        db.SerialNumbers.Add(new SerialNumber
        {
            Id = snId,
            Serial = "W00400001",
            ProductId = product120.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = TestHelpers.TestUserId
        });
        db.PlantPrinters.Add(new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = TestHelpers.PlantPlt1Id,
            PrinterName = "NP-Update",
            DocumentPath = "/Solutions/MES/DataPlateFoilLabel.nlbl",
            PrintLocation = "Nameplate",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var mockNiceLabel = new Mock<INiceLabelService>();
        mockNiceLabel
            .Setup(x => x.PrintNameplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((true, (string?)null));

        var sut = CreateService(db, mockNiceLabel);
        var result = await sut.UpdateAsync(snId, new UpdateNameplateRecordDto
        {
            ProductId = product250.Id,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.Equal("W00400001", result.SerialNumber);
        Assert.Equal(product250.Id, result.ProductId);
        Assert.Equal(250, result.TankSize);
        Assert.True(result.PrintSucceeded);

        var serial = db.SerialNumbers.First(s => s.Id == snId);
        Assert.Equal(product250.Id, serial.ProductId);

        mockNiceLabel.Verify(x => x.PrintNameplateAsync(
            "NP-Update", "/Solutions/MES/DataPlateFoilLabel.nlbl", 1, It.IsAny<string>(),
            product250.TankType, product250.TankSize, "W00400001"), Times.Once);
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);
        var sut = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), new UpdateNameplateRecordDto
            {
                ProductId = product.Id,
                OperatorId = TestHelpers.TestUserId
            }));
    }
}
