using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/checklists")]
public class ChecklistsController : ControllerBase
{
    private readonly IChecklistService _checklistService;

    public ChecklistsController(IChecklistService checklistService)
    {
        _checklistService = checklistService;
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<ChecklistTemplateDto>>> GetTemplates(
        [FromQuery] Guid? siteId,
        [FromQuery] string? checklistType,
        CancellationToken ct)
    {
        var templates = await _checklistService.GetTemplatesAsync(siteId, checklistType, ct);
        return Ok(templates);
    }

    [HttpGet("templates/{templateId:guid}")]
    public async Task<ActionResult<ChecklistTemplateDto>> GetTemplate(Guid templateId, CancellationToken ct)
    {
        var template = await _checklistService.GetTemplateAsync(templateId, ct);
        if (template == null)
        {
            return NotFound();
        }

        return Ok(template);
    }

    [HttpPost("templates")]
    public async Task<ActionResult<ChecklistTemplateDto>> UpsertTemplate([FromBody] UpsertChecklistTemplateRequestDto request, CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }
        if (!TryGetCallerRoleTier(out var callerRoleTier))
        {
            return BadRequest(new { message = "Missing X-User-Role-Tier header." });
        }
        if (!TryGetCallerUserId(out var userId))
        {
            return BadRequest(new { message = "Missing X-User-Id header." });
        }

        try
        {
            var template = await _checklistService.UpsertTemplateAsync(request, userId, callerRoleTier, callerSiteId, ct);
            return Ok(template);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("templates/resolve")]
    public async Task<ActionResult<ChecklistTemplateDto>> ResolveTemplate([FromBody] ResolveChecklistTemplateRequestDto request, CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }

        try
        {
            var template = await _checklistService.ResolveTemplateAsync(request, callerSiteId, ct);
            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("entries")]
    public async Task<ActionResult<ChecklistEntryDto>> CreateEntry([FromBody] CreateChecklistEntryRequestDto request, CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }
        if (!TryGetCallerRoleTier(out var callerRoleTier))
        {
            return BadRequest(new { message = "Missing X-User-Role-Tier header." });
        }
        if (!TryGetCallerUserId(out var callerUserId))
        {
            return BadRequest(new { message = "Missing X-User-Id header." });
        }

        try
        {
            // Always trust the authenticated caller identity for operator ownership.
            request.OperatorUserId = callerUserId;
            var entry = await _checklistService.CreateEntryAsync(request, callerSiteId, callerRoleTier, ct);
            return Ok(entry);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("entries/{entryId:guid}/responses")]
    public async Task<ActionResult<ChecklistEntryDto>> SubmitResponses(Guid entryId, [FromBody] SubmitChecklistResponsesRequestDto request, CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }
        if (!TryGetCallerRoleTier(out var callerRoleTier))
        {
            return BadRequest(new { message = "Missing X-User-Role-Tier header." });
        }

        try
        {
            var entry = await _checklistService.SubmitResponsesAsync(entryId, request, callerSiteId, callerRoleTier, ct);
            if (entry == null)
            {
                return NotFound();
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("entries/{entryId:guid}/complete")]
    public async Task<ActionResult<ChecklistEntryDto>> CompleteEntry(Guid entryId, CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }
        if (!TryGetCallerRoleTier(out var callerRoleTier))
        {
            return BadRequest(new { message = "Missing X-User-Role-Tier header." });
        }

        try
        {
            var entry = await _checklistService.CompleteEntryAsync(entryId, callerSiteId, callerRoleTier, ct);
            if (entry == null)
            {
                return NotFound();
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<ChecklistEntryDto>>> GetHistory(
        [FromQuery] Guid siteId,
        [FromQuery] Guid? workCenterId,
        [FromQuery] string? checklistType,
        CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }
        if (siteId != callerSiteId)
        {
            return Forbid();
        }

        var entries = await _checklistService.GetEntryHistoryAsync(siteId, workCenterId, checklistType, ct);
        return Ok(entries);
    }

    [HttpGet("entries/{entryId:guid}")]
    public async Task<ActionResult<ChecklistEntryDto>> GetEntry(Guid entryId, CancellationToken ct)
    {
        if (!TryGetCallerSiteId(out var callerSiteId))
        {
            return BadRequest(new { message = "Missing X-User-Site-Id header." });
        }

        var entry = await _checklistService.GetEntryDetailAsync(entryId, callerSiteId, ct);
        if (entry == null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    private bool TryGetCallerRoleTier(out decimal roleTier)
    {
        roleTier = 99m;
        return Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader)
               && decimal.TryParse(tierHeader, out roleTier);
    }

    private bool TryGetCallerSiteId(out Guid siteId)
    {
        siteId = Guid.Empty;
        return Request.Headers.TryGetValue("X-User-Site-Id", out var siteHeader)
               && Guid.TryParse(siteHeader, out siteId);
    }

    private bool TryGetCallerUserId(out Guid userId)
    {
        userId = Guid.Empty;
        return Request.Headers.TryGetValue("X-User-Id", out var userHeader)
               && Guid.TryParse(userHeader, out userId);
    }
}
