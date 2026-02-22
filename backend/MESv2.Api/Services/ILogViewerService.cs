using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ILogViewerService
{
    Task<List<RollsLogEntryDto>> GetRollsLogAsync(Guid siteId, string startDate, string endDate, CancellationToken ct = default);
    Task<List<FitupLogEntryDto>> GetFitupLogAsync(Guid siteId, string startDate, string endDate, CancellationToken ct = default);
    Task<List<HydroLogEntryDto>> GetHydroLogAsync(Guid siteId, string startDate, string endDate, CancellationToken ct = default);
    Task<List<RtXrayLogEntryDto>> GetRtXrayLogAsync(Guid siteId, string startDate, string endDate, CancellationToken ct = default);
    Task<SpotXrayLogResponseDto> GetSpotXrayLogAsync(Guid siteId, string startDate, string endDate, CancellationToken ct = default);
}
