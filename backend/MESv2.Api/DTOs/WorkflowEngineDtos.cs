namespace MESv2.Api.DTOs;

public class WorkflowStepDefinitionDto
{
    public Guid Id { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public List<string> RequiredFields { get; set; } = new();
    public List<Guid> RequiredChecklistTemplateIds { get; set; } = new();
    public string ApprovalMode { get; set; } = "None";
    public List<string> ApprovalAssignments { get; set; } = new();
    public bool AllowReject { get; set; }
    public string? OnApproveNextStepCode { get; set; }
    public string? OnRejectTargetStepCode { get; set; }
}

public class WorkflowDefinitionDto
{
    public Guid Id { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string StartStepCode { get; set; } = string.Empty;
    public List<WorkflowStepDefinitionDto> Steps { get; set; } = new();
}

public class UpsertWorkflowDefinitionDto
{
    public Guid? SourceDefinitionIdForNewVersion { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string StartStepCode { get; set; } = string.Empty;
    public List<WorkflowStepDefinitionDto> Steps { get; set; } = new();
}

public class StartWorkflowRequestDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
}

public class AdvanceStepRequestDto
{
    public Guid WorkflowInstanceId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string? PayloadJson { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class ApproveRejectRequestDto
{
    public Guid WorkflowInstanceId { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string? Comments { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class CompleteWorkItemRequestDto
{
    public Guid WorkItemId { get; set; }
    public Guid ActorUserId { get; set; }
    public string? PayloadJson { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class WorkflowInstanceDto
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public int WorkflowDefinitionVersion { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStepCode { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class WorkflowEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public class WorkItemDto
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string WorkItemType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public Guid? AssignedUserId { get; set; }
    public decimal? AssignedRoleTier { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class NotificationRuleDto
{
    public Guid Id { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string RecipientMode { get; set; } = string.Empty;
    public string RecipientConfigJson { get; set; } = "{}";
    public string TemplateKey { get; set; } = string.Empty;
    public string ClearPolicy { get; set; } = "None";
    public bool IsActive { get; set; }
}

public class HoldTagDto
{
    public Guid Id { get; set; }
    public int HoldTagNumber { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid? ProductionLineId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid SerialNumberMasterId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public Guid? DefectCodeId { get; set; }
    public string? Disposition { get; set; }
    public string BusinessStatus { get; set; } = string.Empty;
    public Guid WorkflowInstanceId { get; set; }
    public Guid? LinkedNcrId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateHoldTagRequestDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid? ProductionLineId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid SerialNumberMasterId { get; set; }
    public string? SerialNumberText { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public Guid? DefectCodeId { get; set; }
    public Guid ActorUserId { get; set; }
}

public class SetHoldTagDispositionRequestDto
{
    public Guid HoldTagId { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public string? DispositionNotes { get; set; }
    public string? ReleaseJustification { get; set; }
    public Guid? RepairInstructionTemplateId { get; set; }
    public string? RepairInstructionNotes { get; set; }
    public string? ScrapReasonCode { get; set; }
    public string? ScrapReasonText { get; set; }
    public Guid ActorUserId { get; set; }
}

public class LinkHoldTagNcrRequestDto
{
    public Guid HoldTagId { get; set; }
    public Guid LinkedNcrId { get; set; }
    public Guid ActorUserId { get; set; }
}

public class ResolveHoldTagRequestDto
{
    public Guid HoldTagId { get; set; }
    public Guid ActorUserId { get; set; }
}

public class VoidHoldTagRequestDto
{
    public Guid HoldTagId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
}

public class NcrTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVendorRelated { get; set; }
    public string? Description { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
}

public class UpsertNcrTypeRequestDto
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsVendorRelated { get; set; }
    public string? Description { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
}

public class NcrDto
{
    public Guid Id { get; set; }
    public int NcrNumber { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceEntityId { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid SubmitterUserId { get; set; }
    public Guid CoordinatorUserId { get; set; }
    public Guid NcrTypeId { get; set; }
    public string CurrentStepCode { get; set; } = string.Empty;
    public Guid WorkflowInstanceId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public DateTime DateUtc { get; set; }
}

public class CreateNcrRequestDto
{
    public string SourceType { get; set; } = "DirectQuality";
    public Guid? SourceEntityId { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid DetectedByUserId { get; set; }
    public Guid SubmitterUserId { get; set; }
    public Guid CoordinatorUserId { get; set; }
    public Guid NcrTypeId { get; set; }
    public DateTime DateUtc { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public Guid? VendorId { get; set; }
    public string? PoNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilOrSlabNumber { get; set; }
}

public class UpdateNcrDataRequestDto
{
    public Guid NcrId { get; set; }
    public Guid CoordinatorUserId { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public string? PoNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilOrSlabNumber { get; set; }
    public Guid ActorUserId { get; set; }
}

public class SubmitNcrStepRequestDto
{
    public Guid NcrId { get; set; }
    public string ActionCode { get; set; } = "Submit";
    public Guid ActorUserId { get; set; }
    public string? PayloadJson { get; set; }
}

public class NcrDecisionRequestDto
{
    public Guid NcrId { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string? Comments { get; set; }
}

public class VoidNcrRequestDto
{
    public Guid NcrId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
}

public class AddNcrAttachmentRequestDto
{
    public Guid NcrId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
}
