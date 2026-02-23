using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IUserService
{
    Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<AdminUserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<AdminUserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto, decimal? callerRoleTier, CancellationToken ct = default);
    Task<AdminUserDto?> DeleteUserAsync(Guid id, decimal? callerRoleTier, CancellationToken ct = default);
}
