using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/shift-schedules")]
[Authorize]
public class ShiftSchedulesController : ControllerBase
{
    private readonly IOeeService _oeeService;

    public ShiftSchedulesController(IOeeService oeeService)
    {
        _oeeService = oeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShiftScheduleDto>>> GetAll(
        [FromQuery] Guid plantId, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.GetShiftSchedulesAsync(plantId, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftScheduleDto>> Create(
        [FromBody] CreateShiftScheduleDto dto, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var userId = GetUserId();
        var result = await _oeeService.CreateShiftScheduleAsync(dto, userId, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShiftScheduleDto>> Update(
        Guid id, [FromBody] UpdateShiftScheduleDto dto, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.UpdateShiftScheduleAsync(id, dto, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        return await _oeeService.DeleteShiftScheduleAsync(id, ct) ? NoContent() : NotFound();
    }

    private bool IsTeamLeadOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 5.0m;
        return false;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
