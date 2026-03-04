namespace MESv2.Api.Models;

public class WorkflowStepApproval
{
    public Guid Id { get; set; }
    public Guid WorkflowStepInstanceId { get; set; }
    public string AssignmentType { get; set; } = string.Empty; // User | Role
    public Guid? AssignedUserId { get; set; }
    public decimal? AssignedRoleTier { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Approved | Cancelled
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Comments { get; set; }

    public WorkflowStepInstance WorkflowStepInstance { get; set; } = null!;
}
