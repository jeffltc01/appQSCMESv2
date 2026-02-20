using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class CharacteristicWorkCenterMapper
{
    public static CharacteristicWorkCenter? Map(dynamic row)
    {
        return new CharacteristicWorkCenter
        {
            Id = (Guid)row.Id,
            CharacteristicId = (Guid)row.CharacteristicId,
            WorkCenterId = (Guid)row.WorkCenterId
        };
    }
}
