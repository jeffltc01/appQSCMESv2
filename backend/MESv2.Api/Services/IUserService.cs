using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IUserService
{
    Task<List<AdminUserDto>> GetAllUsersAsync(Guid? siteId = null, IReadOnlyCollection<decimal>? roleTiers = null, CancellationToken ct = default);
    Task<AdminUserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<AdminUserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto, decimal? callerRoleTier, CancellationToken ct = default);
    Task<AdminUserDto?> DeleteUserAsync(Guid id, decimal? callerRoleTier, CancellationToken ct = default);
}
