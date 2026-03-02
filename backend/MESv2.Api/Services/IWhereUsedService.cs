using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IWhereUsedService
{
    Task<IReadOnlyList<WhereUsedResultDto>> SearchAsync(
        string? heatNumber,
        string? coilNumber,
        string? lotNumber,
        Guid? siteId,
        CancellationToken cancellationToken = default);
}
