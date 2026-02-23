using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/downtime-events")]
public class DowntimeEventsController : ControllerBase
{
    private readonly IDowntimeService _downtimeService;

    public DowntimeEventsController(IDowntimeService downtimeService)
    {
        _downtimeService = downtimeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DowntimeEventDto>>> GetAll(
        [FromQuery] Guid workCenterId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return StatusCode(403, new { message = "Team Lead or above required." });

        var list = await _downtimeService.GetDowntimeEventsAsync(workCenterId, from, to, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<DowntimeEventDto>> Create([FromBody] CreateDowntimeEventDto dto, CancellationToken cancellationToken)
    {
        var initiatedByUserId = GetUserId();
        if (initiatedByUserId == null)
            return StatusCode(401, new { message = "User not identified." });

        var result = await _downtimeService.CreateDowntimeEventAsync(dto, initiatedByUserId.Value, cancellationToken);
        return StatusCode(201, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DowntimeEventDto>> Update(Guid id, [FromBody] UpdateDowntimeEventDto dto, CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return StatusCode(403, new { message = "Team Lead or above required." });

        var result = await _downtimeService.UpdateDowntimeEventAsync(id, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return StatusCode(403, new { message = "Team Lead or above required." });

        var success = await _downtimeService.DeleteDowntimeEventAsync(id, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }

    private bool IsTeamLeadOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 5m;
        return false;
    }
}
