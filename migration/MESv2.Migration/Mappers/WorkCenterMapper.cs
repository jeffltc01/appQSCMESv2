using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class WorkCenterMapper
{
    private static readonly Dictionary<string, string> DataEntryTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rolls"] = "Rolls",
        ["LS"] = "standard",
        ["Barcode"] = "Barcode",
        ["Fitup"] = null!,
        ["Hydro"] = null!,
        ["Spot"] = null!,
        ["DataPlate"] = null!,
    };

    public static WorkCenter? Map(dynamic row,
        Dictionary<string, Guid> workCenterTypes,
        Dictionary<string, Guid> plants,
        List<ProductionLine> lines)
    {
        string siteCode = ((string)(row.SiteCode ?? "")).Trim();
        string name = (string)(row.WorkCenterName ?? "");

        if (!plants.TryGetValue(siteCode, out var plantId))
            return null;

        // Infer WorkCenterType from name
        var wcTypeId = InferWorkCenterType(name, workCenterTypes);

        // Find the first production line for this plant
        var line = lines.FirstOrDefault(l => l.PlantId == plantId);

        return new WorkCenter
        {
            Id = (Guid)row.Id,
            Name = name,
            PlantId = plantId,
            WorkCenterTypeId = wcTypeId,
            ProductionLineId = line?.Id,
            NumberOfWelders = (int)(row.NoOfWelders ?? 0),
            DataEntryType = (string?)row.DataEntryType,
            MaterialQueueForWCId = (Guid?)row.MaterialQueueForWCId
        };
    }

    private static Guid InferWorkCenterType(string name, Dictionary<string, Guid> types)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("material") || lower.Contains("queue"))
            return types.GetValueOrDefault("Material Queue", Guid.Empty);
        if (lower.Contains("spot") && lower.Contains("x-ray"))
            return types.GetValueOrDefault("Spot X-Ray", Guid.Empty);
        if (lower.Contains("x-ray") || lower.Contains("xray") || lower.Contains("rt "))
            return types.GetValueOrDefault("X-Ray", Guid.Empty);
        if (lower.Contains("long seam") && lower.Contains("insp"))
            return types.GetValueOrDefault("Inspection", Guid.Empty);
        if (lower.Contains("round seam") && lower.Contains("insp"))
            return types.GetValueOrDefault("Inspection", Guid.Empty);
        if (lower.Contains("long seam"))
            return types.GetValueOrDefault("Long Seam", Guid.Empty);
        if (lower.Contains("round seam"))
            return types.GetValueOrDefault("Round Seam", Guid.Empty);
        if (lower.Contains("fitup"))
            return types.GetValueOrDefault("Fitup", Guid.Empty);
        if (lower.Contains("nameplate"))
            return types.GetValueOrDefault("Nameplate", Guid.Empty);
        if (lower.Contains("hydro"))
            return types.GetValueOrDefault("Hydro", Guid.Empty);
        if (lower.Contains("roll"))
            return types.GetValueOrDefault("Rolls", Guid.Empty);
        return types.Values.FirstOrDefault();
    }
}
