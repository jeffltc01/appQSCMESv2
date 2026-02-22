using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IOeeService
{
    // Shift Schedules
    Task<IReadOnlyList<ShiftScheduleDto>> GetShiftSchedulesAsync(Guid plantId, CancellationToken ct = default);
    Task<ShiftScheduleDto> CreateShiftScheduleAsync(CreateShiftScheduleDto dto, Guid? userId, CancellationToken ct = default);
    Task<ShiftScheduleDto?> UpdateShiftScheduleAsync(Guid id, UpdateShiftScheduleDto dto, CancellationToken ct = default);
    Task<bool> DeleteShiftScheduleAsync(Guid id, CancellationToken ct = default);

    // Capacity Targets
    Task<IReadOnlyList<WorkCenterCapacityTargetDto>> GetCapacityTargetsAsync(Guid plantId, CancellationToken ct = default);
    Task<WorkCenterCapacityTargetDto> CreateCapacityTargetAsync(CreateWorkCenterCapacityTargetDto dto, CancellationToken ct = default);
    Task<WorkCenterCapacityTargetDto?> UpdateCapacityTargetAsync(Guid id, UpdateWorkCenterCapacityTargetDto dto, CancellationToken ct = default);
    Task<bool> DeleteCapacityTargetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkCenterCapacityTargetDto>> BulkUpsertCapacityTargetsAsync(BulkUpsertCapacityTargetsDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetDistinctTankSizesAsync(Guid plantId, CancellationToken ct = default);

    // OEE Calculation
    Task<OeeMetricsDto> CalculateOeeAsync(Guid wcId, Guid plantId, string date, CancellationToken ct = default);
}
