namespace MESv2.Api.Models;

public class ProductionLine
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PlantId { get; set; }

    public Plant Plant { get; set; } = null!;
    public ICollection<WorkCenter> WorkCenters { get; set; } = new List<WorkCenter>();
}
