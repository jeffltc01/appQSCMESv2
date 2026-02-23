using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/spot-xray")]
public class SpotXrayController : ControllerBase
{
    private readonly ISpotXrayService _spotXrayService;

    public SpotXrayController(ISpotXrayService spotXrayService)
    {
        _spotXrayService = spotXrayService;
    }

    [HttpGet("lanes")]
    public async Task<ActionResult<SpotXrayLaneQueuesDto>> GetLaneQueues(
        [FromQuery] string siteCode, CancellationToken ct)
    {
        try
        {
            var result = await _spotXrayService.GetLaneQueuesAsync(siteCode, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("increments")]
    public async Task<ActionResult<CreateSpotXrayIncrementsResponse>> CreateIncrements(
        [FromBody] CreateSpotXrayIncrementsRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _spotXrayService.CreateIncrementsAsync(request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("increments/{id:guid}")]
    public async Task<ActionResult<SpotXrayIncrementDetailDto>> GetIncrement(Guid id, CancellationToken ct)
    {
        var result = await _spotXrayService.GetIncrementAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("increments/recent")]
    public async Task<ActionResult<List<SpotXrayIncrementSummaryDto>>> GetRecentIncrements(
        [FromQuery] string siteCode, CancellationToken ct)
    {
        var result = await _spotXrayService.GetRecentIncrementsAsync(siteCode, ct);
        return Ok(result);
    }

    [HttpPut("increments/{id:guid}")]
    public async Task<ActionResult<SpotXrayIncrementDetailDto>> SaveResults(
        Guid id, [FromBody] SaveSpotXrayResultsRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _spotXrayService.SaveResultsAsync(id, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("shot-number")]
    public async Task<ActionResult<NextShotNumberResponse>> GetNextShotNumber(
        [FromBody] NextShotNumberRequest request, CancellationToken ct)
    {
        var result = await _spotXrayService.GetNextShotNumberAsync(request.PlantId, ct);
        return Ok(result);
    }
}
