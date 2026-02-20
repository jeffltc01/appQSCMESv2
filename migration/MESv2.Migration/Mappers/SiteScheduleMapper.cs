using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class SiteScheduleMapper
{
    public static SiteSchedule? Map(dynamic row)
    {
        return new SiteSchedule
        {
            Id = (Guid)row.Id,
            SiteCode = (string)(row.SiteCode ?? ""),
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
