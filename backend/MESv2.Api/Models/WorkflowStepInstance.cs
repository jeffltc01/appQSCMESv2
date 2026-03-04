namespace MESv2.Api.Models;

public class WorkflowStepInstance
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public string? Comments { get; set; }

    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public ICollection<WorkflowStepApproval> Approvals { get; set; } = new List<WorkflowStepApproval>();
}
