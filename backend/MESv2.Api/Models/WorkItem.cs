namespace MESv2.Api.Models;

public class WorkItem
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string WorkItemType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public Guid? AssignedUserId { get; set; }
    public decimal? AssignedRoleTier { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }

    public WorkflowInstance WorkflowInstance { get; set; } = null!;
}
