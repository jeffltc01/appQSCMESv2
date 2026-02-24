using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IDemoShellFlowService
{
    Task<DemoShellCurrentDto> GetCurrentAsync(Guid workCenterId, Guid callerUserId, CancellationToken ct = default);
    Task<DemoShellCurrentDto> AdvanceAsync(Guid workCenterId, Guid callerUserId, CancellationToken ct = default);
}
