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
                .FirstOrDefaultAsync(p => p.TankSize == parsedTankSize
                    && p.ProductType!.SystemTypeName == "shell", cancellationToken);
            resolvedProductId = product?.Id;
        }

        var isCatchUp = false;
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
            isCatchUp = true;
            warning = "Rolls missed — annotation created.";
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

        if (isCatchUp)
        {
            var correctionNeededType = await _db.AnnotationTypes
                .FirstOrDefaultAsync(at => at.Name == "Correction Needed", cancellationToken);
            if (correctionNeededType != null)
            {
                _db.Annotations.Add(new Annotation
                {
                    Id = Guid.NewGuid(),
                    ProductionRecordId = record.Id,
                    AnnotationTypeId = correctionNeededType.Id,
                    Flag = true,
                    Notes = $"Rolls scan missed for shell {dto.SerialNumber}. Material lot assumed from previous shell — validate.",
                    InitiatedByUserId = dto.OperatorId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        if (!string.IsNullOrEmpty(dto.HeatNumber) && !string.IsNullOrEmpty(dto.CoilNumber))
        {
            var plateSerial = $"Heat {dto.HeatNumber} Coil {dto.CoilNumber}";
            var plateSn = await _db.SerialNumbers
                .FirstOrDefaultAsync(s => s.Serial == plateSerial, cancellationToken);
            if (plateSn != null)
            {
                var alreadyLinked = await _db.TraceabilityLogs
                    .AnyAsync(t => t.FromSerialNumberId == plateSn.Id
                        && t.ToSerialNumberId == serial.Id
                        && t.Relationship == "plate", cancellationToken);
                if (!alreadyLinked)
                {
                    _db.TraceabilityLogs.Add(new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = plateSn.Id,
                        ToSerialNumberId = serial.Id,
                        ProductionRecordId = record.Id,
                        Relationship = "plate",
                        Quantity = 1,
                        Timestamp = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return new CreateProductionRecordResponseDto
        {
            Id = record.Id,
            SerialNumber = serial.Serial,
            Timestamp = record.Timestamp,
            Warning = warning
        };
    }
}
