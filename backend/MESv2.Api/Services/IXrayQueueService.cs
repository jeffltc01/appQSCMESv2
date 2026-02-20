using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IXrayQueueService
{
    Task<IReadOnlyList<XrayQueueItemDto>> GetQueueAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<XrayQueueItemDto> AddAsync(Guid wcId, AddXrayQueueItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default);
}
