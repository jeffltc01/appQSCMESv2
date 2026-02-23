using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/nlq")]
[Authorize]
public class NlqController : ControllerBase
{
    private readonly INaturalLanguageQueryService _service;

    public NlqController(INaturalLanguageQueryService service)
    {
        _service = service;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<NaturalLanguageQueryResponseDto>> Ask(
        [FromBody] NaturalLanguageQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (!TryGetRoleTier(out var roleTier))
            return Forbid();

        if (!TryGetSiteId(out var siteId))
            return Forbid();

        try
        {
            var response = await _service.AskAsync(
                userId.Value,
                roleTier,
                siteId,
                request,
                cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _ = ex;
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private bool TryGetRoleTier(out decimal roleTier)
    {
        roleTier = 99m;
        if (!Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader))
            return false;
        return decimal.TryParse(tierHeader, out roleTier);
    }

    private bool TryGetSiteId(out Guid siteId)
    {
        siteId = Guid.Empty;
        if (!Request.Headers.TryGetValue("X-User-Site-Id", out var siteHeader))
            return false;
        return Guid.TryParse(siteHeader, out siteId);
    }
}
