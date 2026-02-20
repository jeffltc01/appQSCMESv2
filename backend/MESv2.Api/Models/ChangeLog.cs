namespace MESv2.Api.Models;

public class ChangeLog
{
    public Guid Id { get; set; }
    public string RecordTable { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public DateTime ChangeDateTime { get; set; }
    public Guid ChangeByUserId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
    public Guid? FromValueId { get; set; }
    public Guid? ToValueId { get; set; }

    public User ChangeByUser { get; set; } = null!;
}
