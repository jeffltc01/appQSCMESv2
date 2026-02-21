namespace MESv2.Api.Models;

public class SerialNumber
{
    public Guid Id { get; set; }
    public string Serial { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public Guid PlantId { get; set; }
    public string? Notes { get; set; }
    public Guid? MillVendorId { get; set; }
    public Guid? ProcessorVendorId { get; set; }
    public Guid? HeadsVendorId { get; set; }
    public string? CoilNumber { get; set; }
    public string? HeatNumber { get; set; }
    public string? LotNumber { get; set; }
    public Guid? ReplaceBySNId { get; set; }
    public bool Rs1Changed { get; set; }
    public bool Rs2Changed { get; set; }
    public bool Rs3Changed { get; set; }
    public bool Rs4Changed { get; set; }
    public bool IsObsolete { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public Guid? ModifiedByUserId { get; set; }

    public Product? Product { get; set; }
    public Vendor? MillVendor { get; set; }
    public Vendor? ProcessorVendor { get; set; }
    public Vendor? HeadsVendor { get; set; }
    public SerialNumber? ReplaceBySN { get; set; }
    public User? CreatedByUser { get; set; }
    public User? ModifiedByUser { get; set; }
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
