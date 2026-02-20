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
    public async Task<ActionResult<KanbanCardLookupDto>> GetCardLookup(string cardId, CancellationToken cancellationToken)
    {
        var result = await _workCenterService.GetCardLookupAsync(cardId, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
