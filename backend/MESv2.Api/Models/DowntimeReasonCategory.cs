namespace MESv2.Api.Models;

public class DowntimeReasonCategory
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Plant Plant { get; set; } = null!;
    public ICollection<DowntimeReason> DowntimeReasons { get; set; } = new List<DowntimeReason>();
}
