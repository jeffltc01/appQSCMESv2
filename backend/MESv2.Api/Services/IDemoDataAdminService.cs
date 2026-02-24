using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IDemoDataAdminService
{
    Task<DemoDataResetSeedResultDto> ResetAndSeedAsync(CancellationToken ct = default);
    Task<DemoDataRefreshDatesResultDto> RefreshDatesAsync(CancellationToken ct = default);
}
