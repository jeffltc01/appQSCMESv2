namespace MESv2.Api.Models;

public class PlantPrinter
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string PrintLocation { get; set; } = string.Empty;

    public Plant Plant { get; set; } = null!;
}
