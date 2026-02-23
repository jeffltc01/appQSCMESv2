using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class MaterialQueueMapper
{
    public static MaterialQueueItem? Map(dynamic row)
    {
        var d = (IDictionary<string, object>)row;

        int pos = 0;
        if (d.TryGetValue("QueuePosition", out var qp))
        {
            if (qp is decimal dpv) pos = (int)dpv;
            else if (qp is int ipv) pos = ipv;
        }

        int qty = 0;
        if (d.TryGetValue("Quantity", out var qv))
        {
            if (qv is decimal dqv) qty = (int)dqv;
            else if (qv is int iqv) qty = iqv;
        }

        return new MaterialQueueItem
        {
            Id = G(d, "Id"),
            WorkCenterId = G(d, "WorkCenterId"),
            Position = pos,
            Status = MapStatus(S(d, "QueueStatus")),
            Quantity = qty,
            QuantityCompleted = 0,
            CardId = null,
            CardColor = null,
            CreatedAt = Dt(d, "CreateDateTime") ?? DateTime.UtcNow,
            OperatorId = null,
            QueueType = null,
            SerialNumberId = Gn(d, "SerialNumberMasterId")
        };
    }

    private static string MapStatus(string? v1Status)
    {
        if (string.IsNullOrEmpty(v1Status)) return "queued";
        return v1Status.ToLowerInvariant() switch
        {
            "active" => "active",
            "completed" or "complete" => "completed",
            _ => "queued"
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
