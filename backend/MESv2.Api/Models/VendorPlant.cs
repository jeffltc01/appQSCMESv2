namespace MESv2.Api.Models;

public class VendorPlant
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public Guid PlantId { get; set; }

    public Vendor Vendor { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
}
