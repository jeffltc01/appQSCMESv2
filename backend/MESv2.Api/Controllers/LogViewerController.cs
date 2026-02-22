using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/logs")]
public class LogViewerController : ControllerBase
{
    private readonly ILogViewerService _logViewerService;
    private readonly MesDbContext _db;

    public LogViewerController(ILogViewerService logViewerService, MesDbContext db)
    {
        _logViewerService = logViewerService;
        _db = db;
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
        [FromBody] CreateLogAnnotationDto dto,
        CancellationToken ct)
    {
        var productionRecord = await _db.ProductionRecords
            .FirstOrDefaultAsync(r => r.Id == dto.ProductionRecordId, ct);
        if (productionRecord == null)
            return NotFound(new { message = "Production record not found." });

        var annotationType = await _db.AnnotationTypes
            .FirstOrDefaultAsync(t => t.Id == dto.AnnotationTypeId, ct);
        if (annotationType == null)
            return BadRequest(new { message = "Invalid annotation type." });

        var user = await _db.Users.FindAsync(new object[] { dto.InitiatedByUserId }, ct);
        if (user == null)
            return BadRequest(new { message = "Invalid user." });

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = dto.ProductionRecordId,
            AnnotationTypeId = dto.AnnotationTypeId,
            Flag = true,
            Notes = dto.Notes,
            InitiatedByUserId = dto.InitiatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Annotations.Add(annotation);
        await _db.SaveChangesAsync(ct);

        var serial = await _db.SerialNumbers
            .Where(s => s.Id == productionRecord.SerialNumberId)
            .Select(s => s.Serial)
            .FirstOrDefaultAsync(ct);

        return Ok(new AdminAnnotationDto
        {
            Id = annotation.Id,
            SerialNumber = serial ?? "",
            AnnotationTypeName = annotationType.Name,
            AnnotationTypeId = annotationType.Id,
            Flag = annotation.Flag,
            Notes = annotation.Notes,
            InitiatedByName = user.DisplayName,
            CreatedAt = annotation.CreatedAt
        });
    }
}
