namespace MESv2.Api.DTOs;

public class QueueAdvanceResponseDto
{
    public string ShellSize { get; set; } = string.Empty;
    public string HeatNumber { get; set; } = string.Empty;
    public string CoilNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityCompleted { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
}

public class KanbanCardLookupDto
{
    public string HeatNumber { get; set; } = string.Empty;
    public string CoilNumber { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public string? CardColor { get; set; }
    public int? TankSize { get; set; }
}
