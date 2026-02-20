using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class PlantMapper
{
    private static readonly Dictionary<string, string> TimeZoneMap = new()
    {
        ["000"] = "America/New_York",
        ["600"] = "America/Los_Angeles",
        ["700"] = "America/Denver"
    };

    public static Plant? Map(dynamic row)
    {
        string code = ((string)(row.SiteNoFull ?? "")).Trim();
        return new Plant
        {
            Id = (Guid)row.Id,
            Code = code,
            Name = (string)(row.SiteName ?? ""),
            TimeZoneId = TimeZoneMap.GetValueOrDefault(code, "America/Chicago"),
            CurrentPlantGearId = (Guid?)row.CurrentPlantGearId
        };
    }
}
