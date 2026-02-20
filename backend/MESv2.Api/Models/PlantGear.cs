namespace MESv2.Api.Models;

public class PlantGear
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid PlantId { get; set; }

    public Plant Plant { get; set; } = null!;
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
