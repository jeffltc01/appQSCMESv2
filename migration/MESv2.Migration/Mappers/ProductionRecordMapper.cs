using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ProductionRecordMapper
{
    public static ProductionRecord? Map(dynamic row, Dictionary<string, Guid> usersByEmpNo, MigrationLogger log)
    {
        // CompletedByUserId in v1 is nvarchar(50) holding a GUID as text
        Guid operatorId = Guid.Empty;
        string? completedBy = row.CompletedByUserId as string;

        if (!string.IsNullOrEmpty(completedBy) && Guid.TryParse(completedBy, out var parsedGuid))
        {
            operatorId = parsedGuid;
        }
        else
        {
            Guid? createdByUserId = (Guid?)row.CreatedByUserId;
            if (createdByUserId.HasValue && createdByUserId.Value != Guid.Empty)
                operatorId = createdByUserId.Value;
            else
                log.Warn($"ManufacturingLog {row.Id}: CompletedByUserId is empty/null and no CreatedByUserId fallback");
        }

        return new ProductionRecord
        {
            Id = (Guid)row.Id,
            SerialNumberId = (Guid)row.SerialNumberMasterId,
            WorkCenterId = (Guid?)row.WorkCenterId ?? Guid.Empty,
            AssetId = (Guid?)row.AssetId,
            ProductionLineId = (Guid?)row.ProductionLineId ?? Guid.Empty,
            OperatorId = operatorId,
            Timestamp = (DateTime?)row.LogDateTime ?? DateTime.UtcNow,
            ProductInId = (Guid?)row.ProductIdIn,
            ProductOutId = (Guid?)row.ProductIdOut,
            PlantGearId = (Guid?)row.PlantGearId,
            InspectionResult = null
        };
    }
}
