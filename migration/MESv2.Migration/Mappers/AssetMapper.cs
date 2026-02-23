using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class AssetMapper
{
    public static Asset? Map(object row,
        Dictionary<string, Guid> plantsByCode,
        List<(Guid Id, string Name)> allWorkCenters,
        Dictionary<Guid, Guid> plantToProductionLine,
        MigrationLogger log)
    {
        var d = (IDictionary<string, object>)row;
        var siteCodeRaw = d.TryGetValue("SiteCode", out var scv) ? scv : null;
        Guid plantId = Guid.Empty;

        if (siteCodeRaw is Guid siteGuid)
        {
            plantId = siteGuid;
            if (!plantsByCode.Values.Contains(plantId))
            {
                log.Warn($"Asset {Get(d, "Id")}: SiteCode Guid '{siteGuid}' not found in Plants");
                return null;
            }
        }
        else
        {
            string siteCode = (siteCodeRaw?.ToString() ?? "").Trim();
            if (!plantsByCode.TryGetValue(siteCode, out plantId))
            {
                log.Warn($"Asset {Get(d, "Id")}: SiteCode '{siteCode}' not found in Plants");
                return null;
            }
        }

        string assetName = Get(d, "AssetName")?.ToString() ?? "";
        string assetType = (Get(d, "AssetType")?.ToString() ?? "").Trim();

        string searchName = assetName.Replace(" Asset", "").Trim();
        var matched = allWorkCenters.FirstOrDefault(w =>
            w.Name.Equals(searchName, StringComparison.OrdinalIgnoreCase));

        if (matched == default && !string.IsNullOrEmpty(assetType))
        {
            matched = allWorkCenters.FirstOrDefault(w =>
                w.Name.Contains(assetType, StringComparison.OrdinalIgnoreCase));
        }

        Guid workCenterId;
        if (matched != default)
        {
            workCenterId = matched.Id;
        }
        else if (allWorkCenters.Count > 0)
        {
            workCenterId = allWorkCenters[0].Id;
            log.Warn($"Asset {Get(d, "Id")} '{assetName}': no WC name match, assigned to '{allWorkCenters[0].Name}'");
        }
        else
        {
            log.Warn($"Asset {Get(d, "Id")} '{assetName}': no WorkCenters found. Skipping.");
            return null;
        }

        if (!plantToProductionLine.TryGetValue(plantId, out var productionLineId))
        {
            log.Warn($"Asset {Get(d, "Id")} '{assetName}': no ProductionLine for Plant {plantId}. Skipping.");
            return null;
        }

        var idRaw = d.TryGetValue("Id", out var idv) ? idv : null;

        return new Asset
        {
            Id = idRaw is Guid g ? g : Guid.NewGuid(),
            Name = assetName,
            WorkCenterId = workCenterId,
            ProductionLineId = productionLineId,
            LimbleIdentifier = Get(d, "MaintenanceIdentifier")?.ToString()
        };
    }

    private static object? Get(IDictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var val) && val is not DBNull) return val;
        return null;
    }
}
