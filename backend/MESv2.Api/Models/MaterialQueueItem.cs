namespace MESv2.Api.Models;

public class MaterialQueueItem
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public int Position { get; set; }
    public string Status { get; set; } = string.Empty; // queued, active, completed
    public string ProductDescription { get; set; } = string.Empty;
    public string? ShellSize { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public string CoilNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? CardId { get; set; }
    public string? CardColor { get; set; }
    public DateTime CreatedAt { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
}
