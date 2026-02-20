using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class ProductionRecordService : IProductionRecordService
{
    private readonly MesDbContext _db;

    public ProductionRecordService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<CreateProductionRecordResponseDto> CreateAsync(CreateProductionRecordDto dto, CancellationToken cancellationToken = default)
    {
        string? warning = null;

        var serial = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == dto.SerialNumber, cancellationToken);

        Guid? resolvedProductId = null;
        if (!string.IsNullOrEmpty(dto.ShellSize) && int.TryParse(dto.ShellSize, out var parsedTankSize))
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.TankSize == parsedTankSize, cancellationToken);
            resolvedProductId = product?.Id;
        }

        if (serial == null)
        {
            serial = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = dto.SerialNumber,
                ProductId = resolvedProductId,
                CreatedAt = DateTime.UtcNow
            };
            _db.SerialNumbers.Add(serial);
            await _db.SaveChangesAsync(cancellationToken);
            warning = "Serial created (catch-up flow).";
        }
        else if (serial.ProductId == null && resolvedProductId != null)
        {
            serial.ProductId = resolvedProductId;
        }

        var duplicate = await _db.ProductionRecords
            .AnyAsync(r => r.SerialNumberId == serial.Id && r.WorkCenterId == dto.WorkCenterId
                && r.Timestamp > DateTime.UtcNow.AddMinutes(-5), cancellationToken);
        if (duplicate)
            warning = (warning != null ? warning + " " : "") + "Duplicate production record detected.";

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serial.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow,
            InspectionResult = dto.InspectionResult,
            PlantGearId = null
        };

        _db.ProductionRecords.Add(record);

        foreach (var welderId in dto.WelderIds)
        {
            _db.WelderLogs.Add(new WelderLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = record.Id,
                UserId = welderId,
                CharacteristicId = null
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateProductionRecordResponseDto
        {
            Id = record.Id,
            SerialNumber = serial.Serial,
            Timestamp = record.Timestamp,
            Warning = warning
        };
    }
}
