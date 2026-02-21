using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request.EmployeeNumber,
            request.Pin,
            request.SiteId,
            request.IsWelder,
            cancellationToken);
        if (result == null)
            return Unauthorized();
        return Ok(result);
    }

    [Authorize]
    [HttpPut("pin")]
    public async Task<IActionResult> ChangePin([FromBody] ChangePinDto dto, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var success = await _authService.ChangePinAsync(userId, dto.CurrentPin, dto.NewPin, cancellationToken);
        if (!success)
            return BadRequest(new { message = "PIN change failed. Verify your current PIN and ensure the new PIN is 4-20 digits." });

        return Ok(new { message = "PIN changed successfully." });
    }
}
