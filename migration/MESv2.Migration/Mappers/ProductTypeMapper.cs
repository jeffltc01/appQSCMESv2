using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ProductTypeMapper
{
    public static ProductType? Map(dynamic row)
    {
        return new ProductType
        {
            Id = (Guid)row.Id,
            Name = (string)(row.ProductTypeName ?? ""),
            SystemTypeName = (string?)row.SystemTypeName
        };
    }
}
