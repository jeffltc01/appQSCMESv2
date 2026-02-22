using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/supervisor-dashboard")]
[Authorize]
public class SupervisorDashboardController : ControllerBase
{
    private readonly ISupervisorDashboardService _service;

    public SupervisorDashboardController(ISupervisorDashboardService service)
    {
        _service = service;
    }

    [HttpGet("{wcId:guid}/metrics")]
    public async Task<ActionResult<SupervisorDashboardMetricsDto>> GetMetrics(
        Guid wcId,
        [FromQuery] Guid plantId,
        [FromQuery] string date,
        [FromQuery] Guid? operatorId,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return Forbid();

        var metrics = await _service.GetMetricsAsync(wcId, plantId, date, operatorId, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("{wcId:guid}/records")]
    public async Task<ActionResult<IReadOnlyList<SupervisorRecordDto>>> GetRecords(
        Guid wcId,
        [FromQuery] Guid plantId,
        [FromQuery] string date,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return Forbid();

        var records = await _service.GetRecordsAsync(wcId, plantId, date, cancellationToken);
        return Ok(records);
    }

    [HttpPost("annotate")]
    public async Task<ActionResult<SupervisorAnnotationResultDto>> SubmitAnnotation(
        [FromBody] CreateSupervisorAnnotationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return Forbid();

        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _service.SubmitAnnotationAsync(userId.Value, request, cancellationToken);
        return Ok(result);
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
