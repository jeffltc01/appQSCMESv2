using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class AuthService : IAuthService
{
    private readonly MesDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public AuthService(MesDbContext db, IConfiguration config, IHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public async Task<LoginConfigDto?> GetLoginConfigAsync(string empNo, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.DefaultSite)
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive, cancellationToken);
        if (user == null)
            return null;

        return new LoginConfigDto
        {
            RequiresPin = user.RequirePinForLogin,
            DefaultSiteId = user.DefaultSiteId,
            AllowSiteSelection = true,
            IsWelder = user.IsCertifiedWelder,
            UserName = user.DisplayName
        };
    }

    public async Task<LoginResultDto?> LoginAsync(string empNo, string? pin, Guid siteId, bool isWelder, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.DefaultSite)
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive, cancellationToken);
        if (user == null)
            return null;

        if (user.RequirePinForLogin)
        {
            if (string.IsNullOrEmpty(pin))
                return null;
            if (string.IsNullOrEmpty(user.PinHash) || !BCrypt.Net.BCrypt.Verify(pin, user.PinHash))
                return null;
        }

        var token = GenerateJwt(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            EmployeeNumber = user.EmployeeNumber,
            DisplayName = user.DisplayName,
            RoleTier = user.RoleTier,
            RoleName = user.RoleName,
            DefaultSiteId = user.DefaultSiteId,
            IsCertifiedWelder = user.IsCertifiedWelder,
            UserType = (int)user.UserType,
            PlantCode = user.DefaultSite?.Code ?? string.Empty,
            PlantName = user.DefaultSite?.Name ?? string.Empty,
            PlantTimeZoneId = user.DefaultSite?.TimeZoneId ?? "America/Chicago"
        };

        return new LoginResultDto { Token = token, User = userDto };
    }

    public async Task<bool> ChangePinAsync(Guid userId, string? currentPin, string newPin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(newPin) || newPin.Length < 4 || newPin.Length > 6 || !newPin.All(char.IsDigit))
            return false;

        var user = await _db.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return false;

        if (!string.IsNullOrEmpty(user.PinHash))
        {
            if (string.IsNullOrEmpty(currentPin) || !BCrypt.Net.BCrypt.Verify(currentPin, user.PinHash))
                return false;
        }

        user.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private string GenerateJwt(User user)
    {
        string key;
        if (_env.IsDevelopment())
            key = _config["Jwt:Key"] ?? "dev-secret-key-min-32-chars-long-for-hs256";
        else
            key = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key must be configured in non-Development environments.");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new("employeeNumber", user.EmployeeNumber),
            new("defaultSiteId", user.DefaultSiteId.ToString())
        };

        var issuer = _config["Jwt:Issuer"] ?? "MESv2";
        var audience = _config["Jwt:Audience"] ?? "MESv2";
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
