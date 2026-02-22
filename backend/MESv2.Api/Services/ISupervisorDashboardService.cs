using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ISupervisorDashboardService
{
    Task<SupervisorDashboardMetricsDto> GetMetricsAsync(
        Guid wcId, Guid plantId, string date, Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupervisorRecordDto>> GetRecordsAsync(
        Guid wcId, Guid plantId, string date,
        CancellationToken cancellationToken = default);

    Task<SupervisorAnnotationResultDto> SubmitAnnotationAsync(
        Guid userId, CreateSupervisorAnnotationRequest request,
        CancellationToken cancellationToken = default);

    Task<PerformanceTableResponseDto> GetPerformanceTableAsync(
        Guid wcId, Guid plantId, string date, string view,
        Guid? operatorId = null, CancellationToken cancellationToken = default);
}
