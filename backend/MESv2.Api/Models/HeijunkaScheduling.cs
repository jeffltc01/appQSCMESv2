namespace MESv2.Api.Models;

public class Schedule
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime WeekStartDateLocal { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public int FreezeHours { get; set; } = 24;
    public int RevisionNumber { get; set; } = 1;
    public byte[]? RowVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? LastModifiedByUserId { get; set; }

    public ICollection<ScheduleLine> Lines { get; set; } = new List<ScheduleLine>();
    public ICollection<ScheduleChangeLog> ChangeLogs { get; set; } = new List<ScheduleChangeLog>();
}

public class ScheduleLine
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public DateTime PlannedDateLocal { get; set; }
    public int? SequenceIndex { get; set; }
    public Guid? ProductId { get; set; }
    public string PlanningClass { get; set; } = "Wheel";
    public decimal PlannedQty { get; set; }
    public DateTime? PlannedStartLocal { get; set; }
    public DateTime? PlannedEndLocal { get; set; }
    public string? PolicySnapshotJson { get; set; }
    public string? LoadGroupId { get; set; }
    public DateTime? DispatchDateLocal { get; set; }
    public string? MesPlanningGroupId { get; set; }
    public string? PlanningResourceId { get; set; }
    public string? ExecutionResourceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }

    public Schedule Schedule { get; set; } = null!;
}

public class ScheduleChangeLog
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid? ScheduleLineId { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }
    public string ChangeReasonCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }

    public Schedule Schedule { get; set; } = null!;
}

public class ErpSalesOrderDemandRaw
{
    public Guid Id { get; set; }
    public string ErpSalesOrderId { get; set; } = string.Empty;
    public string ErpSalesOrderLineId { get; set; } = string.Empty;
    public string ErpSkuCode { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string ErpLoadNumberRaw { get; set; } = string.Empty;
    public DateTime DispatchDateLocal { get; set; }
    public decimal RequiredQty { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime SourceExtractedAtUtc { get; set; }
    public DateTime? ErpLastChangedAtUtc { get; set; }
    public string? SourceBatchId { get; set; }
    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ErpDemandSnapshot
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public string ErpSalesOrderId { get; set; } = string.Empty;
    public string ErpSalesOrderLineId { get; set; } = string.Empty;
    public string ErpLoadNumberRaw { get; set; } = string.Empty;
    public string LoadGroupId { get; set; } = string.Empty;
    public int LoadLegIndex { get; set; }
    public DateTime DispatchDateLocal { get; set; }
    public Guid? ProductId { get; set; }
    public string ErpSkuCode { get; set; } = string.Empty;
    public string? MesPlanningGroupId { get; set; }
    public decimal RequiredQty { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime? ErpLastChangedAtUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ErpSkuPlanningGroupMapping
{
    public Guid Id { get; set; }
    public string ErpSkuCode { get; set; } = string.Empty;
    public string MesPlanningGroupId { get; set; } = string.Empty;
    public string? SiteCode { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid MappingOwnerUserId { get; set; }
    public DateTime? LastReviewedAtUtc { get; set; }
    public bool RequiresReview { get; set; }
}

public class UnmappedDemandException
{
    public Guid Id { get; set; }
    public string ErpSkuCode { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string LoadGroupId { get; set; } = string.Empty;
    public DateTime DispatchDateLocal { get; set; }
    public decimal RequiredQty { get; set; }
    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;
    public string ExceptionStatus { get; set; } = "Open";
    public string? ResolutionNotes { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class ScheduleExecutionEvent
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public string? ExecutionResourceId { get; set; }
    public Guid? ProductId { get; set; }
    public string? MesPlanningGroupId { get; set; }
    public DateTime ExecutionDateLocal { get; set; }
    public decimal ActualQty { get; set; }
    public DateTime? RunStartUtc { get; set; }
    public DateTime? RunEndUtc { get; set; }
    public Guid? ScheduleLineId { get; set; }
    public string ExecutionState { get; set; } = "Completed";
    public string? ShortfallReasonCode { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid RecordedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class SupermarketPositionSnapshot
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public Guid? ProductId { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal InTransitQty { get; set; }
    public decimal DemandQty { get; set; }
    public DateTime? StockoutStartUtc { get; set; }
    public DateTime? StockoutEndUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}

public class HeijunkaWorkCenterBreakdownConfig
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public Guid WorkCenterId { get; set; }
    public string GroupingDimensionsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid LastModifiedByUserId { get; set; }
}
