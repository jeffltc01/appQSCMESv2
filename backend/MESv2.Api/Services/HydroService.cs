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
        var record = new HydroRecord
        {
            Id = Guid.NewGuid(),
            AssemblyAlphaCode = dto.AssemblyAlphaCode,
            NameplateSerialNumber = dto.NameplateSerialNumber,
            Result = dto.Result,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow
        };

        _db.HydroRecords.Add(record);

        // Create traceability link: assembly → finished serial
        _db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromAlphaCode = dto.AssemblyAlphaCode,
            ToAlphaCode = dto.NameplateSerialNumber,
            Relationship = "hydro-marriage",
            Timestamp = DateTime.UtcNow
        });

        if (dto.Defects.Count > 0)
        {
            foreach (var defect in dto.Defects)
            {
                _db.DefectLogs.Add(new DefectLog
                {
                    Id = Guid.NewGuid(),
                    HydroRecordId = record.Id,
                    DefectCodeId = defect.DefectCodeId,
                    CharacteristicId = defect.CharacteristicId,
                    LocationId = defect.LocationId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new HydroRecordResponseDto
        {
            Id = record.Id,
            AssemblyAlphaCode = record.AssemblyAlphaCode,
            NameplateSerialNumber = record.NameplateSerialNumber,
            Result = record.Result,
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
