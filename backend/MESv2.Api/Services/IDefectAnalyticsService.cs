using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IDefectAnalyticsService
{
    Task<DefectParetoResponseDto> GetDefectParetoAsync(
        Guid wcId,
        Guid plantId,
        string date,
        string view,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    Task<DowntimeParetoResponseDto> GetDowntimeParetoAsync(
        Guid wcId,
        Guid plantId,
        string date,
        string view,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);
}
