using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class DefectLogMapper
{
    public static DefectLog? Map(dynamic row, Dictionary<Guid, string> snLookup)
    {
        Guid? snId = (Guid?)row.SerialNumberMasterId;
        string serialNumber = "";
        if (snId.HasValue && snLookup.TryGetValue(snId.Value, out var sn))
            serialNumber = sn;

        Guid? repairedByUserId = (Guid?)row.RepairedByUserId;
        bool isRepaired = repairedByUserId.HasValue && repairedByUserId.Value != Guid.Empty;

        return new DefectLog
        {
            Id = (Guid)row.Id,
            ProductionRecordId = (Guid?)row.ManufacturingLogId,
            InspectionRecordId = null,
            HydroRecordId = null,
            SerialNumber = serialNumber,
            DefectCodeId = (Guid)row.DefectId,
            CharacteristicId = (Guid)row.CharacteristicId,
            LocationId = (Guid)row.DefectLocationId,
            LocationDetail = (string?)row.LocDetailsCode,
            IsRepaired = isRepaired,
            RepairedByUserId = isRepaired ? repairedByUserId : null,
            CreatedAt = (DateTime?)row.CreatedDateTime ?? DateTime.UtcNow,
            Timestamp = (DateTime?)row.CreatedDateTime ?? DateTime.UtcNow
        };
    }
}
