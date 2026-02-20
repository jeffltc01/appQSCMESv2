namespace MESv2.Api.Models;

public class Vendor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty; // mill, processor, head
    public string? SiteCode { get; set; }
    public bool IsActive { get; set; } = true;
}
