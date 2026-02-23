using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class SiteScheduleMapper
{
    public static SiteSchedule? Map(dynamic row, Dictionary<string, Guid>? plantsByCode = null)
    {
        Guid plantId = Guid.Empty;
        string siteCode = (string)(row.SiteCode ?? "");
        if (plantsByCode != null && !string.IsNullOrEmpty(siteCode))
            plantsByCode.TryGetValue(siteCode, out plantId);

        return new SiteSchedule
        {
            Id = (Guid)row.Id,
            PlantId = plantId,
            Quantity = (decimal)(row.Quantity ?? 0m),
            QuantityComplete = (decimal)(row.QuantityComplete ?? 0m),
            TankSize = (int)(row.TankSize ?? 0),
            TankType = (string?)row.TankType,
            ItemNo = (string?)row.ItemNo,
            ColorName = (string?)row.ColorName,
            ColorValue = (string?)row.ColorValue,
            BuildDate = (DateTime?)row.BuildDate,
            DispatchDate = (DateTime?)row.DispatchDate,
            Comments = (string?)row.Comments,
            Status = (string?)row.Status,
            MasterScheduleId = (Guid?)row.MasterScheduleId
        };
    }
}
