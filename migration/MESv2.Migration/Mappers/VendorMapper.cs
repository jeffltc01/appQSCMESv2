using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class VendorMapper
{
    public static Vendor? Map(dynamic row)
    {
        // v1 uses IsMill/IsProcessor booleans; v2 uses VendorType string
        bool isMill = (bool?)row.IsMill ?? false;
        bool isProcessor = (bool?)row.IsProcessor ?? false;
        string vendorType = (string?)row.VendorType ?? "";
        if (string.IsNullOrEmpty(vendorType))
        {
            if (isMill) vendorType = "mill";
            else if (isProcessor) vendorType = "processor";
            else vendorType = "head";
        }

        return new Vendor
        {
            Id = (Guid)row.Id,
            Name = (string)(row.VendorName ?? ""),
            VendorType = vendorType,
            SiteCode = (string?)row.SiteCode,
            IsActive = true
        };
    }
}
