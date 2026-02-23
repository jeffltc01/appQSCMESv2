using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class DefectLogMapper
{
    public static DefectLog? Map(object row, Dictionary<Guid, string> snLookup)
    {
        var d = (IDictionary<string, object>)row;

        var repairedBy = Gn(d, "RepairedByUserId");
        bool isRepaired = repairedBy.HasValue && repairedBy.Value != Guid.Empty;

        return new DefectLog
        {
            Id = G(d, "Id"),
            ProductionRecordId = Gn(d, "ManufacturingLogId"),
            SerialNumberId = G(d, "SerialNumberMasterId"),
            DefectCodeId = G(d, "DefectId"),
            CharacteristicId = G(d, "CharacteristicId"),
            LocationId = G(d, "DefectLocationId"),
            LocDetailsCode = S(d, "LocDetailsCode"),
            IsRepaired = isRepaired,
            RepairedByUserId = isRepaired ? repairedBy : null,
            CreatedAt = Dt(d, "CreatedDateTime") ?? DateTime.UtcNow,
            Timestamp = Dt(d, "CreatedDateTime") ?? DateTime.UtcNow
        };
    }

    private static Guid G(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g ? g : Guid.Empty;
    private static Guid? Gn(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g && g != Guid.Empty ? g : null;
    private static string? S(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v != null && v is not DBNull ? v.ToString() : null;
    private static DateTime? Dt(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is DateTime dt ? dt : null;
}
