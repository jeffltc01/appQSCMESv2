using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ISerialProcessingGateService
{
    Task<SerialProcessingBlockResultDto> EvaluateBySerialIdAsync(Guid serialNumberId, CancellationToken cancellationToken = default);
}
