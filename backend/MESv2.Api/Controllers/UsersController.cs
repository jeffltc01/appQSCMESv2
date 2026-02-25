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
    private readonly IUserService _userService;

    public UsersController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
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
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAllUsers(CancellationToken cancellationToken, [FromQuery] Guid? siteId = null, [FromQuery] decimal[]? roleTiers = null)
    {
        return Ok(await _userService.GetAllUsersAsync(siteId, roleTiers, cancellationToken));
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
        try
        {
            return Ok(await _userService.CreateUserAsync(dto, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private decimal? GetCallerRoleTier()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var tier))
            return tier;
        return null;
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.UpdateUserAsync(id, dto, GetCallerRoleTier(), cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id, GetCallerRoleTier(), cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}
