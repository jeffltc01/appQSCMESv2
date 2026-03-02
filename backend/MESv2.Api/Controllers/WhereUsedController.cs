using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/where-used")]
public class WhereUsedController : ControllerBase
{
    private readonly IWhereUsedService _service;

    public WhereUsedController(IWhereUsedService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WhereUsedResultDto>>> Search(
        [FromQuery] string? heatNumber,
        [FromQuery] string? coilNumber,
        [FromQuery] string? lotNumber,
        [FromQuery] Guid? siteId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(heatNumber)
            && string.IsNullOrWhiteSpace(coilNumber)
            && string.IsNullOrWhiteSpace(lotNumber))
        {
            return BadRequest("At least one search field is required.");
        }

        var result = await _service.SearchAsync(heatNumber, coilNumber, lotNumber, siteId, cancellationToken);
        return Ok(result);
    }
}
