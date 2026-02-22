using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class CharacteristicMapper
{
    public static Characteristic? Map(dynamic row)
    {
        // v1 has both ProductId and ProductTypeId; v2 drops ProductId
        Guid? productTypeId = (Guid?)row.ProductTypeId;

        int? lookupId = (int?)row.LookupId;

        return new Characteristic
        {
            Id = (Guid)row.Id,
            Code = lookupId?.ToString() ?? "",
            Name = (string)(row.Characteristic ?? ""),
            SpecHigh = (decimal?)row.SpecLimitHigh,
            SpecLow = (decimal?)row.SpecLimitLow,
            SpecTarget = (decimal?)row.SpecLimitTarget,
            ProductTypeId = productTypeId
        };
    }
}
