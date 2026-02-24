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
            // v1 stores parent in SerialNumberMasterId and component in SerialNumberComponentId.
            // v2 traversal expects edge direction component -> parent.
            FromSerialNumberId = Gn(d, "SerialNumberComponentId"),
            ToSerialNumberId = Gn(d, "SerialNumberMasterId"),
            ProductionRecordId = Gn(d, "ManufacturingLogId"),
            Relationship = InferRelationship(d),
            Quantity = qty,
            TankLocation = S(d, "TankLocation"),
            Timestamp = Dt(d, "CreatedDateTime") ?? DateTime.UtcNow
        };
    }

    private static string InferRelationship(IDictionary<string, object> d)
    {
        var explicitRelationship =
            S(d, "Relationship")
            ?? S(d, "TraceabilityType")
            ?? S(d, "TraceType")
            ?? S(d, "LinkType");

        if (!string.IsNullOrWhiteSpace(explicitRelationship))
            return explicitRelationship!;

        var location = S(d, "TankLocation");
        if (string.IsNullOrWhiteSpace(location))
            return "component";

        if (location.Equals("Head 1", StringComparison.OrdinalIgnoreCase))
            return "leftHead";
        if (location.Equals("Head 2", StringComparison.OrdinalIgnoreCase))
            return "rightHead";
        if (location.StartsWith("Shell", StringComparison.OrdinalIgnoreCase))
            return "shell";

        return "component";
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
