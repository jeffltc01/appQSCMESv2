using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public class WorkCenterMapResult
{
    public required WorkCenter WorkCenter { get; init; }
    public Guid? ProductionLineId { get; init; }
}

public static class WorkCenterMapper
{
    private static readonly Dictionary<string, string?> DataEntryTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rolls"] = "Rolls",
        ["Fitup"] = "Fitup",
        ["Hydro"] = "Hydro",
        ["Spot"] = "Spot",
        ["DataPlate"] = "DataPlate",
    };

    public static WorkCenterMapResult? Map(dynamic row,
        Dictionary<string, Guid> workCenterTypes,
        Dictionary<string, Guid> plants,
        List<ProductionLine> lines)
    {
        string siteCode = ((string)(row.SiteCode ?? "")).Trim();
        string name = (string)(row.WorkCenterName ?? "");

        if (!plants.TryGetValue(siteCode, out var plantId))
            return null;

        var wcTypeId = InferWorkCenterType(name, workCenterTypes);
        var line = lines.FirstOrDefault(l => l.PlantId == plantId);

        string? dataEntryType = ResolveDataEntryType((string?)row.DataEntryType, name);

        var wc = new WorkCenter
        {
            Id = (Guid)row.Id,
            Name = name,
            WorkCenterTypeId = wcTypeId,
            NumberOfWelders = (int)(row.NoOfWelders ?? 0),
            DataEntryType = dataEntryType,
            MaterialQueueForWCId = (Guid?)row.MaterialQueueForWCId
        };

        return new WorkCenterMapResult
        {
            WorkCenter = wc,
            ProductionLineId = line?.Id
        };
    }

    private static string? ResolveDataEntryType(string? v1Value, string wcName)
    {
        if (!string.IsNullOrWhiteSpace(v1Value) && DataEntryTypeMap.TryGetValue(v1Value, out var mapped))
            return mapped;

        return InferDataEntryTypeFromName(wcName);
    }

    private static string? InferDataEntryTypeFromName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("material") && lower.Contains("queue")) return "MatQueue-Material";
        if (lower.Contains("fitup") && lower.Contains("queue")) return "MatQueue-Fitup";
        if (lower.Contains("rt") && lower.Contains("x") || lower.Contains("real time")) return "MatQueue-Shell";
        if (lower.Contains("spot") && lower.Contains("x")) return "Spot";
        if (lower.Contains("long seam") && lower.Contains("insp")) return "Barcode-LongSeamInsp";
        if (lower.Contains("long seam")) return "Barcode-LongSeam";
        if (lower.Contains("round seam") && lower.Contains("insp")) return "Barcode-RoundSeamInsp";
        if (lower.Contains("round seam")) return "Barcode-RoundSeam";
        if (lower.Contains("fitup")) return "Fitup";
        if (lower.Contains("nameplate") || lower.Contains("data plate")) return "DataPlate";
        if (lower.Contains("hydro")) return "Hydro";
        if (lower.Contains("roll")) return "Rolls";
        return null;
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
