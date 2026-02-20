namespace MESv2.Api.Models;

public class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public string? LimbleIdentifier { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public ProductionLine ProductionLine { get; set; } = null!;
}
