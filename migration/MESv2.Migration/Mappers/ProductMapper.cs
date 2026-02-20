using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ProductMapper
{
    public static Product? Map(dynamic row)
    {
        // v1 ProductTypeId is stored as nvarchar(50) but holds a GUID string
        Guid productTypeId = Guid.Empty;
        string? ptStr = row.ProductTypeId as string;
        if (!string.IsNullOrEmpty(ptStr))
            Guid.TryParse(ptStr, out productTypeId);

        return new Product
        {
            Id = (Guid)row.Id,
            ProductNumber = (string)(row.ProductName ?? row.ProductNumber ?? ""),
            TankSize = (int)(row.TankSize ?? 0),
            TankType = (string)(row.TankType ?? ""),
            SageItemNumber = (string?)row.SageItemNo,
            NameplateNumber = (string?)row.NameplateProductNo,
            SiteNumbers = (string?)row.SiteNumbers,
            ProductTypeId = productTypeId
        };
    }
}
