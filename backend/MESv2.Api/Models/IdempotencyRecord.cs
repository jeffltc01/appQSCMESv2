namespace MESv2.Api.Models;

public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
}
