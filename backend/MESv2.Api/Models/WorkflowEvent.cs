namespace MESv2.Api.Models;

public class WorkflowEvent
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public bool IsOutboxDispatched { get; set; }

    public WorkflowInstance WorkflowInstance { get; set; } = null!;
}
