using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class AssemblyService : IAssemblyService
{
    private readonly MesDbContext _db;

    public AssemblyService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetNextAlphaCodeAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _db.Plants.FirstAsync(p => p.Id == plantId, cancellationToken);
        var current = plant.NextTankAlphaCode;

        plant.NextTankAlphaCode = AdvanceAlphaCode(current);

        return current;
    }

    public static string AdvanceAlphaCode(string code)
    {
        var high = code[0] - 'A';
        var low = code[1] - 'A';
        var nextIndex = (high * 26 + low + 1) % 676;
        return $"{(char)('A' + nextIndex / 26)}{(char)('A' + nextIndex % 26)}";
    }

    public async Task<CreateAssemblyResponseDto> CreateAsync(CreateAssemblyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.WorkCenterId == Guid.Empty)
            throw new ArgumentException("Work center is required.");
        if (dto.ProductionLineId == Guid.Empty)
            throw new ArgumentException("Production line is required. Please select a production line before saving.");
        if (dto.OperatorId == Guid.Empty)
            throw new ArgumentException("Operator is required.");

        var wc = await _db.WorkCenters
            .FirstOrDefaultAsync(w => w.Id == dto.WorkCenterId, cancellationToken);
        if (wc == null)
            throw new ArgumentException("Work center not found.");

        var plantId = await _db.ProductionLines
            .Where(p => p.Id == dto.ProductionLineId)
            .Select(p => p.PlantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plantId == Guid.Empty)
            throw new ArgumentException("Production line not found.");

        var alphaCode = await GetNextAlphaCodeAsync(plantId, cancellationToken);

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == dto.TankSize, cancellationToken)
            ?? throw new ArgumentException($"No assembled tank product found for tank size {dto.TankSize}.");

        var assemblySn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = alphaCode,
            ProductId = product.Id,
            PlantId = plantId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = dto.OperatorId
        };
        _db.SerialNumbers.Add(assemblySn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = assemblySn.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId == Guid.Empty ? null : dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        for (int i = 0; i < dto.Shells.Count; i++)
        {
            var sn = await _db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == dto.Shells[i], cancellationToken);
            if (sn != null)
            {
                _db.TraceabilityLogs.Add(new TraceabilityLog
                {
                    Id = Guid.NewGuid(),
                    FromSerialNumberId = sn.Id,
                    ToSerialNumberId = assemblySn.Id,
                    ProductionRecordId = record.Id,
                    Relationship = "ShellToAssembly",
                    TankLocation = $"Shell {i + 1}",
                    Quantity = 1,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        AddHeadTrace(dto.LeftHeadLotId, dto.LeftHeadHeatNumber, dto.LeftHeadCoilNumber, dto.LeftHeadLotNumber,
            assemblySn, record.Id, plantId, "Head 1");
        AddHeadTrace(dto.RightHeadLotId, dto.RightHeadHeatNumber, dto.RightHeadCoilNumber, dto.RightHeadLotNumber,
            assemblySn, record.Id, plantId, "Head 2");

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

        return new CreateAssemblyResponseDto
        {
            Id = assemblySn.Id,
            AlphaCode = assemblySn.Serial,
            Timestamp = record.Timestamp
        };
    }

    public async Task<CreateAssemblyResponseDto> ReassembleAsync(string alphaCode, ReassemblyDto dto, CancellationToken cancellationToken = default)
    {
        var assemblySn = await _db.SerialNumbers
            .Where(s => s.Serial == alphaCode && !s.IsObsolete && s.Product!.ProductType!.SystemTypeName == "assembled")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (assemblySn == null)
            throw new ArgumentException("Assembly not found.");

        if (dto.Shells != null)
        {
            var existingShellLogs = await _db.TraceabilityLogs
                .Where(t => t.ToSerialNumberId == assemblySn.Id
                    && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell"))
                .ToListAsync(cancellationToken);
            _db.TraceabilityLogs.RemoveRange(existingShellLogs);

            for (int i = 0; i < dto.Shells.Count; i++)
            {
                var sn = await _db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == dto.Shells[i], cancellationToken);
                if (sn != null)
                {
                    _db.TraceabilityLogs.Add(new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = sn.Id,
                        ToSerialNumberId = assemblySn.Id,
                        Relationship = "ShellToAssembly",
                        TankLocation = $"Shell {i + 1}",
                        Quantity = 1,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        if (dto.OperatorId.HasValue)
            assemblySn.ModifiedByUserId = dto.OperatorId.Value;
        assemblySn.ModifiedDateTime = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateAssemblyResponseDto
        {
            Id = assemblySn.Id,
            AlphaCode = assemblySn.Serial,
            Timestamp = assemblySn.ModifiedDateTime ?? assemblySn.CreatedAt
        };
    }

    private void AddHeadTrace(string? lotId, string? heatNumber, string? coilNumber, string? lotNumber,
        SerialNumber assemblySn, Guid productionRecordId, Guid plantId, string tankLocation)
    {
        if (string.IsNullOrEmpty(lotId) && string.IsNullOrEmpty(heatNumber) && string.IsNullOrEmpty(coilNumber) && string.IsNullOrEmpty(lotNumber))
            return;

        Guid? headSnId = null;
        var hasHeatCoil = !string.IsNullOrEmpty(heatNumber) || !string.IsNullOrEmpty(coilNumber);
        var hasLot = !string.IsNullOrEmpty(lotNumber);

        if (hasHeatCoil || hasLot)
        {
            var serial = hasHeatCoil
                ? $"Head {heatNumber ?? ""}/{coilNumber ?? ""}"
                : $"Lot:{lotNumber}";
            var headSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = serial,
                CoilNumber = coilNumber,
                HeatNumber = heatNumber,
                LotNumber = lotNumber,
                PlantId = plantId,
                CreatedAt = DateTime.UtcNow
            };
            _db.SerialNumbers.Add(headSn);
            headSnId = headSn.Id;
        }

        _db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = headSnId,
            ToSerialNumberId = assemblySn.Id,
            ProductionRecordId = productionRecordId,
            Relationship = "HeadToAssembly",
            TankLocation = tankLocation,
            Timestamp = DateTime.UtcNow
        });
    }
}
