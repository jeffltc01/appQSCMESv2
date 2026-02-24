using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/demo-shell-flow")]
[Authorize]
public class DemoShellFlowController : ControllerBase
{
    private readonly IDemoShellFlowService _demoShellFlowService;

    public DemoShellFlowController(IDemoShellFlowService demoShellFlowService)
    {
        _demoShellFlowService = demoShellFlowService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<DemoShellCurrentDto>> GetCurrent([FromQuery] Guid workCenterId, CancellationToken ct)
    {
        if (workCenterId == Guid.Empty)
            return BadRequest(new { message = "workCenterId is required." });

        if (!TryGetCallerUserId(out var callerUserId))
            return Unauthorized();

        try
        {
            return Ok(await _demoShellFlowService.GetCurrentAsync(workCenterId, callerUserId, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("advance")]
    public async Task<ActionResult<DemoShellCurrentDto>> Advance([FromBody] DemoShellAdvanceRequestDto request, CancellationToken ct)
    {
        if (request.WorkCenterId == Guid.Empty)
            return BadRequest(new { message = "workCenterId is required." });

        if (!TryGetCallerUserId(out var callerUserId))
            return Unauthorized();

        try
        {
            return Ok(await _demoShellFlowService.AdvanceAsync(request.WorkCenterId, callerUserId, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetCallerUserId(out Guid userId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out userId);
    }
}
