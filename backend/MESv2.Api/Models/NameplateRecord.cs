namespace MESv2.Api.Models;

public class NameplateRecord
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime Timestamp { get; set; }

    public Product Product { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public User Operator { get; set; } = null!;
}
