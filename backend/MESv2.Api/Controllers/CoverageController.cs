using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/coverage")]
[Authorize]
public class CoverageController : ControllerBase
{
    private readonly ICoverageReportService _coverageService;
    private readonly MesDbContext _db;

    public CoverageController(ICoverageReportService coverageService, MesDbContext db)
    {
        _coverageService = coverageService;
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 1.0m)
            return Forbid();

        if (!_coverageService.IsConfigured)
            return StatusCode(503, new { message = "Coverage reports are not configured." });

        var json = await _coverageService.GetSummaryJsonAsync(ct);
        if (json is null)
            return NotFound(new { message = "No coverage reports available yet." });

        return Content(json, "application/json");
    }

    [HttpGet("{layer}/{**path}")]
    public async Task<IActionResult> GetReportFile(string layer, string? path, CancellationToken ct)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 1.0m)
            return Forbid();

        if (!_coverageService.IsConfigured)
            return StatusCode(503, new { message = "Coverage reports are not configured." });

        var result = await _coverageService.GetReportFileAsync(layer, path ?? "index.html", ct);
        if (result is null)
            return NotFound();

        return File(result.Value.Content, result.Value.ContentType);
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
