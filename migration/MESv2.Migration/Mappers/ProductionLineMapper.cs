using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ProductionLineMapper
{
    public static ProductionLine? Map(dynamic row, Dictionary<string, Guid> plantsByCode)
    {
        string siteNo = ((string)(row.SiteNo ?? "")).Trim();
        if (!plantsByCode.TryGetValue(siteNo, out var plantId))
            return null;

        return new ProductionLine
        {
            Id = (Guid)row.Id,
            Name = (string)(row.LineName ?? ""),
            PlantId = plantId
        };
    }
}
