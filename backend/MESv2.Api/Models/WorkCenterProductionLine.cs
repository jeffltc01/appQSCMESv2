namespace MESv2.Api.Models;

public class WorkCenterProductionLine
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int NumberOfWelders { get; set; }
    public bool DowntimeTrackingEnabled { get; set; }
    public int DowntimeThresholdMinutes { get; set; } = 5;

    public WorkCenter WorkCenter { get; set; } = null!;
    public ProductionLine ProductionLine { get; set; } = null!;
    public ICollection<WorkCenterProductionLineDowntimeReason> WorkCenterProductionLineDowntimeReasons { get; set; } = new List<WorkCenterProductionLineDowntimeReason>();
    public ICollection<DowntimeEvent> DowntimeEvents { get; set; } = new List<DowntimeEvent>();
    public ICollection<WorkCenterCapacityTarget> CapacityTargets { get; set; } = new List<WorkCenterCapacityTarget>();
}
