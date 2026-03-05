namespace MESv2.Api.DTOs;

public class ErpDemandRawIngestDto
{
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
}

public class IngestErpDemandRequestDto
{
    public List<ErpDemandRawIngestDto> Rows { get; set; } = new();
}

public class IngestErpDemandResultDto
{
    public int RawRowsInserted { get; set; }
    public int SnapshotsCreated { get; set; }
    public int UnmappedExceptionsCreated { get; set; }
}

public class UpsertErpSkuMappingRequestDto
{
    public string ErpSkuCode { get; set; } = string.Empty;
    public string MesPlanningGroupId { get; set; } = string.Empty;
    public string? SiteCode { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresReview { get; set; }
}

public class ErpSkuMappingDto : UpsertErpSkuMappingRequestDto
{
    public Guid Id { get; set; }
    public Guid MappingOwnerUserId { get; set; }
    public DateTime? LastReviewedAtUtc { get; set; }
}

public class GenerateScheduleDraftRequestDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime WeekStartDateLocal { get; set; }
    public int FreezeHours { get; set; } = 24;
    public string PlanningResourceId { get; set; } = "default";
}

public class UpsertWorkCenterBreakdownConfigRequestDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public Guid WorkCenterId { get; set; }
    public List<string> GroupingDimensions { get; set; } = new();
}

public class WorkCenterBreakdownConfigDto
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public List<string> GroupingDimensions { get; set; } = new();
}

public class WorkCenterScheduleBreakdownRequestDto
{
    public Guid ScheduleId { get; set; }
    public Guid WorkCenterId { get; set; }
}

public class WorkCenterScheduleBreakdownRowDto
{
    public DateTime PlannedDateLocal { get; set; }
    public decimal PlannedQty { get; set; }
    public Dictionary<string, string> DimensionValues { get; set; } = new();
}

public class WorkCenterScheduleBreakdownDto
{
    public Guid ScheduleId { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public DateTime WeekStartDateLocal { get; set; }
    public List<string> GroupingDimensions { get; set; } = new();
    public List<WorkCenterScheduleBreakdownRowDto> Rows { get; set; } = new();
}

public class ScheduleLineDto
{
    public Guid Id { get; set; }
    public DateTime PlannedDateLocal { get; set; }
    public int? SequenceIndex { get; set; }
    public Guid? ProductId { get; set; }
    public string PlanningClass { get; set; } = string.Empty;
    public decimal PlannedQty { get; set; }
    public string? LoadGroupId { get; set; }
    public DateTime? DispatchDateLocal { get; set; }
    public string? MesPlanningGroupId { get; set; }
    public string? PlanningResourceId { get; set; }
    public string? ExecutionResourceId { get; set; }
}

public class ScheduleDto
{
    public Guid Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime WeekStartDateLocal { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishedAtUtc { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public int FreezeHours { get; set; }
    public int RevisionNumber { get; set; }
    public List<ScheduleLineDto> Lines { get; set; } = new();
}

public class FreezeOverrideRequestDto
{
    public Guid ScheduleId { get; set; }
    public Guid ScheduleLineId { get; set; }
    public DateTime NewPlannedDateLocal { get; set; }
    public decimal? NewPlannedQty { get; set; }
    public string ChangeReasonCode { get; set; } = string.Empty;
}

public class ReorderScheduleLineRequestDto
{
    public Guid ScheduleId { get; set; }
    public Guid ScheduleLineId { get; set; }
    public int NewSequenceIndex { get; set; }
    public string ChangeReasonCode { get; set; } = string.Empty;
}

public class MoveScheduleLineRequestDto
{
    public Guid ScheduleId { get; set; }
    public Guid ScheduleLineId { get; set; }
    public DateTime NewPlannedDateLocal { get; set; }
    public int? NewSequenceIndex { get; set; }
    public string ChangeReasonCode { get; set; } = string.Empty;
}

public class ScheduleChangeLogDto
{
    public Guid Id { get; set; }
    public Guid? ScheduleLineId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string ChangeReasonCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? FromValue { get; set; }
    public string? ToValue { get; set; }
}

public class UnmappedDemandExceptionDto
{
    public Guid Id { get; set; }
    public string ErpSkuCode { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string LoadGroupId { get; set; } = string.Empty;
    public DateTime DispatchDateLocal { get; set; }
    public decimal RequiredQty { get; set; }
    public DateTime DetectedAtUtc { get; set; }
    public string ExceptionStatus { get; set; } = string.Empty;
    public string? ResolutionNotes { get; set; }
}

public class ResolveUnmappedDemandExceptionRequestDto
{
    public Guid ExceptionId { get; set; }
    public string Action { get; set; } = "Resolve";
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class DispatchRiskSummaryDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime WeekStartDateLocal { get; set; }
    public int OpenUnmappedExceptions { get; set; }
    public int LoadGroupsDue { get; set; }
    public int LoadGroupsPlanned { get; set; }
    public bool HasDispatchRisk => OpenUnmappedExceptions > 0 || LoadGroupsPlanned < LoadGroupsDue;
}

public class FinalScanExecutionRequestDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public string? ExecutionResourceId { get; set; }
    public Guid? ProductId { get; set; }
    public string? MesPlanningGroupId { get; set; }
    public DateTime ExecutionDateLocal { get; set; }
    public decimal ActualQty { get; set; } = 1m;
    public Guid? ScheduleLineId { get; set; }
    public string ExecutionState { get; set; } = "Completed";
    public string? ShortfallReasonCode { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class ScheduleExecutionEventDto
{
    public Guid Id { get; set; }
    public Guid? ScheduleLineId { get; set; }
    public string ExecutionState { get; set; } = string.Empty;
    public decimal ActualQty { get; set; }
    public DateTime ExecutionDateLocal { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class KpiMetricDto
{
    public decimal? Value { get; set; }
    public string? NullReasonCode { get; set; }
}

public class HeijunkaKpiResponseDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime FromDateLocal { get; set; }
    public DateTime ToDateLocal { get; set; }
    public bool IsEligible { get; set; }
    public string? EligibilityReason { get; set; }
    public KpiMetricDto ScheduleAdherencePercent { get; set; } = new();
    public KpiMetricDto PlanAttainmentPercent { get; set; } = new();
    public KpiMetricDto LoadReadinessPercent { get; set; } = new();
    public KpiMetricDto SupermarketStockoutDurationMinutes { get; set; } = new();
}

public class DispatchWeekOrderCoverageDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime WeekStartDateLocal { get; set; }
    public string LoadGroupId { get; set; } = string.Empty;
    public DateTime DispatchDateLocal { get; set; }
    public string ErpSalesOrderId { get; set; } = string.Empty;
    public string ErpSalesOrderLineId { get; set; } = string.Empty;
    public string ErpSkuCode { get; set; } = string.Empty;
    public string? MesPlanningGroupId { get; set; }
    public decimal RequiredQty { get; set; }
    public decimal LoadGroupRequiredQty { get; set; }
    public decimal LoadGroupPlannedQty { get; set; }
    public bool IsMapped { get; set; }
    public bool LoadGroupCovered => LoadGroupPlannedQty >= LoadGroupRequiredQty;
}

public class SupermarketQuantityStatusDto
{
    public string SiteCode { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public DateTime WeekStartDateLocal { get; set; }
    public Guid? ProductId { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal InTransitQty { get; set; }
    public decimal DemandQty { get; set; }
    public decimal NetAvailableQty { get; set; }
    public decimal StockoutDurationMinutes { get; set; }
    public bool HasOpenStockout { get; set; }
    public DateTime LastCapturedAtUtc { get; set; }
}
