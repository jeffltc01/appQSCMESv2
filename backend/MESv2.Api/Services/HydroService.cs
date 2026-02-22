using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class HydroService : IHydroService
{
    private readonly MesDbContext _db;

    public HydroService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<HydroRecordResponseDto> CreateAsync(CreateHydroRecordDto dto, CancellationToken cancellationToken = default)
    {
        var sellableSn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == dto.NameplateSerialNumber, cancellationToken)
            ?? throw new InvalidOperationException($"Sellable serial number '{dto.NameplateSerialNumber}' not found.");

        var assemblySn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == dto.AssemblyAlphaCode && s.Product!.ProductType!.SystemTypeName == "assembled", cancellationToken);

        if (assemblySn != null)
        {
            var assemblyTankSize = assemblySn.Product?.TankSize;
            var sellableTankSize = sellableSn.Product?.TankSize;
            if (assemblyTankSize != null && sellableTankSize != null && assemblyTankSize != sellableTankSize)
            {
                throw new InvalidOperationException(
                    $"Tank size mismatch: the shell's assembly is {assemblyTankSize} gal but the nameplate is {sellableTankSize} gal. These must match.");
            }
        }

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sellableSn.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId,
            OperatorId = dto.OperatorId,
            ProductionLineId = dto.ProductionLineId,
            Timestamp = DateTime.UtcNow,
            InspectionResult = dto.Result
        };
        _db.ProductionRecords.Add(record);

        if (assemblySn != null)
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = assemblySn.Id,
                ToSerialNumberId = sellableSn.Id,
                Relationship = "hydro-marriage",
                Timestamp = DateTime.UtcNow
            });
        }

        if (dto.Defects.Count > 0)
        {
            foreach (var defect in dto.Defects)
            {
                _db.DefectLogs.Add(new DefectLog
                {
                    Id = Guid.NewGuid(),
                    ProductionRecordId = record.Id,
                    SerialNumberId = sellableSn.Id,
                    DefectCodeId = defect.DefectCodeId,
                    CharacteristicId = defect.CharacteristicId,
                    LocationId = defect.LocationId,
                    CreatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new HydroRecordResponseDto
        {
            Id = record.Id,
            AssemblyAlphaCode = dto.AssemblyAlphaCode,
            NameplateSerialNumber = dto.NameplateSerialNumber,
            Result = dto.Result,
            Timestamp = record.Timestamp
        };
    }

    public async Task<IReadOnlyList<DefectLocationDto>> GetLocationsByCharacteristicAsync(Guid characteristicId, CancellationToken cancellationToken = default)
    {
        var locations = await _db.DefectLocations
            .Where(d => d.CharacteristicId == characteristicId)
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        return locations.Select(d => new DefectLocationDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name
        }).ToList();
    }
}
