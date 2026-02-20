namespace MESv2.Api.DTOs;

public class CreateNameplateRecordDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid OperatorId { get; set; }
}

public class NameplateRecordResponseDto
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public DateTime Timestamp { get; set; }
}
