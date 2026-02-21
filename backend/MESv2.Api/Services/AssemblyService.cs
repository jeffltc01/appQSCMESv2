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
        var existing = await _db.SerialNumbers
            .Where(s => s.PlantId == plantId && s.Product!.ProductType!.SystemTypeName == "assembled")
            .Select(s => s.Serial)
            .ToListAsync(cancellationToken);

        var maxIndex = -1;
        foreach (var code in existing)
        {
            if (code.Length != 2) continue;
            var first = code[0] - 'A';
            var second = code[1] - 'A';
            if (first is >= 0 and <= 25 && second is >= 0 and <= 25)
            {
                var idx = first * 26 + second;
                if (idx > maxIndex) maxIndex = idx;
            }
        }

        var nextIndex = (maxIndex + 1) % 676;
        var high = nextIndex / 26;
        var low = nextIndex % 26;
        return $"{(char)('A' + high)}{(char)('A' + low)}";
    }

    public async Task<CreateAssemblyResponseDto> CreateAsync(CreateAssemblyDto dto, CancellationToken cancellationToken = default)
    {
        var wc = await _db.WorkCenters
            .FirstOrDefaultAsync(w => w.Id == dto.WorkCenterId, cancellationToken);
        if (wc == null)
            throw new ArgumentException("Work center not found.");

        var plantId = await _db.ProductionLines
            .Where(p => p.Id == dto.ProductionLineId)
            .Select(p => p.PlantId)
            .FirstOrDefaultAsync(cancellationToken);

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
            AssetId = dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        foreach (var shellSerial in dto.Shells)
        {
            var sn = await _db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == shellSerial, cancellationToken);
            if (sn != null)
            {
                _db.TraceabilityLogs.Add(new TraceabilityLog
                {
                    Id = Guid.NewGuid(),
                    FromSerialNumberId = sn.Id,
                    ToSerialNumberId = assemblySn.Id,
                    Relationship = "shell",
                    Quantity = 1,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        if (!string.IsNullOrEmpty(dto.LeftHeadLotId))
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                ToSerialNumberId = assemblySn.Id,
                Relationship = "leftHead",
                TankLocation = dto.LeftHeadLotId,
                Timestamp = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrEmpty(dto.RightHeadLotId))
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                ToSerialNumberId = assemblySn.Id,
                Relationship = "rightHead",
                TankLocation = dto.RightHeadLotId,
                Timestamp = DateTime.UtcNow
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
            .FirstOrDefaultAsync(s => s.Serial == alphaCode && s.Product!.ProductType!.SystemTypeName == "assembled", cancellationToken);
        if (assemblySn == null)
            throw new ArgumentException("Assembly not found.");

        if (dto.Shells != null)
        {
            var existingShellLogs = await _db.TraceabilityLogs
                .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "shell")
                .ToListAsync(cancellationToken);
            _db.TraceabilityLogs.RemoveRange(existingShellLogs);

            foreach (var shellSerial in dto.Shells)
            {
                var sn = await _db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == shellSerial, cancellationToken);
                if (sn != null)
                {
                    _db.TraceabilityLogs.Add(new TraceabilityLog
                    {
                        Id = Guid.NewGuid(),
                        FromSerialNumberId = sn.Id,
                        ToSerialNumberId = assemblySn.Id,
                        Relationship = "shell",
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
}
