using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/logs")]
public class LogViewerController : ControllerBase
{
    private readonly ILogViewerService _logViewerService;
    private readonly IAnnotationService _annotationService;

    public LogViewerController(ILogViewerService logViewerService, IAnnotationService annotationService)
    {
        _logViewerService = logViewerService;
        _annotationService = annotationService;
    }

    [HttpGet("rolls")]
    public async Task<ActionResult<List<RollsLogEntryDto>>> GetRollsLog(
        [FromQuery] Guid siteId, [FromQuery] string startDate, [FromQuery] string endDate,
        CancellationToken ct)
    {
        var result = await _logViewerService.GetRollsLogAsync(siteId, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("fitup")]
    public async Task<ActionResult<List<FitupLogEntryDto>>> GetFitupLog(
        [FromQuery] Guid siteId, [FromQuery] string startDate, [FromQuery] string endDate,
        CancellationToken ct)
    {
        var result = await _logViewerService.GetFitupLogAsync(siteId, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("hydro")]
    public async Task<ActionResult<List<HydroLogEntryDto>>> GetHydroLog(
        [FromQuery] Guid siteId, [FromQuery] string startDate, [FromQuery] string endDate,
        CancellationToken ct)
    {
        var result = await _logViewerService.GetHydroLogAsync(siteId, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("rt-xray")]
    public async Task<ActionResult<List<RtXrayLogEntryDto>>> GetRtXrayLog(
        [FromQuery] Guid siteId, [FromQuery] string startDate, [FromQuery] string endDate,
        CancellationToken ct)
    {
        var result = await _logViewerService.GetRtXrayLogAsync(siteId, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpGet("spot-xray")]
    public async Task<ActionResult<SpotXrayLogResponseDto>> GetSpotXrayLog(
        [FromQuery] Guid siteId, [FromQuery] string startDate, [FromQuery] string endDate,
        CancellationToken ct)
    {
        var result = await _logViewerService.GetSpotXrayLogAsync(siteId, startDate, endDate, ct);
        return Ok(result);
    }

    [HttpPost("annotations")]
    public async Task<ActionResult<AdminAnnotationDto>> CreateAnnotation(
        [FromBody] CreateLogAnnotationDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _annotationService.CreateForProductionRecordAsync(dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
