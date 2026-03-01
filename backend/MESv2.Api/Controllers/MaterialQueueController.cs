using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/material-queue")]
public class MaterialQueueController : ControllerBase
{
    private readonly IWorkCenterService _workCenterService;

    public MaterialQueueController(IWorkCenterService workCenterService)
    {
        _workCenterService = workCenterService;
    }

    [HttpGet("card/{cardId}")]
    public async Task<ActionResult<KanbanCardLookupDto>> GetCardLookup(
        string cardId,
        [FromQuery] Guid workCenterId,
        [FromQuery] Guid productionLineId,
        CancellationToken cancellationToken)
    {
        var result = await _workCenterService.GetCardLookupAsync(workCenterId, productionLineId, cardId, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
