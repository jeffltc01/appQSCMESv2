using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/heijunka-scheduling")]
[Authorize]
public class HeijunkaSchedulingController : ControllerBase
{
    private readonly IHeijunkaSchedulingService _service;

    public HeijunkaSchedulingController(IHeijunkaSchedulingService service)
    {
        _service = service;
    }

    [HttpPost("erp/ingest")]
    public async Task<ActionResult<IngestErpDemandResultDto>> IngestErpDemand([FromBody] IngestErpDemandRequestDto request, CancellationToken ct)
        => Ok(await _service.IngestErpDemandAsync(request, GetUserId(), ct));

    [HttpGet("mappings")]
    public async Task<ActionResult<IReadOnlyList<ErpSkuMappingDto>>> GetMappings([FromQuery] string? siteCode, CancellationToken ct)
        => Ok(await _service.GetSkuMappingsAsync(siteCode, ct));

    [HttpPost("mappings")]
    public async Task<ActionResult<ErpSkuMappingDto>> UpsertMapping([FromBody] UpsertErpSkuMappingRequestDto request, CancellationToken ct)
    {
        if (!CanManageMappings()) return Forbid();
        return Ok(await _service.UpsertSkuMappingAsync(request, GetUserId(), ct));
    }

    [HttpPost("drafts/generate")]
    public async Task<ActionResult<ScheduleDto>> GenerateDraft([FromBody] GenerateScheduleDraftRequestDto request, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        if (!CanAccessSite(request.SiteCode)) return Forbid();
        return Ok(await _service.GenerateDraftAsync(request, GetUserId(), ct));
    }

    [HttpGet("{scheduleId:guid}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(Guid scheduleId, CancellationToken ct)
    {
        var schedule = await _service.GetScheduleAsync(scheduleId, ct);
        if (schedule == null) return NotFound();
        if (!CanAccessSite(schedule.SiteCode)) return Forbid();
        return Ok(schedule);
    }

    [HttpPost("{scheduleId:guid}/publish")]
    public async Task<ActionResult<ScheduleDto>> Publish(Guid scheduleId, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        var result = await _service.PublishScheduleAsync(scheduleId, GetUserId(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("{scheduleId:guid}/close")]
    public async Task<ActionResult<ScheduleDto>> Close(Guid scheduleId, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        var result = await _service.CloseScheduleAsync(scheduleId, GetUserId(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("{scheduleId:guid}/reopen")]
    public async Task<ActionResult<ScheduleDto>> Reopen(Guid scheduleId, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        var result = await _service.ReopenScheduleAsync(scheduleId, GetUserId(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("freeze-override")]
    public async Task<ActionResult<ScheduleDto>> FreezeOverride([FromBody] FreezeOverrideRequestDto request, CancellationToken ct)
    {
        var result = await _service.ApplyFreezeOverrideAsync(request, GetUserId(), GetRoleTier(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("resequence")]
    public async Task<ActionResult<ScheduleDto>> Resequence([FromBody] ReorderScheduleLineRequestDto request, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        var result = await _service.ReorderScheduleLineAsync(request, GetUserId(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("calendar-move")]
    public async Task<ActionResult<ScheduleDto>> CalendarMove([FromBody] MoveScheduleLineRequestDto request, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        var result = await _service.MoveScheduleLineAsync(request, GetUserId(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{scheduleId:guid}/change-history")]
    public async Task<ActionResult<IReadOnlyList<ScheduleChangeLogDto>>> GetChangeHistory(Guid scheduleId, CancellationToken ct)
        => Ok(await _service.GetChangeHistoryAsync(scheduleId, ct));

    [HttpGet("exceptions")]
    public async Task<ActionResult<IReadOnlyList<UnmappedDemandExceptionDto>>> GetExceptions([FromQuery] string siteCode, CancellationToken ct)
    {
        if (!CanAccessSite(siteCode)) return Forbid();
        return Ok(await _service.GetUnmappedExceptionsAsync(siteCode, ct));
    }

    [HttpPost("exceptions/resolve")]
    public async Task<ActionResult<UnmappedDemandExceptionDto>> ResolveException([FromBody] ResolveUnmappedDemandExceptionRequestDto request, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        var result = await _service.ResolveOrDeferExceptionAsync(request, GetUserId(), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("risk-summary")]
    public async Task<ActionResult<DispatchRiskSummaryDto>> GetRiskSummary([FromQuery] string siteCode, [FromQuery] Guid productionLineId, [FromQuery] DateTime weekStartDateLocal, CancellationToken ct)
    {
        if (!CanAccessSite(siteCode)) return Forbid();
        return Ok(await _service.GetDispatchRiskSummaryAsync(siteCode, productionLineId, weekStartDateLocal, ct));
    }

    [HttpGet("dispatch-week-orders")]
    public async Task<ActionResult<IReadOnlyList<DispatchWeekOrderCoverageDto>>> GetDispatchWeekOrders([FromQuery] string siteCode, [FromQuery] Guid productionLineId, [FromQuery] DateTime weekStartDateLocal, [FromQuery] Guid scheduleId, CancellationToken ct)
    {
        if (!CanAccessSite(siteCode)) return Forbid();
        return Ok(await _service.GetDispatchWeekOrderCoverageAsync(siteCode, productionLineId, weekStartDateLocal, scheduleId, ct));
    }

    [HttpGet("supermarket-quantities")]
    public async Task<ActionResult<IReadOnlyList<SupermarketQuantityStatusDto>>> GetSupermarketQuantities([FromQuery] string siteCode, [FromQuery] Guid productionLineId, [FromQuery] DateTime weekStartDateLocal, CancellationToken ct)
    {
        if (!CanAccessSite(siteCode)) return Forbid();
        return Ok(await _service.GetSupermarketQuantityStatusAsync(siteCode, productionLineId, weekStartDateLocal, ct));
    }

    [HttpPost("execution/final-scan")]
    public async Task<ActionResult<ScheduleExecutionEventDto>> RecordFinalScan([FromBody] FinalScanExecutionRequestDto request, CancellationToken ct)
    {
        if (!CanPlan()) return Forbid();
        if (!CanAccessSite(request.SiteCode)) return Forbid();
        return Ok(await _service.RecordFinalScanExecutionAsync(request, GetUserId(), ct));
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<HeijunkaKpiResponseDto>> GetKpis([FromQuery] string siteCode, [FromQuery] Guid productionLineId, [FromQuery] DateTime fromDateLocal, [FromQuery] DateTime toDateLocal, CancellationToken ct)
    {
        if (!CanAccessSite(siteCode)) return Forbid();
        return Ok(await _service.GetPhase1KpisAsync(siteCode, productionLineId, fromDateLocal, toDateLocal, ct));
    }

    private bool CanAccessSite(string siteCode)
    {
        if (GetRoleTier() <= 2m) return true;
        if (Request.Headers.TryGetValue("X-User-Site-Code", out var callerSiteCode) &&
            string.Equals(callerSiteCode.ToString(), siteCode, StringComparison.OrdinalIgnoreCase))
            return true;
        if (Request.Headers.TryGetValue("X-User-Site-Id", out var siteIdHeader) &&
            Guid.TryParse(siteIdHeader, out _))
            return true;
        return false;
    }

    private bool CanPlan() => GetRoleTier() <= 5.0m;
    private bool CanManageMappings() => GetRoleTier() <= 4.0m;

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var idHeader) &&
            Guid.TryParse(idHeader, out var userId))
            return userId;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var claimUserId) ? claimUserId : Guid.Empty;
    }

    private decimal GetRoleTier()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var headerValue) &&
            decimal.TryParse(headerValue, out var roleTier))
            return roleTier;
        return 99m;
    }
}
