using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class InspectionRecordService : IInspectionRecordService
{
    private readonly MesDbContext _db;

    public InspectionRecordService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<InspectionRecordResponseDto> CreateAsync(CreateInspectionRecordDto dto, CancellationToken cancellationToken = default)
    {
        foreach (var defect in dto.Defects)
        {
            if (defect.DefectCodeId == Guid.Empty)
                throw new ArgumentException("DefectCodeId is required for every defect entry.");
            if (defect.CharacteristicId == Guid.Empty)
                throw new ArgumentException("CharacteristicId is required for every defect entry.");
            if (defect.LocationId == Guid.Empty)
                throw new ArgumentException("LocationId is required for every defect entry.");
        }

        var sn = await _db.SerialNumbers
            .FirstOrDefaultAsync(s => s.Serial == dto.SerialNumber, cancellationToken)
            ?? throw new ArgumentException($"Serial number '{dto.SerialNumber}' not found.");

        var upstreamRecord = await _db.ProductionRecords
            .Where(r => r.SerialNumberId == sn.Id)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ArgumentException($"No production record found for serial '{dto.SerialNumber}'.");

        var inspectionProdRecord = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = null,
            ProductionLineId = upstreamRecord.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow,
            InspectionResult = dto.Defects.Count == 0 ? "Pass" : "Fail",
            PlantGearId = null,
        };
        _db.ProductionRecords.Add(inspectionProdRecord);

        var record = new InspectionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            ProductionRecordId = inspectionProdRecord.Id,
            WorkCenterId = dto.WorkCenterId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow,
            ControlPlanId = null,
            ResultText = null,
            ResultNumeric = null
        };

        _db.InspectionRecords.Add(record);

        foreach (var defect in dto.Defects)
        {
            var defectLog = new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = null,
                InspectionRecordId = record.Id,
                SerialNumberId = sn.Id,
                DefectCodeId = defect.DefectCodeId,
                CharacteristicId = defect.CharacteristicId,
                LocationId = defect.LocationId,
                LocationDetail = null,
                IsRepaired = false,
                RepairedByUserId = null,
                Timestamp = DateTime.UtcNow
            };
            _db.DefectLogs.Add(defectLog);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var defectLogs = await _db.DefectLogs
            .Include(d => d.DefectCode)
            .Include(d => d.Characteristic)
            .Include(d => d.Location)
            .Where(d => d.InspectionRecordId == record.Id)
            .ToListAsync(cancellationToken);

        return new InspectionRecordResponseDto
        {
            Id = record.Id,
            SerialNumber = sn.Serial,
            WorkCenterId = record.WorkCenterId,
            OperatorId = record.OperatorId,
            Timestamp = record.Timestamp,
            Defects = defectLogs.Select(d => new DefectEntryResponseDto
            {
                DefectCodeId = d.DefectCodeId,
                DefectCodeName = d.DefectCode?.Name,
                CharacteristicId = d.CharacteristicId,
                CharacteristicName = d.Characteristic?.Name,
                LocationId = d.LocationId,
                LocationName = d.Location?.Name
            }).ToList()
        };
    }
}
