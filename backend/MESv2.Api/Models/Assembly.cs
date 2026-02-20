namespace MESv2.Api.Models;

public class Assembly
{
    public Guid Id { get; set; }
    public string AlphaCode { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid AssetId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsActive { get; set; } = true;

    public WorkCenter WorkCenter { get; set; } = null!;
    public Asset Asset { get; set; } = null!;
    public ProductionLine ProductionLine { get; set; } = null!;
    public User Operator { get; set; } = null!;
}
