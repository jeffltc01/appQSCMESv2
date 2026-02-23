using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class UserService : IUserService
{
    private readonly MesDbContext _db;

    public UserService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<List<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .Include(u => u.DefaultSite)
            .OrderBy(u => (double)u.RoleTier).ThenBy(u => u.DisplayName)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                EmployeeNumber = u.EmployeeNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DisplayName = u.DisplayName,
                RoleTier = u.RoleTier,
                RoleName = u.RoleName,
                DefaultSiteId = u.DefaultSiteId,
                DefaultSiteName = u.DefaultSite.Name,
                IsCertifiedWelder = u.IsCertifiedWelder,
                RequirePinForLogin = u.RequirePinForLogin,
                HasPin = u.PinHash != null,
                UserType = (int)u.UserType,
                IsActive = u.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var normalizedEmpNo = NormalizeEmployeeNumber(dto.EmployeeNumber, dto.UserType);

        var exists = await _db.Users.AnyAsync(u => u.EmployeeNumber == normalizedEmpNo, ct);
        if (exists)
            throw new InvalidOperationException("Employee number already exists.");

        if (!string.IsNullOrEmpty(dto.Pin) &&
            (dto.Pin.Length < 4 || dto.Pin.Length > 20 || !dto.Pin.All(char.IsDigit)))
            throw new ArgumentException("PIN must be 4-20 digits.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = normalizedEmpNo,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName,
            RoleTier = dto.RoleTier,
            RoleName = dto.RoleName,
            DefaultSiteId = dto.DefaultSiteId,
            IsCertifiedWelder = dto.IsCertifiedWelder,
            RequirePinForLogin = dto.RequirePinForLogin,
            UserType = (UserType)dto.UserType
        };

        if (!string.IsNullOrEmpty(dto.Pin))
            user.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var site = await _db.Plants.FindAsync(new object[] { dto.DefaultSiteId }, ct);
        return MapToDto(user, site?.Name ?? "");
    }

    public async Task<AdminUserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto, decimal? callerRoleTier, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, ct);
        if (user == null) return null;

        if (user.RoleTier <= 2m && callerRoleTier.HasValue && callerRoleTier.Value > 2m)
            throw new UnauthorizedAccessException("You do not have permission to modify this user.");

        var normalizedEmpNo = NormalizeEmployeeNumber(dto.EmployeeNumber, dto.UserType);
        var exists = await _db.Users.AnyAsync(u => u.EmployeeNumber == normalizedEmpNo && u.Id != id, ct);
        if (exists)
            throw new InvalidOperationException("Employee number already exists.");

        if (!string.IsNullOrEmpty(dto.Pin) &&
            (dto.Pin.Length < 4 || dto.Pin.Length > 20 || !dto.Pin.All(char.IsDigit)))
            throw new ArgumentException("PIN must be 4-20 digits.");

        user.EmployeeNumber = normalizedEmpNo;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.DisplayName = dto.DisplayName;
        user.RoleTier = dto.RoleTier;
        user.RoleName = dto.RoleName;
        user.DefaultSiteId = dto.DefaultSiteId;
        user.IsCertifiedWelder = dto.IsCertifiedWelder;
        user.RequirePinForLogin = dto.RequirePinForLogin;
        if (!dto.RequirePinForLogin)
            user.PinHash = null;
        else if (!string.IsNullOrEmpty(dto.Pin))
            user.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
        user.UserType = (UserType)dto.UserType;
        user.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);

        var site = await _db.Plants.FindAsync(new object[] { dto.DefaultSiteId }, ct);
        return MapToDto(user, site?.Name ?? "");
    }

    public async Task<AdminUserDto?> DeleteUserAsync(Guid id, decimal? callerRoleTier, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.DefaultSite).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return null;

        if (user.RoleTier <= 2m && callerRoleTier.HasValue && callerRoleTier.Value > 2m)
            throw new UnauthorizedAccessException("You do not have permission to deactivate this user.");

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        return MapToDto(user, user.DefaultSite?.Name ?? "");
    }

    private static string NormalizeEmployeeNumber(string empNo, int userType)
    {
        var raw = empNo.StartsWith("AI", StringComparison.OrdinalIgnoreCase) ? empNo[2..] : empNo;
        return userType == (int)UserType.AuthorizedInspector ? "AI" + raw : raw;
    }

    private static AdminUserDto MapToDto(User user, string siteName) => new()
    {
        Id = user.Id,
        EmployeeNumber = user.EmployeeNumber,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DisplayName = user.DisplayName,
        RoleTier = user.RoleTier,
        RoleName = user.RoleName,
        DefaultSiteId = user.DefaultSiteId,
        DefaultSiteName = siteName,
        IsCertifiedWelder = user.IsCertifiedWelder,
        RequirePinForLogin = user.RequirePinForLogin,
        HasPin = !string.IsNullOrEmpty(user.PinHash),
        UserType = (int)user.UserType,
        IsActive = user.IsActive
    };
}
