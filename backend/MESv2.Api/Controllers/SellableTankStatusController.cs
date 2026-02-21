using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/sellable-tank-status")]
public class SellableTankStatusController : ControllerBase
{
    private readonly ISellableTankStatusService _service;

    public SellableTankStatusController(ISellableTankStatusService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SellableTankStatusDto>>> GetStatus(
        [FromQuery] Guid siteId,
        [FromQuery] string date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use yyyy-MM-dd.");

        var result = await _service.GetStatusAsync(siteId, parsedDate, cancellationToken);
        return Ok(result);
    }
}
