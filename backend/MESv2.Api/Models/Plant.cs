namespace MESv2.Api.Models;

public class Plant
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<ProductionLine> ProductionLines { get; set; } = new List<ProductionLine>();
    public ICollection<WorkCenter> WorkCenters { get; set; } = new List<WorkCenter>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<PlantGear> PlantGears { get; set; } = new List<PlantGear>();
}
