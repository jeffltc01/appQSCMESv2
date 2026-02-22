using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class RealTimeXrayService : IRealTimeXrayService
{
    private readonly MesDbContext _db;
    private readonly ILogger<RealTimeXrayService> _logger;

    public RealTimeXrayService(MesDbContext db, ILogger<RealTimeXrayService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<XrayInspectionResponseDto> ProcessInspectionAsync(
        XrayInspectionRequestDto dto, CancellationToken cancellationToken = default)
    {
        var errors = new List<XrayErrorDto>();

        try
        {
            // Step 1: Look up Plant by SiteCode
            var plant = await _db.Plants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == dto.SiteCode, cancellationToken);

            if (plant is null)
            {
                errors.Add(new XrayErrorDto { Description = $"Site code '{dto.SiteCode}' not found." });
                return Fail(errors);
            }

            // Step 2: Look up SerialNumber by Serial + PlantId
            var serialNumber = await _db.SerialNumbers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.Serial == dto.SerialNumber && s.PlantId == plant.Id,
                    cancellationToken);

            if (serialNumber is null)
            {
                errors.Add(new XrayErrorDto { Description = $"Serial number '{dto.SerialNumber}' not found for site '{dto.SiteCode}'." });
                return Fail(errors);
            }

            // Step 3: Determine ProductionLineId from earliest production record for this SN
            var firstProductionRecord = await _db.ProductionRecords
                .AsNoTracking()
                .Where(r => r.SerialNumberId == serialNumber.Id)
                .OrderBy(r => r.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstProductionRecord is null)
            {
                errors.Add(new XrayErrorDto { Description = $"No prior production record found for serial '{dto.SerialNumber}'." });
                return Fail(errors);
            }

            var productionLineId = firstProductionRecord.ProductionLineId;

            // Step 4: Look up WorkCenter where DataEntryType = "RealTimeXray"
            var workCenter = await _db.WorkCenters
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.DataEntryType == "RealTimeXray", cancellationToken);

            if (workCenter is null)
            {
                errors.Add(new XrayErrorDto { Description = "No work center configured with DataEntryType 'RealTimeXray'." });
                return Fail(errors);
            }

            // Step 5: Create ProductionRecord
            var productionRecord = new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = serialNumber.Id,
                WorkCenterId = workCenter.Id,
                AssetId = null,
                ProductionLineId = productionLineId,
                OperatorId = dto.UserID,
                PlantGearId = plant.CurrentPlantGearId,
                Timestamp = DateTime.UtcNow,
            };
            _db.ProductionRecords.Add(productionRecord);

            // Step 6: Look up ControlPlan via WorkCenterProductionLine junction
            var wcpl = await _db.WorkCenterProductionLines
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    j => j.WorkCenterId == workCenter.Id && j.ProductionLineId == productionLineId,
                    cancellationToken);

            if (wcpl is null)
            {
                errors.Add(new XrayErrorDto
                {
                    Description = $"No WorkCenterProductionLine configured for WorkCenter '{workCenter.Name}' and the serial's production line."
                });
                return Fail(errors);
            }

            var controlPlan = await _db.ControlPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    cp => cp.WorkCenterProductionLineId == wcpl.Id && cp.IsGateCheck && cp.IsEnabled,
                    cancellationToken);

            if (controlPlan is null)
            {
                errors.Add(new XrayErrorDto
                {
                    Description = $"No enabled gate-check control plan found for WorkCenter '{workCenter.Name}' on this production line."
                });
                return Fail(errors);
            }

            // Step 7: Create InspectionRecord
            var resultText = dto.InspectionResult == 0 ? "Reject" : "Accept";

            var inspectionRecord = new InspectionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = serialNumber.Id,
                ProductionRecordId = productionRecord.Id,
                WorkCenterId = workCenter.Id,
                OperatorId = dto.UserID,
                Timestamp = DateTime.UtcNow,
                ControlPlanId = controlPlan.Id,
                ResultText = resultText,
                ResultNumeric = null,
            };
            _db.InspectionRecords.Add(inspectionRecord);

            // Step 8: Create DefectLogs
            if (dto.Defects.Count > 0)
            {
                var defectLocation = await _db.DefectLocations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        dl => dl.CharacteristicId == controlPlan.CharacteristicId,
                        cancellationToken);

                if (defectLocation is null)
                {
                    errors.Add(new XrayErrorDto
                    {
                        Description = $"No defect location configured for the control plan's characteristic."
                    });
                    return Fail(errors);
                }

                foreach (var defect in dto.Defects)
                {
                    _db.DefectLogs.Add(new DefectLog
                    {
                        Id = Guid.NewGuid(),
                        ProductionRecordId = productionRecord.Id,
                        InspectionRecordId = inspectionRecord.Id,
                        SerialNumberId = serialNumber.Id,
                        DefectCodeId = defect.DefectID,
                        CharacteristicId = controlPlan.CharacteristicId,
                        LocationId = defectLocation.Id,
                        LocDetails1 = defect.LocationDetails1,
                        LocDetails2 = defect.LocationDetails2,
                        LocDetailsCode = defect.LocationDetailsCode,
                        IsRepaired = false,
                        Timestamp = DateTime.UtcNow,
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "X-ray inspection saved for serial {Serial} at site {Site}: {Result}",
                dto.SerialNumber, dto.SiteCode, resultText);

            // Step 9: Return success
            return new XrayInspectionResponseDto { IsSuccess = 1, Errors = new List<XrayErrorDto>() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process X-ray inspection for serial {Serial}", dto.SerialNumber);
            errors.Add(new XrayErrorDto { Description = $"An unexpected error occurred: {ex.Message}" });
            return Fail(errors);
        }
    }

    private static XrayInspectionResponseDto Fail(List<XrayErrorDto> errors)
    {
        return new XrayInspectionResponseDto { IsSuccess = 0, Errors = errors };
    }
}
