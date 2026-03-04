namespace MESv2.Api.Models;

public class WorkflowInstance
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public int WorkflowDefinitionVersion { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Status { get; set; } = "Draft";
    public string CurrentStepCode { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    public ICollection<WorkflowStepInstance> StepInstances { get; set; } = new List<WorkflowStepInstance>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public ICollection<WorkflowEvent> Events { get; set; } = new List<WorkflowEvent>();
}
