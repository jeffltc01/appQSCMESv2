namespace MESv2.Api.Models;

public class Plant
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "America/Chicago";
    public Guid? CurrentPlantGearId { get; set; }
    public string? LimbleLocationId { get; set; }
    public string NextTankAlphaCode { get; set; } = "AA";

    public PlantGear? CurrentPlantGear { get; set; }
    public ICollection<ProductionLine> ProductionLines { get; set; } = new List<ProductionLine>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<PlantGear> PlantGears { get; set; } = new List<PlantGear>();
    public ICollection<PlantPrinter> PlantPrinters { get; set; } = new List<PlantPrinter>();
}
