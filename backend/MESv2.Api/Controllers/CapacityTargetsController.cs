using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/capacity-targets")]
[Authorize]
public class CapacityTargetsController : ControllerBase
{
    private readonly IOeeService _oeeService;

    public CapacityTargetsController(IOeeService oeeService)
    {
        _oeeService = oeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkCenterCapacityTargetDto>>> GetAll(
        [FromQuery] Guid plantId, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.GetCapacityTargetsAsync(plantId, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WorkCenterCapacityTargetDto>> Create(
        [FromBody] CreateWorkCenterCapacityTargetDto dto, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.CreateCapacityTargetAsync(dto, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkCenterCapacityTargetDto>> Update(
        Guid id, [FromBody] UpdateWorkCenterCapacityTargetDto dto, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.UpdateCapacityTargetAsync(id, dto, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        return await _oeeService.DeleteCapacityTargetAsync(id, ct) ? NoContent() : NotFound();
    }

    [HttpPut("bulk")]
    public async Task<ActionResult<IReadOnlyList<WorkCenterCapacityTargetDto>>> BulkUpsert(
        [FromBody] BulkUpsertCapacityTargetsDto dto, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.BulkUpsertCapacityTargetsAsync(dto, ct);
        return Ok(result);
    }

    [HttpGet("tank-sizes")]
    public async Task<ActionResult<IReadOnlyList<int>>> GetTankSizes(
        [FromQuery] Guid plantId, CancellationToken ct)
    {
        if (!IsTeamLeadOrAbove()) return Forbid();
        var result = await _oeeService.GetDistinctTankSizesAsync(plantId, ct);
        return Ok(result);
    }

    private bool IsTeamLeadOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 5.0m;
        return false;
    }
}
