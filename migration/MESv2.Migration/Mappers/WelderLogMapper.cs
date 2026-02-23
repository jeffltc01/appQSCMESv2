using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class WelderLogMapper
{
    public static WelderLog? Map(dynamic row)
    {
        var d = (IDictionary<string, object>)row;

        return new WelderLog
        {
            Id = G(d, "Id"),
            ProductionRecordId = G(d, "ManufacturingLogId"),
            UserId = G(d, "WelderUserId"),
            CharacteristicId = Gn(d, "CharacteristicId")
        };
    }

    private static Guid G(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g ? g : Guid.Empty;
    private static Guid? Gn(IDictionary<string, object> d, string k) =>
        d.TryGetValue(k, out var v) && v is Guid g && g != Guid.Empty ? g : null;
}
