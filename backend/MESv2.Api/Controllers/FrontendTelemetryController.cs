using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/frontend-telemetry")]
[Authorize]
public class FrontendTelemetryController : ControllerBase
{
    private const int DefaultWarningThreshold = 250_000;
    private readonly IFrontendTelemetryService _telemetryService;
    private readonly MesDbContext _db;
    private readonly ILogger<FrontendTelemetryController> _logger;

    public FrontendTelemetryController(
        IFrontendTelemetryService telemetryService,
        MesDbContext db,
        ILogger<FrontendTelemetryController> logger)
    {
        _telemetryService = telemetryService;
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] FrontendTelemetryIngestDto dto, CancellationToken ct)
    {
        if (dto is null)
            return BadRequest("Payload is required.");

        if (dto.UserId is null)
        {
            var userIdFromClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdFromClaim, out var userId))
                dto.UserId = userId;
        }

        try
        {
            await _telemetryService.IngestAsync(dto, ct);
        }
        catch (Exception ex)
        {
            // Telemetry is best-effort: never fail operator workflows on telemetry persistence errors.
            _logger.LogWarning(ex, "Failed to persist frontend telemetry event.");
        }

        return Accepted();
    }

    [HttpGet]
    public async Task<ActionResult<FrontendTelemetryPageDto>> GetEvents(
        [FromQuery] string? category,
        [FromQuery] string? source,
        [FromQuery] string? severity,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? workCenterId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool reactRuntimeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 3.0m)
            return Forbid();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var result = await _telemetryService.GetEventsAsync(
            category, source, severity, userId, workCenterId, from, to, reactRuntimeOnly, page, pageSize, ct);

        return Ok(result);
    }

    [HttpGet("filters")]
    public async Task<ActionResult<FrontendTelemetryFilterOptionsDto>> GetFilters(CancellationToken ct)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 3.0m)
            return Forbid();

        return Ok(await _telemetryService.GetFilterOptionsAsync(ct));
    }

    [HttpGet("count")]
    public async Task<ActionResult<FrontendTelemetryCountDto>> GetCount(
        [FromQuery] long? warningThreshold,
        CancellationToken ct)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 3.0m)
            return Forbid();

        var threshold = warningThreshold.GetValueOrDefault(DefaultWarningThreshold);
        threshold = Math.Max(1, threshold);
        return Ok(await _telemetryService.GetCountAsync(threshold, ct));
    }

    [HttpPost("archive")]
    public async Task<ActionResult<FrontendTelemetryArchiveResultDto>> ArchiveOldest(
        [FromBody] FrontendTelemetryArchiveRequestDto? request,
        CancellationToken ct)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 3.0m)
            return Forbid();

        var keepRows = request?.KeepRows ?? DefaultWarningThreshold;
        keepRows = Math.Max(1, keepRows);
        return Ok(await _telemetryService.ArchiveOldestAsync(keepRows, ct));
    }

    private async Task<decimal> GetCallerRoleTier(CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return 99m;

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.RoleTier })
            .FirstOrDefaultAsync(ct);

        return user?.RoleTier ?? 99m;
    }
}
