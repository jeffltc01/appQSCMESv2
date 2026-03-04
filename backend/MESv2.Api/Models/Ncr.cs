namespace MESv2.Api.Models;

public class Ncr
{
    public Guid Id { get; set; }
    public int NcrNumber { get; set; }
    public string SourceType { get; set; } = "DirectQuality";
    public Guid? SourceEntityId { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid DetectedByUserId { get; set; }
    public Guid SubmitterUserId { get; set; }
    public Guid CoordinatorUserId { get; set; }
    public Guid NcrTypeId { get; set; }
    public DateTime DateUtc { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string CurrentStepCode { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid LastModifiedByUserId { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public Guid WorkflowInstanceId { get; set; }

    public Guid? VendorId { get; set; }
    public string? PoNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilOrSlabNumber { get; set; }

    public NcrType NcrType { get; set; } = null!;
    public ICollection<NcrAttachment> Attachments { get; set; } = new List<NcrAttachment>();
}
