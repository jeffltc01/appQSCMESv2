using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class InspectionRecordService : IInspectionRecordService
{
    private static readonly Dictionary<string, HashSet<string>> ValidResultValues = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PassFail"] = new(StringComparer.Ordinal) { "Pass", "Fail" },
        ["AcceptReject"] = new(StringComparer.Ordinal) { "Accept", "Reject" },
        ["GoNoGo"] = new(StringComparer.Ordinal) { "Go", "NoGo" },
    };

    private readonly MesDbContext _db;

    public InspectionRecordService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<InspectionRecordResponseDto> CreateAsync(CreateInspectionRecordDto dto, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .FirstOrDefaultAsync(s => s.Serial == dto.SerialNumber, cancellationToken)
            ?? throw new ArgumentException($"Serial number '{dto.SerialNumber}' not found.");

        var upstreamRecord = await _db.ProductionRecords
            .Where(r => r.SerialNumberId == sn.Id)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ArgumentException($"No production record found for serial '{dto.SerialNumber}'.");

        foreach (var result in dto.Results)
        {
            var cp = await _db.ControlPlans.FindAsync(new object[] { result.ControlPlanId }, cancellationToken)
                ?? throw new ArgumentException($"ControlPlan '{result.ControlPlanId}' not found.");

            if (!ValidResultValues.TryGetValue(cp.ResultType, out var allowed) || !allowed.Contains(result.ResultText))
                throw new ArgumentException(
                    $"Invalid ResultText '{result.ResultText}' for ResultType '{cp.ResultType}'. " +
                    $"Valid values: {(ValidResultValues.TryGetValue(cp.ResultType, out var v) ? string.Join(", ", v) : "none")}");
        }

        foreach (var defect in dto.Defects)
        {
            if (defect.DefectCodeId == Guid.Empty)
                throw new ArgumentException("DefectCodeId is required for every defect entry.");
            if (defect.CharacteristicId == Guid.Empty)
                throw new ArgumentException("CharacteristicId is required for every defect entry.");
            if (defect.LocationId == Guid.Empty)
                throw new ArgumentException("LocationId is required for every defect entry.");
        }

        ProductionRecord inspectionProdRecord;
        if (dto.ProductionRecordId.HasValue)
        {
            inspectionProdRecord = await _db.ProductionRecords
                .FirstOrDefaultAsync(r => r.Id == dto.ProductionRecordId.Value, cancellationToken)
                ?? throw new ArgumentException($"ProductionRecord '{dto.ProductionRecordId.Value}' not found.");
        }
        else
        {
            inspectionProdRecord = new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = sn.Id,
                WorkCenterId = dto.WorkCenterId,
                AssetId = null,
                ProductionLineId = upstreamRecord.ProductionLineId,
                OperatorId = dto.OperatorId,
                Timestamp = DateTime.UtcNow,
                PlantGearId = null,
            };
            _db.ProductionRecords.Add(inspectionProdRecord);
        }

        var firstRecordId = Guid.Empty;
        foreach (var result in dto.Results)
        {
            var record = new InspectionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = sn.Id,
                ProductionRecordId = inspectionProdRecord.Id,
                WorkCenterId = dto.WorkCenterId,
                OperatorId = dto.OperatorId,
                Timestamp = DateTime.UtcNow,
                ControlPlanId = result.ControlPlanId,
                ResultText = result.ResultText,
                ResultNumeric = null
            };
            _db.InspectionRecords.Add(record);
            if (firstRecordId == Guid.Empty) firstRecordId = record.Id;
        }

        foreach (var defect in dto.Defects)
        {
            _db.DefectLogs.Add(new DefectLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = inspectionProdRecord.Id,
                SerialNumberId = sn.Id,
                DefectCodeId = defect.DefectCodeId,
                CharacteristicId = defect.CharacteristicId,
                LocationId = defect.LocationId,
                LocationDetail = null,
                IsRepaired = false,
                RepairedByUserId = null,
                Timestamp = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var defectLogs = await _db.DefectLogs
            .Include(d => d.DefectCode)
            .Include(d => d.Characteristic)
            .Include(d => d.Location)
            .Where(d => d.ProductionRecordId == inspectionProdRecord.Id)
            .ToListAsync(cancellationToken);

        return new InspectionRecordResponseDto
        {
            Id = firstRecordId,
            SerialNumber = sn.Serial,
            WorkCenterId = dto.WorkCenterId,
            OperatorId = dto.OperatorId,
            Timestamp = inspectionProdRecord.Timestamp,
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
