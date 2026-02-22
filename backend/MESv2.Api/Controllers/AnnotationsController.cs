using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/annotations")]
public class AnnotationsController : ControllerBase
{
    private readonly MesDbContext _db;

    public AnnotationsController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminAnnotationDto>>> GetAll(
        [FromQuery] Guid? siteId,
        CancellationToken cancellationToken)
    {
        var query = _db.Annotations
            .Include(a => a.AnnotationType)
            .Include(a => a.InitiatedByUser)
            .Include(a => a.ResolvedByUser)
            .Include(a => a.ProductionRecord)
                .ThenInclude(pr => pr!.SerialNumber)
            .Include(a => a.ProductionRecord)
                .ThenInclude(pr => pr!.ProductionLine)
            .AsQueryable();

        if (siteId.HasValue)
        {
            query = query.Where(a =>
                (a.ProductionRecordId != null && a.ProductionRecord!.ProductionLine.PlantId == siteId.Value) ||
                (a.DowntimeEventId != null) ||
                (a.SerialNumberId != null));
        }

        var list = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminAnnotationDto
            {
                Id = a.Id,
                SerialNumber = a.ProductionRecord != null && a.ProductionRecord.SerialNumber != null
                    ? a.ProductionRecord.SerialNumber.Serial : "",
                AnnotationTypeName = a.AnnotationType.Name,
                AnnotationTypeId = a.AnnotationTypeId,
                Flag = a.Flag,
                Notes = a.Notes,
                InitiatedByName = a.InitiatedByUser.DisplayName,
                ResolvedByName = a.ResolvedByUser != null ? a.ResolvedByUser.DisplayName : null,
                ResolvedNotes = a.ResolvedNotes,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAnnotationDto>> Update(
        Guid id,
        [FromBody] UpdateAnnotationDto dto,
        CancellationToken cancellationToken)
    {
        var annotation = await _db.Annotations
            .Include(a => a.AnnotationType)
            .Include(a => a.InitiatedByUser)
            .Include(a => a.ProductionRecord)
                .ThenInclude(pr => pr!.SerialNumber)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (annotation == null) return NotFound();

        annotation.Flag = dto.Flag;
        annotation.Notes = dto.Notes;
        annotation.ResolvedNotes = dto.ResolvedNotes;
        if (dto.ResolvedByUserId.HasValue)
            annotation.ResolvedByUserId = dto.ResolvedByUserId;

        await _db.SaveChangesAsync(cancellationToken);

        var resolvedUser = annotation.ResolvedByUserId.HasValue
            ? await _db.Users.FindAsync(new object[] { annotation.ResolvedByUserId.Value }, cancellationToken)
            : null;

        return Ok(new AdminAnnotationDto
        {
            Id = annotation.Id,
            SerialNumber = annotation.ProductionRecord?.SerialNumber?.Serial ?? "",
            AnnotationTypeName = annotation.AnnotationType.Name,
            AnnotationTypeId = annotation.AnnotationTypeId,
            Flag = annotation.Flag,
            Notes = annotation.Notes,
            InitiatedByName = annotation.InitiatedByUser.DisplayName,
            ResolvedByName = resolvedUser?.DisplayName,
            ResolvedNotes = annotation.ResolvedNotes,
            CreatedAt = annotation.CreatedAt
        });
    }
}
