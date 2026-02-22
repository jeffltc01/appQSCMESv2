namespace MESv2.Api.Models;

public class WorkCenterProductionLineDowntimeReason
{
    public Guid Id { get; set; }
    public Guid WorkCenterProductionLineId { get; set; }
    public Guid DowntimeReasonId { get; set; }

    public WorkCenterProductionLine WorkCenterProductionLine { get; set; } = null!;
    public DowntimeReason DowntimeReason { get; set; } = null!;
}
