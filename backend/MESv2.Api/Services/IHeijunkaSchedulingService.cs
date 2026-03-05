using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IHeijunkaSchedulingService
{
    Task<IngestErpDemandResultDto> IngestErpDemandAsync(IngestErpDemandRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<ErpSkuMappingDto> UpsertSkuMappingAsync(UpsertErpSkuMappingRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ErpSkuMappingDto>> GetSkuMappingsAsync(string? siteCode, CancellationToken ct = default);
    Task<ScheduleDto> GenerateDraftAsync(GenerateScheduleDraftRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<ScheduleDto?> GetScheduleAsync(Guid scheduleId, CancellationToken ct = default);
    Task<ScheduleDto?> PublishScheduleAsync(Guid scheduleId, Guid actorUserId, CancellationToken ct = default);
    Task<ScheduleDto?> CloseScheduleAsync(Guid scheduleId, Guid actorUserId, CancellationToken ct = default);
    Task<ScheduleDto?> ReopenScheduleAsync(Guid scheduleId, Guid actorUserId, CancellationToken ct = default);
    Task<ScheduleDto?> ApplyFreezeOverrideAsync(FreezeOverrideRequestDto request, Guid actorUserId, decimal actorRoleTier, CancellationToken ct = default);
    Task<ScheduleDto?> ReorderScheduleLineAsync(ReorderScheduleLineRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<ScheduleDto?> MoveScheduleLineAsync(MoveScheduleLineRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduleChangeLogDto>> GetChangeHistoryAsync(Guid scheduleId, CancellationToken ct = default);
    Task<IReadOnlyList<UnmappedDemandExceptionDto>> GetUnmappedExceptionsAsync(string siteCode, CancellationToken ct = default);
    Task<UnmappedDemandExceptionDto?> ResolveOrDeferExceptionAsync(ResolveUnmappedDemandExceptionRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<DispatchRiskSummaryDto> GetDispatchRiskSummaryAsync(string siteCode, Guid productionLineId, DateTime weekStartDateLocal, CancellationToken ct = default);
    Task<IReadOnlyList<DispatchWeekOrderCoverageDto>> GetDispatchWeekOrderCoverageAsync(string siteCode, Guid productionLineId, DateTime weekStartDateLocal, Guid scheduleId, CancellationToken ct = default);
    Task<IReadOnlyList<SupermarketQuantityStatusDto>> GetSupermarketQuantityStatusAsync(string siteCode, Guid productionLineId, DateTime weekStartDateLocal, CancellationToken ct = default);
    Task<ScheduleExecutionEventDto> RecordFinalScanExecutionAsync(FinalScanExecutionRequestDto request, Guid actorUserId, CancellationToken ct = default);
    Task<HeijunkaKpiResponseDto> GetPhase1KpisAsync(string siteCode, Guid productionLineId, DateTime fromDateLocal, DateTime toDateLocal, CancellationToken ct = default);
}
