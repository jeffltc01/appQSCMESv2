using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class HydroService : IHydroService
{
    private static readonly Dictionary<string, HashSet<string>> ValidResultValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PassFail"] = new(StringComparer.Ordinal) { "Pass", "Fail" },
        ["AcceptReject"] = new(StringComparer.Ordinal) { "Accept", "Reject" },
        ["GoNoGo"] = new(StringComparer.Ordinal) { "Go", "NoGo" },
    };

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
            .Where(s => s.Serial == dto.AssemblyAlphaCode && !s.IsObsolete && s.Product!.ProductType!.SystemTypeName == "assembled")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

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

        foreach (var result in dto.Results)
        {
            var cp = await _db.ControlPlans.FindAsync(new object[] { result.ControlPlanId }, cancellationToken)
                ?? throw new ArgumentException($"ControlPlan '{result.ControlPlanId}' not found.");

            if (!ValidResultValues.TryGetValue(cp.ResultType, out var allowed) || !allowed.Contains(result.ResultText))
                throw new ArgumentException(
                    $"Invalid ResultText '{result.ResultText}' for ResultType '{cp.ResultType}'.");
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
        };
        _db.ProductionRecords.Add(record);

        foreach (var result in dto.Results)
        {
            _db.InspectionRecords.Add(new InspectionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = sellableSn.Id,
                ProductionRecordId = record.Id,
                WorkCenterId = dto.WorkCenterId,
                OperatorId = dto.OperatorId,
                Timestamp = DateTime.UtcNow,
                ControlPlanId = result.ControlPlanId,
                ResultText = result.ResultText,
            });
        }

        if (assemblySn != null)
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = assemblySn.Id,
                ToSerialNumberId = sellableSn.Id,
                ProductionRecordId = record.Id,
                Relationship = "hydro-marriage",
                Timestamp = DateTime.UtcNow
            });
        }

        _db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = sellableSn.Id,
            ToSerialNumberId = assemblySn?.Id,
            ProductionRecordId = record.Id,
            Relationship = "NameplateToAssembly",
            Timestamp = DateTime.UtcNow
        });

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
