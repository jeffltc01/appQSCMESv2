namespace MESv2.Api.Models;

public class PrintLog
{
    public Guid Id { get; set; }
    public Guid SerialNumberId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid RequestedByUserId { get; set; }

    public SerialNumber SerialNumber { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
}
