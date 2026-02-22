using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IDigitalTwinService
{
    Task<DigitalTwinSnapshotDto> GetSnapshotAsync(
        Guid plantId, Guid productionLineId, CancellationToken cancellationToken = default);
}
