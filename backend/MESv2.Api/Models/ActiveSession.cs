namespace MESv2.Api.Models;

public class ActiveSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? AssetId { get; set; }
    public DateTime LoginDateTime { get; set; }
    public DateTime LastHeartbeatDateTime { get; set; }

    public User User { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public ProductionLine ProductionLine { get; set; } = null!;
    public Asset? Asset { get; set; }
}
