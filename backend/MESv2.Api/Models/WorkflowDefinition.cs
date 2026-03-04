namespace MESv2.Api.Models;

public class WorkflowDefinition
{
    public Guid Id { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string StartStepCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public ICollection<WorkflowStepDefinition> Steps { get; set; } = new List<WorkflowStepDefinition>();
    public ICollection<WorkflowInstance> Instances { get; set; } = new List<WorkflowInstance>();
}
