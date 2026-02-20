using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("login-config")]
    public async Task<ActionResult<LoginConfigDto>> GetLoginConfig([FromQuery] string empNo, CancellationToken cancellationToken)
    {
        var config = await _authService.GetLoginConfigAsync(empNo, cancellationToken);
        if (config == null)
            return NotFound();
        return Ok(config);
    }
}
