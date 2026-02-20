using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class AssetMapper
{
    /// <summary>
    /// V1 mesAsset has SiteCode but no WorkCenterId FK. The runner passes a
    /// name-based lookup of WorkCenters so we can match by asset name convention
    /// (e.g. "Rolls 1" asset -> "Rolls 1" work center at the same site).
    /// Falls back to first WC at the same plant if no name match.
    /// </summary>
    public static Asset? Map(dynamic row,
        Dictionary<string, Guid> plantsByCode,
        List<(Guid Id, string Name, Guid PlantId)> workCenters,
        MigrationLogger log)
    {
        string siteCode = ((string)(row.SiteCode ?? "")).Trim();
        string assetName = (string)(row.AssetName ?? "");
        string assetType = ((string)(row.AssetType ?? "")).Trim();

        if (!plantsByCode.TryGetValue(siteCode, out var plantId))
        {
            log.Warn($"Asset {row.Id}: SiteCode '{siteCode}' not found in Plants");
            return null;
        }

        var siteWCs = workCenters.Where(w => w.PlantId == plantId).ToList();

        // Try matching by name: strip " Asset" suffix from asset name, match WC name
        string searchName = assetName.Replace(" Asset", "").Trim();
        var matched = siteWCs.FirstOrDefault(w =>
            w.Name.Equals(searchName, StringComparison.OrdinalIgnoreCase));

        if (matched == default && !string.IsNullOrEmpty(assetType))
        {
            matched = siteWCs.FirstOrDefault(w =>
                w.Name.Contains(assetType, StringComparison.OrdinalIgnoreCase));
        }

        Guid workCenterId;
        if (matched != default)
        {
            workCenterId = matched.Id;
        }
        else if (siteWCs.Count > 0)
        {
            workCenterId = siteWCs[0].Id;
            log.Warn($"Asset {row.Id} '{assetName}': no WC name match at site {siteCode}, assigned to '{siteWCs[0].Name}'");
        }
        else
        {
            log.Warn($"Asset {row.Id} '{assetName}': no WorkCenters found for site {siteCode}. Skipping.");
            return null;
        }

        return new Asset
        {
            Id = (Guid)row.Id,
            Name = assetName,
            WorkCenterId = workCenterId,
            LimbleIdentifier = (string?)row.MaintenanceIdentifier
        };
    }
}
