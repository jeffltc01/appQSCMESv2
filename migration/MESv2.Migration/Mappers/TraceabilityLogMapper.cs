using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class TraceabilityLogMapper
{
    public static TraceabilityLog? Map(dynamic row)
    {
        var d = (IDictionary<string, object>)row;

        int? qty = null;
        if (d.TryGetValue("Quantity", out var qv))
        {
            if (qv is decimal dec) qty = (int)dec;
            else if (qv is int i) qty = i;
        }

        return new TraceabilityLog
        {
            Id = G(d, "Id"),
            FromSerialNumberId = Gn(d, "SerialNumberMasterId"),
            ToSerialNumberId = Gn(d, "SerialNumberComponentId"),
            ProductionRecordId = Gn(d, "ManufacturingLogId"),
            Relationship = "component",
            Quantity = qty,
            TankLocation = S(d, "TankLocation"),
            Timestamp = DateTime.UtcNow
        };
    }

    private static Guid G(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g ? g : Guid.Empty;
    private static Guid? Gn(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g && g != Guid.Empty ? g : null;
    private static string? S(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v != null && v is not DBNull ? v.ToString() : null;
}
