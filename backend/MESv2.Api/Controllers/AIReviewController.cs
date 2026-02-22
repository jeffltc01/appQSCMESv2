using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/ai-review")]
[Authorize]
public class AIReviewController : ControllerBase
{
    private readonly IAIReviewService _service;

    public AIReviewController(IAIReviewService service)
    {
        _service = service;
    }

    [HttpGet("{wcId:guid}/records")]
    public async Task<ActionResult<IReadOnlyList<AIReviewRecordDto>>> GetRecords(
        Guid wcId, [FromQuery] Guid plantId, [FromQuery] string date,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorizedInspectorOrDirectorPlus())
            return Forbid();

        var records = await _service.GetRecordsAsync(wcId, plantId, date, cancellationToken);
        return Ok(records);
    }

    [HttpPost]
    public async Task<ActionResult<AIReviewResultDto>> SubmitReview(
        [FromBody] CreateAIReviewRequest request, CancellationToken cancellationToken)
    {
        if (!IsAuthorizedInspectorOrDirectorPlus())
            return Forbid();

        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _service.SubmitReviewAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    private bool IsAuthorizedInspectorOrDirectorPlus()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 2m || callerTier == 5.5m;
        return false;
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
