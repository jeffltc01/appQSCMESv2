using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class BarcodeCardMapper
{
    public static BarcodeCard? Map(dynamic row)
    {
        return new BarcodeCard
        {
            Id = (Guid)row.Id,
            CardValue = (string)(row.BarcodeValue ?? ""),
            Color = (string?)row.ColorName,
            Description = null
        };
    }
}
