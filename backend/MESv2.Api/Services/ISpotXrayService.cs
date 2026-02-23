using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ISpotXrayService
{
    Task<SpotXrayLaneQueuesDto> GetLaneQueuesAsync(string siteCode, CancellationToken ct = default);
    Task<CreateSpotXrayIncrementsResponse> CreateIncrementsAsync(CreateSpotXrayIncrementsRequest request, CancellationToken ct = default);
    Task<SpotXrayIncrementDetailDto?> GetIncrementAsync(Guid incrementId, CancellationToken ct = default);
    Task<List<SpotXrayIncrementSummaryDto>> GetRecentIncrementsAsync(string siteCode, CancellationToken ct = default);
    Task<SpotXrayIncrementDetailDto> SaveResultsAsync(Guid incrementId, SaveSpotXrayResultsRequest request, CancellationToken ct = default);
    Task<NextShotNumberResponse> GetNextShotNumberAsync(Guid plantId, CancellationToken ct = default);
}
