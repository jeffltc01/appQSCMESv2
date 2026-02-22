using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/downtime-reason-categories")]
public class DowntimeReasonCategoriesController : ControllerBase
{
    private readonly IDowntimeService _downtimeService;

    public DowntimeReasonCategoriesController(IDowntimeService downtimeService)
    {
        _downtimeService = downtimeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DowntimeReasonCategoryDto>>> GetAll([FromQuery] Guid plantId, CancellationToken cancellationToken)
    {
        var list = await _downtimeService.GetCategoriesAsync(plantId, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<DowntimeReasonCategoryDto>> Create([FromBody] CreateDowntimeReasonCategoryDto dto, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var result = await _downtimeService.CreateCategoryAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DowntimeReasonCategoryDto>> Update(Guid id, [FromBody] UpdateDowntimeReasonCategoryDto dto, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var result = await _downtimeService.UpdateCategoryAsync(id, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var success = await _downtimeService.DeleteCategoryAsync(id, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    private bool IsQualityManagerOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 3m;
        return false;
    }
}

[ApiController]
[Route("api/downtime-reasons")]
public class DowntimeReasonsController : ControllerBase
{
    private readonly IDowntimeService _downtimeService;

    public DowntimeReasonsController(IDowntimeService downtimeService)
    {
        _downtimeService = downtimeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DowntimeReasonDto>>> GetAll([FromQuery] Guid plantId, CancellationToken cancellationToken)
    {
        var list = await _downtimeService.GetReasonsAsync(plantId, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<DowntimeReasonDto>> Create([FromBody] CreateDowntimeReasonDto dto, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var result = await _downtimeService.CreateReasonAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DowntimeReasonDto>> Update(Guid id, [FromBody] UpdateDowntimeReasonDto dto, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var result = await _downtimeService.UpdateReasonAsync(id, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var success = await _downtimeService.DeleteReasonAsync(id, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    private bool IsQualityManagerOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 3m;
        return false;
    }
}
