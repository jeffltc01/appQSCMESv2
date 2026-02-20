using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class RoundSeamService : IRoundSeamService
{
    private readonly MesDbContext _db;

    public RoundSeamService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<RoundSeamSetupDto?> GetSetupAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var setup = await _db.RoundSeamSetups
            .Where(s => s.WorkCenterId == wcId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (setup == null) return null;

        int requiredSeams = setup.TankSize <= 500 ? 2 : setup.TankSize <= 1000 ? 3 : 4;
        bool isComplete = setup.Rs1WelderId.HasValue && setup.Rs2WelderId.HasValue;
        if (requiredSeams >= 3) isComplete = isComplete && setup.Rs3WelderId.HasValue;
        if (requiredSeams >= 4) isComplete = isComplete && setup.Rs4WelderId.HasValue;

        return new RoundSeamSetupDto
        {
            Id = setup.Id,
            TankSize = setup.TankSize,
            Rs1WelderId = setup.Rs1WelderId,
            Rs2WelderId = setup.Rs2WelderId,
            Rs3WelderId = setup.Rs3WelderId,
            Rs4WelderId = setup.Rs4WelderId,
            IsComplete = isComplete
        };
    }

    public async Task<RoundSeamSetupDto> SaveSetupAsync(Guid wcId, CreateRoundSeamSetupDto dto, CancellationToken cancellationToken = default)
    {
        var setup = new RoundSeamSetup
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            TankSize = dto.TankSize,
            Rs1WelderId = dto.Rs1WelderId,
            Rs2WelderId = dto.Rs2WelderId,
            Rs3WelderId = dto.Rs3WelderId,
            Rs4WelderId = dto.Rs4WelderId,
            CreatedAt = DateTime.UtcNow
        };

        _db.RoundSeamSetups.Add(setup);
        await _db.SaveChangesAsync(cancellationToken);

        int requiredSeams = dto.TankSize <= 500 ? 2 : dto.TankSize <= 1000 ? 3 : 4;
        bool isComplete = dto.Rs1WelderId.HasValue && dto.Rs2WelderId.HasValue;
        if (requiredSeams >= 3) isComplete = isComplete && dto.Rs3WelderId.HasValue;
        if (requiredSeams >= 4) isComplete = isComplete && dto.Rs4WelderId.HasValue;

        return new RoundSeamSetupDto
        {
            Id = setup.Id,
            TankSize = setup.TankSize,
            Rs1WelderId = setup.Rs1WelderId,
            Rs2WelderId = setup.Rs2WelderId,
            Rs3WelderId = setup.Rs3WelderId,
            Rs4WelderId = setup.Rs4WelderId,
            IsComplete = isComplete
        };
    }

    public async Task<AssemblyLookupDto?> GetAssemblyByShellAsync(string serial, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .FirstOrDefaultAsync(s => s.Serial == serial, cancellationToken);
        if (sn == null) return null;

        var shellLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.FromSerialNumberId == sn.Id && t.Relationship == "shell", cancellationToken);
        if (shellLog == null || string.IsNullOrEmpty(shellLog.ToAlphaCode)) return null;

        var assembly = await _db.Assemblies
            .FirstOrDefaultAsync(a => a.AlphaCode == shellLog.ToAlphaCode, cancellationToken);
        if (assembly == null) return null;

        int roundSeamCount = assembly.TankSize <= 500 ? 2 : assembly.TankSize <= 1000 ? 3 : 4;

        return new AssemblyLookupDto
        {
            AlphaCode = assembly.AlphaCode,
            TankSize = assembly.TankSize,
            RoundSeamCount = roundSeamCount
        };
    }

    public async Task<CreateProductionRecordResponseDto> CreateRoundSeamRecordAsync(CreateRoundSeamRecordDto dto, CancellationToken cancellationToken = default)
    {
        var assemblyLookup = await GetAssemblyByShellAsync(dto.SerialNumber, cancellationToken);
        if (assemblyLookup == null)
            throw new InvalidOperationException("Shell is not part of any assembly");

        var existing = await _db.ProductionRecords
            .Include(r => r.SerialNumber)
            .Where(r => r.WorkCenterId == dto.WorkCenterId)
            .AnyAsync(r => _db.TraceabilityLogs.Any(t =>
                t.FromSerialNumberId == r.SerialNumberId &&
                t.ToAlphaCode == assemblyLookup.AlphaCode &&
                t.Relationship == "shell"), cancellationToken);

        if (existing)
            throw new InvalidOperationException($"Assembly {assemblyLookup.AlphaCode} has already been recorded at Round Seam");

        var sn = await _db.SerialNumbers.FirstAsync(s => s.Serial == dto.SerialNumber, cancellationToken);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow
        };

        _db.ProductionRecords.Add(record);

        var setup = await _db.RoundSeamSetups
            .Where(s => s.WorkCenterId == dto.WorkCenterId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (setup != null)
        {
            var welderIds = new[] { setup.Rs1WelderId, setup.Rs2WelderId, setup.Rs3WelderId, setup.Rs4WelderId };
            var characteristics = await _db.CharacteristicWorkCenters
                .Include(c => c.Characteristic)
                .Where(c => c.WorkCenterId == dto.WorkCenterId)
                .OrderBy(c => c.Characteristic.Name)
                .ToListAsync(cancellationToken);

            for (int i = 0; i < welderIds.Length && i < characteristics.Count; i++)
            {
                if (welderIds[i].HasValue)
                {
                    _db.WelderLogs.Add(new WelderLog
                    {
                        Id = Guid.NewGuid(),
                        ProductionRecordId = record.Id,
                        UserId = welderIds[i]!.Value,
                        CharacteristicId = characteristics[i].CharacteristicId
                    });
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateProductionRecordResponseDto
        {
            Id = record.Id,
            SerialNumber = dto.SerialNumber,
            Timestamp = record.Timestamp
        };
    }
}
