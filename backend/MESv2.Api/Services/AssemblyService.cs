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
        var existing = await _db.Assemblies
            .Where(a => a.ProductionLine.PlantId == plantId)
            .Select(a => a.AlphaCode)
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

        var assembly = new Assembly
        {
            Id = Guid.NewGuid(),
            AlphaCode = alphaCode,
            TankSize = dto.TankSize,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow,
            IsActive = true
        };

        _db.Assemblies.Add(assembly);

        foreach (var shellSerial in dto.Shells)
        {
            var sn = await _db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == shellSerial, cancellationToken);
            if (sn != null)
            {
                _db.TraceabilityLogs.Add(new TraceabilityLog
                {
                    Id = Guid.NewGuid(),
                    FromSerialNumberId = sn.Id,
                    ToSerialNumberId = null,
                    FromAlphaCode = null,
                    ToAlphaCode = alphaCode,
                    Relationship = "shell",
                    Quantity = 1,
                    TankLocation = null,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        if (!string.IsNullOrEmpty(dto.LeftHeadLotId))
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = null,
                ToSerialNumberId = null,
                FromAlphaCode = null,
                ToAlphaCode = alphaCode,
                Relationship = "leftHead",
                Quantity = null,
                TankLocation = dto.LeftHeadLotId,
                Timestamp = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrEmpty(dto.RightHeadLotId))
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = null,
                ToSerialNumberId = null,
                FromAlphaCode = null,
                ToAlphaCode = alphaCode,
                Relationship = "rightHead",
                Quantity = null,
                TankLocation = dto.RightHeadLotId,
                Timestamp = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateAssemblyResponseDto
        {
            Id = assembly.Id,
            AlphaCode = assembly.AlphaCode,
            Timestamp = assembly.Timestamp
        };
    }

    public async Task<CreateAssemblyResponseDto> ReassembleAsync(string alphaCode, ReassemblyDto dto, CancellationToken cancellationToken = default)
    {
        var assembly = await _db.Assemblies
            .FirstOrDefaultAsync(a => a.AlphaCode == alphaCode, cancellationToken);
        if (assembly == null)
            throw new ArgumentException("Assembly not found.");

        if (dto.Shells != null)
        {
            var existingShellLogs = await _db.TraceabilityLogs
                .Where(t => t.ToAlphaCode == alphaCode && t.Relationship == "shell")
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
                        ToSerialNumberId = null,
                        FromAlphaCode = null,
                        ToAlphaCode = alphaCode,
                        Relationship = "shell",
                        Quantity = 1,
                        TankLocation = null,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        if (dto.OperatorId.HasValue)
            assembly.OperatorId = dto.OperatorId.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateAssemblyResponseDto
        {
            Id = assembly.Id,
            AlphaCode = assembly.AlphaCode,
            Timestamp = assembly.Timestamp
        };
    }
}
