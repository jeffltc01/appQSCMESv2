using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ISellableTankStatusService
{
    Task<IReadOnlyList<SellableTankStatusDto>> GetStatusAsync(Guid siteId, DateOnly date, CancellationToken cancellationToken = default);
}
