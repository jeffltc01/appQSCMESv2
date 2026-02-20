using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class InspectionRecordMapper
{
    /// <summary>
    /// Maps a v1 mesManufacturingInspectionsLog row (joined with mesManufacturingLog)
    /// to a v2 InspectionRecord.
    /// </summary>
    public static InspectionRecord? Map(dynamic row, Dictionary<Guid, string> snLookup)
    {
        Guid? serialNumberId = (Guid?)row.SerialNumberId;
        string serialNumber = "";
        if (serialNumberId.HasValue && snLookup.TryGetValue(serialNumberId.Value, out var sn))
            serialNumber = sn;

        return new InspectionRecord
        {
            Id = (Guid)row.Id,
            SerialNumber = serialNumber,
            WorkCenterId = (Guid?)row.WorkCenterId ?? Guid.Empty,
            OperatorId = (Guid?)row.OperatorUserId ?? Guid.Empty,
            Timestamp = DateTime.UtcNow,
            ControlPlanId = (Guid?)row.ControlPlanId,
            ResultText = (string?)row.ResultText,
            ResultNumeric = (decimal?)row.ResultNumber
        };
    }
}
