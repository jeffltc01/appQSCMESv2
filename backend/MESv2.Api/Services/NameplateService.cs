using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NameplateService : INameplateService
{
    private readonly MesDbContext _db;
    private readonly ILogger<NameplateService> _logger;
    private readonly INiceLabelService _niceLabelService;

    public NameplateService(MesDbContext db, ILogger<NameplateService> logger, INiceLabelService niceLabelService)
    {
        _db = db;
        _logger = logger;
        _niceLabelService = niceLabelService;
    }

    public async Task<NameplateRecordResponseDto> CreateAsync(CreateNameplateRecordDto dto, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.SerialNumbers
            .AnyAsync(s => s.Serial == dto.SerialNumber && s.Product!.ProductType!.SystemTypeName == "sellable", cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This serial number already exists");

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
            ProductionLineId = Guid.Empty,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        await _db.SaveChangesAsync(cancellationToken);

        var (printSucceeded, printMessage) = await PrintForSerialAsync(
            sn.Id, dto.ProductId, plantId, dto.OperatorId, cancellationToken);

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = dto.ProductId,
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
            Timestamp = sn.CreatedAt
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

        var (success, errorMessage) = await _niceLabelService.PrintNameplateAsync(
            printer.PrinterName,
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
