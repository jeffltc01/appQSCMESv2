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

    public Guid? ProductId { get; set; }
    public Guid? VendorMillId { get; set; }
    public Guid? VendorProcessorId { get; set; }
    public Guid? VendorHeadId { get; set; }
    public string? LotNumber { get; set; }
    public string? CoilSlabNumber { get; set; }
    public Guid? OperatorId { get; set; }
    public string? QueueType { get; set; } // rolls, fitup
    public Guid? SerialNumberId { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public SerialNumber? SerialNumber { get; set; }
}
