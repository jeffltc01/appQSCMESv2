namespace MESv2.Api.Models;

public class DowntimeReason
{
    public Guid Id { get; set; }
    public Guid DowntimeReasonCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool CountsAsDowntime { get; set; } = true;
    public int SortOrder { get; set; }

    public DowntimeReasonCategory DowntimeReasonCategory { get; set; } = null!;
    public ICollection<WorkCenterProductionLineDowntimeReason> WorkCenterProductionLineDowntimeReasons { get; set; } = new List<WorkCenterProductionLineDowntimeReason>();
}
