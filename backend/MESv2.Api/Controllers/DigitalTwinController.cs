using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/digital-twin")]
[Authorize]
public class DigitalTwinController : ControllerBase
{
    private readonly IDigitalTwinService _service;

    public DigitalTwinController(IDigitalTwinService service)
    {
        _service = service;
    }

    [HttpGet("{productionLineId:guid}/snapshot")]
    public async Task<ActionResult<DigitalTwinSnapshotDto>> GetSnapshot(
        Guid productionLineId,
        [FromQuery] Guid plantId,
        CancellationToken cancellationToken)
    {
        if (!IsTeamLeadOrAbove())
            return Forbid();

        var snapshot = await _service.GetSnapshotAsync(plantId, productionLineId, cancellationToken);
        return Ok(snapshot);
    }

    private bool IsTeamLeadOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 5.0m;
        return false;
    }
}
