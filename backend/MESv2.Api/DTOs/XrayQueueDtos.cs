namespace MESv2.Api.DTOs;

public class XrayQueueItemDto
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddXrayQueueItemDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public Guid OperatorId { get; set; }
}
