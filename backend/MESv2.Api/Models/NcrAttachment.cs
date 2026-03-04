namespace MESv2.Api.Models;

public class NcrAttachment
{
    public Guid Id { get; set; }
    public Guid NcrId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; }

    public Ncr Ncr { get; set; } = null!;
}
