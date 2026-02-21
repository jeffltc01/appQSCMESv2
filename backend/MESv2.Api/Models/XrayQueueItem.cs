namespace MESv2.Api.Models;

public class XrayQueueItem
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid SerialNumberId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime CreatedAt { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public SerialNumber SerialNumber { get; set; } = null!;
    public User Operator { get; set; } = null!;
}
