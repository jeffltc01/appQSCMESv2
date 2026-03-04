namespace MESv2.Api.Models;

public class HoldTag
{
    public Guid Id { get; set; }
    public int HoldTagNumber { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid? ProductionLineId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid SerialNumberMasterId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public Guid? DefectCodeId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid LastModifiedByUserId { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }

    public string? Disposition { get; set; }
    public Guid? DispositionSetByUserId { get; set; }
    public DateTime? DispositionSetAtUtc { get; set; }
    public string? DispositionNotes { get; set; }
    public string? ReleaseJustification { get; set; }
    public Guid? RepairInstructionTemplateId { get; set; }
    public string? RepairInstructionNotes { get; set; }
    public Guid? LinkedNcrId { get; set; }
    public string? ScrapReasonCode { get; set; }
    public string? ScrapReasonText { get; set; }
    public string BusinessStatus { get; set; } = "Open";
    public Guid WorkflowInstanceId { get; set; }
}
