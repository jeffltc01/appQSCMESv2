using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class WelderLogMapper
{
    public static WelderLog? Map(dynamic row)
    {
        return new WelderLog
        {
            Id = (Guid)row.Id,
            ProductionRecordId = (Guid)row.ManufacturingLogId,
            UserId = (Guid)row.WelderUserId,
            CharacteristicId = (Guid?)row.CharacteristicId
        };
    }
}
