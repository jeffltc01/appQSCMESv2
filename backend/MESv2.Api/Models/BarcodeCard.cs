namespace MESv2.Api.Models;

public class BarcodeCard
{
    public Guid Id { get; set; }
    public string CardValue { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}
