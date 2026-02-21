using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IAuthService
{
    Task<LoginConfigDto?> GetLoginConfigAsync(string empNo, CancellationToken cancellationToken = default);
    Task<LoginResultDto?> LoginAsync(string empNo, string? pin, Guid siteId, bool isWelder, CancellationToken cancellationToken = default);
    Task<bool> ChangePinAsync(Guid userId, string? currentPin, string newPin, CancellationToken cancellationToken = default);
}
