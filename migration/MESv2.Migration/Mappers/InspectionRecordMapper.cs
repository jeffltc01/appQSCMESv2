using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class InspectionRecordMapper
{
    public static InspectionRecord? Map(object row, Dictionary<Guid, string> snLookup)
    {
        var d = (IDictionary<string, object>)row;
        var resultNumeric = Dec(d, "ResultNumber");
        var resultText = S(d, "ResultText");
        if (string.IsNullOrWhiteSpace(resultText) && resultNumeric.HasValue)
            resultText = resultNumeric.Value > 0 ? "Accept" : "Reject";

        var serialNumberId =
            Gn(d, "SerialNumberId")
            ?? Gn(d, "ManufacturingSerialNumberId")
            ?? Gn(d, "SerialNumberMasterId")
            ?? Guid.Empty;

        return new InspectionRecord
        {
            Id = G(d, "Id"),
            SerialNumberId = serialNumberId,
            ProductionRecordId = G(d, "ManufacturingLogId"),
            WorkCenterId = G(d, "WorkCenterId"),
            OperatorId = G(d, "OperatorUserId"),
            Timestamp = Dt(d, "InspectionDate") ?? DateTime.UtcNow,
            ControlPlanId = G(d, "ControlPlanId"),
            ResultText = resultText,
            ResultNumeric = resultNumeric
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
    private static decimal? Dec(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is decimal dc ? dc : null;
}
