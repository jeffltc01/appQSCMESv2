using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly MesDbContext _db;

    public UsersController(IAuthService authService, MesDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpGet("login-config")]
    public async Task<ActionResult<LoginConfigDto>> GetLoginConfig([FromQuery] string empNo, CancellationToken cancellationToken)
    {
        var (config, inactive) = await _authService.GetLoginConfigAsync(empNo, cancellationToken);
        if (config == null)
            return inactive ? Conflict(new { message = "Employee not active." }) : NotFound();
        return Ok(config);
    }

    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var list = await _db.Users
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
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("roles")]
    public ActionResult<IEnumerable<RoleOptionDto>> GetRoles()
    {
        var roles = new List<RoleOptionDto>
        {
            new() { Tier = 1.0m, Name = "Administrator" },
            new() { Tier = 2.0m, Name = "Quality Director" },
            new() { Tier = 2.0m, Name = "Operations Director" },
            new() { Tier = 3.0m, Name = "Quality Manager" },
            new() { Tier = 3.0m, Name = "Plant Manager" },
            new() { Tier = 4.0m, Name = "Supervisor" },
            new() { Tier = 5.0m, Name = "Quality Tech" },
            new() { Tier = 5.0m, Name = "Team Lead" },
            new() { Tier = 5.5m, Name = "Authorized Inspector" },
            new() { Tier = 6.0m, Name = "Operator" }
        };
        return Ok(roles);
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> CreateUser([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        var normalizedEmpNo = NormalizeEmployeeNumber(dto.EmployeeNumber, dto.UserType);
        var exists = await _db.Users.AnyAsync(
            u => u.EmployeeNumber == normalizedEmpNo, cancellationToken);
        if (exists)
            return Conflict(new { message = "Employee number already exists." });

        if (!string.IsNullOrEmpty(dto.Pin) &&
            (dto.Pin.Length < 4 || dto.Pin.Length > 20 || !dto.Pin.All(char.IsDigit)))
            return BadRequest(new { message = "PIN must be 4-20 digits." });

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
            UserType = (Models.UserType)dto.UserType
        };
        if (!string.IsNullOrEmpty(dto.Pin))
            user.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var site = await _db.Plants.FindAsync(new object[] { dto.DefaultSiteId }, cancellationToken);
        return Ok(new AdminUserDto
        {
            Id = user.Id,
            EmployeeNumber = user.EmployeeNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            RoleTier = user.RoleTier,
            RoleName = user.RoleName,
            DefaultSiteId = user.DefaultSiteId,
            DefaultSiteName = site?.Name ?? "",
            IsCertifiedWelder = user.IsCertifiedWelder,
            RequirePinForLogin = user.RequirePinForLogin,
            HasPin = !string.IsNullOrEmpty(user.PinHash),
            UserType = (int)user.UserType,
            IsActive = user.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user == null) return NotFound();

        if (user.RoleTier <= 2m &&
            Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier) && callerTier > 2m)
            return StatusCode(403, new { message = "You do not have permission to modify this user." });

        var normalizedEmpNo = NormalizeEmployeeNumber(dto.EmployeeNumber, dto.UserType);
        var exists = await _db.Users.AnyAsync(
            u => u.EmployeeNumber == normalizedEmpNo && u.Id != id, cancellationToken);
        if (exists)
            return Conflict(new { message = "Employee number already exists." });

        if (!string.IsNullOrEmpty(dto.Pin) &&
            (dto.Pin.Length < 4 || dto.Pin.Length > 20 || !dto.Pin.All(char.IsDigit)))
            return BadRequest(new { message = "PIN must be 4-20 digits." });

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
        user.UserType = (Models.UserType)dto.UserType;
        user.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        var site = await _db.Plants.FindAsync(new object[] { dto.DefaultSiteId }, cancellationToken);
        return Ok(new AdminUserDto
        {
            Id = user.Id,
            EmployeeNumber = user.EmployeeNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            RoleTier = user.RoleTier,
            RoleName = user.RoleName,
            DefaultSiteId = user.DefaultSiteId,
            DefaultSiteName = site?.Name ?? "",
            IsCertifiedWelder = user.IsCertifiedWelder,
            RequirePinForLogin = user.RequirePinForLogin,
            HasPin = !string.IsNullOrEmpty(user.PinHash),
            UserType = (int)user.UserType,
            IsActive = user.IsActive
        });
    }

    private static string NormalizeEmployeeNumber(string empNo, int userType)
    {
        var raw = empNo.StartsWith("AI", StringComparison.OrdinalIgnoreCase)
            ? empNo[2..] : empNo;
        return userType == (int)Models.UserType.AuthorizedInspector
            ? "AI" + raw : raw;
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(u => u.DefaultSite).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null) return NotFound();

        if (user.RoleTier <= 2m &&
            Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier) && callerTier > 2m)
            return StatusCode(403, new { message = "You do not have permission to deactivate this user." });

        user.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminUserDto
        {
            Id = user.Id,
            EmployeeNumber = user.EmployeeNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            RoleTier = user.RoleTier,
            RoleName = user.RoleName,
            DefaultSiteId = user.DefaultSiteId,
            DefaultSiteName = user.DefaultSite?.Name ?? "",
            IsCertifiedWelder = user.IsCertifiedWelder,
            RequirePinForLogin = user.RequirePinForLogin,
            HasPin = !string.IsNullOrEmpty(user.PinHash),
            UserType = (int)user.UserType,
            IsActive = user.IsActive
        });
    }
}
