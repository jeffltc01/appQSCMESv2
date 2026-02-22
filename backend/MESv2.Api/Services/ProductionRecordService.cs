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

        var isRollsWC = await _db.WorkCenters
            .Where(w => w.Id == dto.WorkCenterId)
            .Select(w => w.DataEntryType)
            .FirstOrDefaultAsync(cancellationToken) == "Rolls";

        var isCatchUp = false;
        Guid? previousRollsSerialId = null;
        if (serial == null)
        {
            if (!isRollsWC)
            {
                var rollsRecords = _db.ProductionRecords
                    .Where(r => r.ProductionLineId == dto.ProductionLineId
                                && r.WorkCenter.DataEntryType == "Rolls");

                // Shell serials are sequential zero-padded numbers (e.g. "012744").
                // String comparison preserves numeric order for fixed-width serials,
                // so we can find the nearest predecessor entirely in SQL.
                var predecessor = await rollsRecords
                    .Where(r => r.SerialNumber.Serial.CompareTo(dto.SerialNumber) < 0)
                    .OrderByDescending(r => r.SerialNumber.Serial)
                    .Select(r => new { r.SerialNumberId, r.SerialNumber.ProductId })
                    .FirstOrDefaultAsync(cancellationToken);

                // Fallback: if no predecessor found (e.g. first shell ever), use most recent by timestamp
                var match = predecessor
                    ?? await rollsRecords
                        .OrderByDescending(r => r.Timestamp)
                        .Select(r => new { r.SerialNumberId, r.SerialNumber.ProductId })
                        .FirstOrDefaultAsync(cancellationToken);

                if (match != null)
                {
                    previousRollsSerialId = match.SerialNumberId;
                    if (resolvedProductId == null)
                        resolvedProductId = match.ProductId;
                }
            }

            serial = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = dto.SerialNumber,
                ProductId = resolvedProductId,
                CoilNumber = dto.CoilNumber,
                HeatNumber = dto.HeatNumber,
                CreatedAt = DateTime.UtcNow
            };
            _db.SerialNumbers.Add(serial);
            await _db.SaveChangesAsync(cancellationToken);

            if (!isRollsWC)
            {
                isCatchUp = true;
                warning = "Rolls missed — annotation created.";
            }
        }
        else
        {
            if (serial.ProductId == null && resolvedProductId != null)
                serial.ProductId = resolvedProductId;
            if (!string.IsNullOrEmpty(dto.CoilNumber))
                serial.CoilNumber = dto.CoilNumber;
            if (!string.IsNullOrEmpty(dto.HeatNumber))
                serial.HeatNumber = dto.HeatNumber;
        }

        var duplicate = await _db.ProductionRecords
            .AnyAsync(r => r.SerialNumberId == serial.Id && r.WorkCenterId == dto.WorkCenterId
                && r.Timestamp > DateTime.UtcNow.AddMinutes(-5), cancellationToken);
        if (duplicate)
            warning = (warning != null ? warning + " " : "") + "Duplicate production record detected.";

        var plantGearId = await _db.ProductionLines
            .Where(pl => pl.Id == dto.ProductionLineId)
            .Select(pl => pl.Plant.CurrentPlantGearId)
            .FirstOrDefaultAsync(cancellationToken);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = serial.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow,
            PlantGearId = plantGearId
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
            if (previousRollsSerialId.HasValue)
            {
                var plateLink = await _db.TraceabilityLogs
                    .Where(t => t.ToSerialNumberId == previousRollsSerialId.Value
                                && t.Relationship == "plate")
                    .Select(t => new { t.FromSerialNumberId, t.Quantity })
                    .FirstOrDefaultAsync(cancellationToken);

                if (plateLink != null)
                {
                    _db.TraceabilityLogs.Add(new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = plateLink.FromSerialNumberId,
                        ToSerialNumberId = serial.Id,
                        ProductionRecordId = record.Id,
                        Relationship = "plate",
                        Quantity = plateLink.Quantity,
                        Timestamp = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            var correctionNeededType = await _db.AnnotationTypes
                .FirstOrDefaultAsync(at => at.Name == "Correction Needed", cancellationToken);
            if (correctionNeededType != null)
            {
                var hasTraceability = await _db.TraceabilityLogs
                    .AnyAsync(t => t.ToSerialNumberId == serial.Id && t.Relationship == "plate", cancellationToken);

                _db.Annotations.Add(new Annotation
                {
                    Id = Guid.NewGuid(),
                    ProductionRecordId = record.Id,
                    AnnotationTypeId = correctionNeededType.Id,
                    Status = AnnotationStatus.Open,
                    Notes = hasTraceability
                        ? $"Rolls scan missed for shell {dto.SerialNumber}. Product and material lot inherited from previous Rolls record — verify correct product and material lot."
                        : $"Rolls scan missed for shell {dto.SerialNumber}. No previous Rolls record found — product and material lot unknown. Verify and assign correct product and material lot.",
                    InitiatedByUserId = dto.OperatorId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        if (isRollsWC)
        {
            var activeQueueItem = await _db.MaterialQueueItems
                .FirstOrDefaultAsync(m => m.WorkCenterId == dto.WorkCenterId && m.Status == "active", cancellationToken);
            if (activeQueueItem != null)
            {
                activeQueueItem.QuantityCompleted++;
                if (activeQueueItem.QuantityCompleted >= activeQueueItem.Quantity)
                    activeQueueItem.Status = "completed";
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
