namespace MESv2.Api.Models;

public class MaterialQueueItem
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public int Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityCompleted { get; set; }
    public string? CardId { get; set; }
    public string? CardColor { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? OperatorId { get; set; }
    public string? QueueType { get; set; }
    public Guid? SerialNumberId { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public SerialNumber? SerialNumber { get; set; }
    public User? Operator { get; set; }
}
