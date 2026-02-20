namespace MESv2.Api.Models;

public class WorkCenterType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<WorkCenter> WorkCenters { get; set; } = new List<WorkCenter>();
}
