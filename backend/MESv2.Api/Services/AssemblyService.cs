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

        AddHeadTrace(dto.LeftHeadLotId, dto.LeftHeadHeatNumber, dto.LeftHeadCoilNumber,
            assemblySn, record.Id, plantId, "Head 1");
        AddHeadTrace(dto.RightHeadLotId, dto.RightHeadHeatNumber, dto.RightHeadCoilNumber,
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
            .FirstOrDefaultAsync(s => s.Serial == alphaCode && s.Product!.ProductType!.SystemTypeName == "assembled", cancellationToken);
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

    private void AddHeadTrace(string? lotId, string? heatNumber, string? coilNumber,
        SerialNumber assemblySn, Guid productionRecordId, Guid plantId, string tankLocation)
    {
        if (string.IsNullOrEmpty(lotId) && string.IsNullOrEmpty(heatNumber) && string.IsNullOrEmpty(coilNumber))
            return;

        Guid? headSnId = null;
        if (!string.IsNullOrEmpty(heatNumber) || !string.IsNullOrEmpty(coilNumber))
        {
            var headSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = $"Head {heatNumber ?? ""}/{coilNumber ?? ""}",
                CoilNumber = coilNumber,
                HeatNumber = heatNumber,
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
