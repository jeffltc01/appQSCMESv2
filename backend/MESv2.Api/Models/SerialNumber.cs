namespace MESv2.Api.Models;

public class SerialNumber
{
    public Guid Id { get; set; }
    public string Serial { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product? Product { get; set; }
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
