using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/admin/demo-data")]
[Authorize]
public class DemoDataAdminController : ControllerBase
{
    private readonly IDemoDataAdminService _service;

    public DemoDataAdminController(IDemoDataAdminService service)
    {
        _service = service;
    }

    [HttpPost("reset-seed")]
    public async Task<ActionResult<DemoDataResetSeedResultDto>> ResetSeed(CancellationToken ct)
    {
        if (!IsAdminOnly())
            return Forbid();

        try
        {
            var result = await _service.ResetAndSeedAsync(ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpPost("refresh-dates")]
    public async Task<ActionResult<DemoDataRefreshDatesResultDto>> RefreshDates(CancellationToken ct)
    {
        if (!IsAdminOnly())
            return Forbid();

        try
        {
            var result = await _service.RefreshDatesAsync(ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    private bool IsAdminOnly()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 1.0m;
        return false;
    }
}
