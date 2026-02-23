using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ProductionRecordMapper
{
    public static ProductionRecord? Map(object row, MigrationLogger log)
    {
        var d = (IDictionary<string, object>)row;

        var id = GetGuid(d, "Id");
        var snId = GetGuid(d, "SerialNumberMasterId");
        var wcId = GetGuid(d, "WorkCenterId");
        var productionLineId = GetGuid(d, "ProductionLineId");

        Guid operatorId = Guid.Empty;
        var completedBy = GetString(d, "CompletedByUserId");
        if (!string.IsNullOrEmpty(completedBy) && Guid.TryParse(completedBy, out var parsedGuid))
        {
            operatorId = parsedGuid;
        }
        else
        {
            var createdBy = GetNullableGuid(d, "CreatedByUserId");
            if (createdBy.HasValue && createdBy.Value != Guid.Empty)
                operatorId = createdBy.Value;
        }

        return new ProductionRecord
        {
            Id = id,
            SerialNumberId = snId,
            WorkCenterId = wcId,
            AssetId = GetNullableGuid(d, "AssetId"),
            ProductionLineId = productionLineId,
            OperatorId = operatorId,
            Timestamp = GetDateTime(d, "LogDateTime") ?? DateTime.UtcNow,
            ProductInId = GetNullableGuid(d, "ProductIdIn"),
            ProductOutId = GetNullableGuid(d, "ProductIdOut"),
            PlantGearId = null
        };
    }

    private static Guid GetGuid(IDictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var val) && val is Guid g) return g;
        return Guid.Empty;
    }

    private static Guid? GetNullableGuid(IDictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var val) && val is Guid g && g != Guid.Empty) return g;
        return null;
    }

    private static string? GetString(IDictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var val) && val != null && val is not DBNull) return val.ToString();
        return null;
    }

    private static DateTime? GetDateTime(IDictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var val) && val is DateTime dt) return dt;
        return null;
    }
}
