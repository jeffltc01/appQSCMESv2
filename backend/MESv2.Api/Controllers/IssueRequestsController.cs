using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/issue-requests")]
public class IssueRequestsController : ControllerBase
{
    private readonly IIssueRequestService _service;
    private readonly ILogger<IssueRequestsController> _logger;

    public IssueRequestsController(IIssueRequestService service, ILogger<IssueRequestsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<IssueRequestDto>> Submit(
        [FromBody] CreateIssueRequestDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.SubmitAsync(dto, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit issue request");
            return StatusCode(500, new { message = "Failed to submit issue request." });
        }
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<IssueRequestDto>>> GetMine(
        [FromQuery] Guid userId, CancellationToken ct)
    {
        var result = await _service.GetMyRequestsAsync(userId, ct);
        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<IssueRequestDto>>> GetPending(CancellationToken ct)
    {
        if (!IsQualityManagerOrAbove())
            return StatusCode(403, new { message = "Quality Manager or above required." });

        var result = await _service.GetPendingAsync(ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/approve")]
    public async Task<ActionResult<IssueRequestDto>> Approve(
        Guid id, [FromBody] ApproveIssueRequestDto dto, CancellationToken ct)
    {
        if (!IsQualityManagerOrAbove())
            return StatusCode(403, new { message = "Quality Manager or above required." });

        try
        {
            var result = await _service.ApproveAsync(id, dto, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve issue request {Id}", id);
            return StatusCode(500, new { message = "Failed to approve issue request." });
        }
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<ActionResult<IssueRequestDto>> Reject(
        Guid id, [FromBody] RejectIssueRequestDto dto, CancellationToken ct)
    {
        if (!IsQualityManagerOrAbove())
            return StatusCode(403, new { message = "Quality Manager or above required." });

        try
        {
            var result = await _service.RejectAsync(id, dto, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsQualityManagerOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 3m;
        return false;
    }
}
