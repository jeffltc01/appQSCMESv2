namespace MESv2.Api.Models;

public class QueueTransaction
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ItemSummary { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
}
