using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/defect-analytics")]
[Authorize]
public class DefectAnalyticsController : ControllerBase
{
    private readonly IDefectAnalyticsService _service;

    public DefectAnalyticsController(IDefectAnalyticsService service)
    {
        _service = service;
    }

    [HttpGet("{wcId:guid}/pareto")]
    public async Task<ActionResult<DefectParetoResponseDto>> GetPareto(
        Guid wcId,
        [FromQuery] Guid plantId,
        [FromQuery] string date,
        [FromQuery] string view,
        [FromQuery] Guid? operatorId,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return Forbid();

        var result = await _service.GetDefectParetoAsync(
            wcId, plantId, date, view, operatorId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{wcId:guid}/downtime-pareto")]
    public async Task<ActionResult<DowntimeParetoResponseDto>> GetDowntimePareto(
        Guid wcId,
        [FromQuery] Guid plantId,
        [FromQuery] string date,
        [FromQuery] string view,
        [FromQuery] Guid? operatorId,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return Forbid();

        var result = await _service.GetDowntimeParetoAsync(
            wcId, plantId, date, view, operatorId, cancellationToken);
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
