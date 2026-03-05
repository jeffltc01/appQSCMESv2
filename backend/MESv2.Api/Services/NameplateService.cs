using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NameplateService : INameplateService
{
    private readonly MesDbContext _db;
    private readonly ILogger<NameplateService> _logger;
    private readonly INiceLabelService _niceLabelService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly NiceLabelOptions _niceLabelOptions;

    public NameplateService(
        MesDbContext db,
        ILogger<NameplateService> logger,
        INiceLabelService niceLabelService,
        IHostEnvironment hostEnvironment,
        IOptions<NiceLabelOptions> niceLabelOptions)
    {
        _db = db;
        _logger = logger;
        _niceLabelService = niceLabelService;
        _hostEnvironment = hostEnvironment;
        _niceLabelOptions = niceLabelOptions.Value;
    }

    public async Task<NameplateRecordResponseDto> CreateAsync(CreateNameplateRecordDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _db.SerialNumbers
            .Include(s => s.Product)
            .Where(s => s.Serial == dto.SerialNumber && s.Product!.ProductType!.SystemTypeName == "sellable")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != null)
        {
            return new NameplateRecordResponseDto
            {
                Id = existing.Id,
                SerialNumber = existing.Serial,
                ProductId = existing.ProductId ?? dto.ProductId,
                TankSize = existing.Product?.TankSize,
                Timestamp = existing.CreatedAt,
                PrintSucceeded = false,
                PrintMessage = "Duplicate submit ignored; existing nameplate record returned."
            };
        }

        var operator_ = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == dto.OperatorId, cancellationToken);
        var plantId = operator_?.DefaultSiteId ?? Guid.Empty;

        var sn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = dto.SerialNumber,
            ProductId = dto.ProductId,
            PlantId = plantId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = dto.OperatorId
        };
        _db.SerialNumbers.Add(sn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            WorkCenterId = dto.WorkCenterId,
            OperatorId = dto.OperatorId,
            ProductionLineId = dto.ProductionLineId,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        await _db.SaveChangesAsync(cancellationToken);

        var (printSucceeded, printMessage) = await PrintForSerialAsync(
            sn.Id, dto.ProductId, plantId, dto.OperatorId, cancellationToken);

        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = dto.ProductId,
            TankSize = product?.TankSize,
            Timestamp = record.Timestamp,
            PrintSucceeded = printSucceeded,
            PrintMessage = printMessage
        };
    }

    public async Task<NameplateRecordResponseDto?> GetBySerialAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == serialNumber && s.Product!.ProductType!.SystemTypeName == "sellable", cancellationToken);
        if (sn == null) return null;

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = sn.ProductId ?? Guid.Empty,
            TankSize = sn.Product?.TankSize,
            Timestamp = sn.CreatedAt
        };
    }

    public async Task<NameplateRecordResponseDto> UpdateAsync(Guid id, UpdateNameplateRecordDto dto, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .Include(s => s.Product)
            .ThenInclude(p => p!.ProductType)
            .FirstOrDefaultAsync(
                s => s.Id == id
                    && s.Product != null
                    && s.Product.ProductType != null
                    && s.Product.ProductType.SystemTypeName == "sellable",
                cancellationToken)
            ?? throw new InvalidOperationException("Nameplate serial number not found");

        var product = await _db.Products
            .Include(p => p.ProductType)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found");

        if (!string.Equals(product.ProductType?.SystemTypeName, "sellable", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Product must be a sellable tank");

        sn.ProductId = dto.ProductId;
        await _db.SaveChangesAsync(cancellationToken);

        var (printSucceeded, printMessage) = await PrintForSerialAsync(
            sn.Id,
            dto.ProductId,
            sn.PlantId,
            dto.OperatorId,
            cancellationToken);

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = dto.ProductId,
            TankSize = product.TankSize,
            Timestamp = sn.CreatedAt,
            PrintSucceeded = printSucceeded,
            PrintMessage = printMessage
        };
    }

    public async Task<NameplateRecordResponseDto> ReprintAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Serial number not found");

        var (printSucceeded, printMessage) = await PrintForSerialAsync(
            sn.Id,
            sn.ProductId ?? Guid.Empty,
            sn.PlantId,
            sn.CreatedByUserId ?? Guid.Empty,
            cancellationToken);

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = sn.ProductId ?? Guid.Empty,
            TankSize = sn.Product?.TankSize,
            Timestamp = sn.CreatedAt,
            PrintSucceeded = printSucceeded,
            PrintMessage = printMessage
        };
    }

    private async Task<(bool Success, string? Message)> PrintForSerialAsync(
        Guid serialNumberId,
        Guid productId,
        Guid plantId,
        Guid operatorId,
        CancellationToken cancellationToken)
    {
        var printer = await _db.PlantPrinters
            .Where(pp => pp.PlantId == plantId
                         && pp.PrintLocation == "Nameplate"
                         && pp.Enabled)
            .FirstOrDefaultAsync(cancellationToken);

        if (printer == null)
        {
            _logger.LogInformation("No enabled Nameplate printer found for plant {PlantId}, skipping print", plantId);
            return (false, "No printer configured for this plant");
        }

        if (string.IsNullOrWhiteSpace(printer.DocumentPath))
        {
            _logger.LogInformation("No Nameplate document configured for plant {PlantId}, skipping print", plantId);
            return (false, "No document configured for this plant");
        }

        var product = await _db.Products.FindAsync(new object[] { productId }, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for print", productId);
            return (false, "Product not found");
        }

        var serialNumber = await _db.SerialNumbers.FindAsync(new object[] { serialNumberId }, cancellationToken);
        var serialText = serialNumber?.Serial ?? string.Empty;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var printedOnText = localNow.ToString("MM/dd/yyyy h:mm tt");

        if (!_hostEnvironment.IsProduction() && !_niceLabelOptions.AllowLivePrintInNonProd)
        {
            const string suppressionMessage = "Print suppressed in non-production environment by configuration.";
            _logger.LogInformation(
                "Suppressing Nameplate live print in environment {EnvironmentName} for serial {SerialNumberId}",
                _hostEnvironment.EnvironmentName,
                serialNumberId);

            _db.PrintLogs.Add(new PrintLog
            {
                Id = Guid.NewGuid(),
                SerialNumberId = serialNumberId,
                PrinterName = printer.PrinterName,
                RequestedAt = DateTime.UtcNow,
                Succeeded = false,
                ErrorMessage = suppressionMessage,
                RequestedByUserId = operatorId
            });
            await _db.SaveChangesAsync(cancellationToken);
            return (false, suppressionMessage);
        }

        var (success, errorMessage) = await _niceLabelService.PrintNameplateAsync(
            printer.PrinterName,
            printer.DocumentPath,
            1,
            printedOnText,
            product.TankType,
            product.TankSize,
            serialText);

        _db.PrintLogs.Add(new PrintLog
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serialNumberId,
            PrinterName = printer.PrinterName,
            RequestedAt = DateTime.UtcNow,
            Succeeded = success,
            ErrorMessage = success ? null : errorMessage,
            RequestedByUserId = operatorId
        });
        await _db.SaveChangesAsync(cancellationToken);

        if (success)
            return (true, "Label sent to printer");

        return (false, $"Print failed: {errorMessage}");
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(Guid plantId, CancellationToken ct)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrEmpty(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
