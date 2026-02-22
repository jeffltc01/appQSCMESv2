using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

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
                (a.SerialNumberId != null) ||
                (a.LinkedEntityId != null));
        }

        var rawList = await query
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
                CreatedAt = a.CreatedAt,
                LinkedEntityType = a.LinkedEntityType,
                LinkedEntityId = a.LinkedEntityId,
            })
            .ToListAsync(cancellationToken);

        await ResolveLinkedEntityNames(rawList, cancellationToken);

        return Ok(rawList);
    }

    [HttpPost]
    public async Task<ActionResult<AdminAnnotationDto>> Create(
        [FromBody] CreateAnnotationDto dto,
        CancellationToken cancellationToken)
    {
        var annotationType = await _db.AnnotationTypes.FindAsync(new object[] { dto.AnnotationTypeId }, cancellationToken);
        if (annotationType == null)
            return BadRequest(new { message = "Invalid annotation type." });

        var user = await _db.Users.FindAsync(new object[] { dto.InitiatedByUserId }, cancellationToken);
        if (user == null)
            return BadRequest(new { message = "Invalid user." });

        if (!string.IsNullOrEmpty(dto.LinkedEntityType) && dto.LinkedEntityId.HasValue)
        {
            var valid = dto.LinkedEntityType switch
            {
                "Plant" => await _db.Plants.AnyAsync(p => p.Id == dto.LinkedEntityId, cancellationToken),
                "ProductionLine" => await _db.ProductionLines.AnyAsync(p => p.Id == dto.LinkedEntityId, cancellationToken),
                "WorkCenter" => await _db.WorkCenters.AnyAsync(w => w.Id == dto.LinkedEntityId, cancellationToken),
                _ => false
            };
            if (!valid)
                return BadRequest(new { message = $"{dto.LinkedEntityType} not found." });
        }

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            AnnotationTypeId = dto.AnnotationTypeId,
            Flag = true,
            Notes = dto.Notes,
            InitiatedByUserId = dto.InitiatedByUserId,
            CreatedAt = DateTime.UtcNow,
            LinkedEntityType = dto.LinkedEntityType,
            LinkedEntityId = dto.LinkedEntityId,
        };

        _db.Annotations.Add(annotation);
        await _db.SaveChangesAsync(cancellationToken);

        var result = new AdminAnnotationDto
        {
            Id = annotation.Id,
            SerialNumber = "",
            AnnotationTypeName = annotationType.Name,
            AnnotationTypeId = annotationType.Id,
            Flag = annotation.Flag,
            Notes = annotation.Notes,
            InitiatedByName = user.DisplayName,
            CreatedAt = annotation.CreatedAt,
            LinkedEntityType = annotation.LinkedEntityType,
            LinkedEntityId = annotation.LinkedEntityId,
        };

        await ResolveLinkedEntityNames(new List<AdminAnnotationDto> { result }, cancellationToken);

        return Ok(result);
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

        var result = new AdminAnnotationDto
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
            CreatedAt = annotation.CreatedAt,
            LinkedEntityType = annotation.LinkedEntityType,
            LinkedEntityId = annotation.LinkedEntityId,
        };

        await ResolveLinkedEntityNames(new List<AdminAnnotationDto> { result }, cancellationToken);

        return Ok(result);
    }

    private async Task ResolveLinkedEntityNames(
        List<AdminAnnotationDto> items,
        CancellationToken cancellationToken)
    {
        var linked = items.Where(i => i.LinkedEntityId.HasValue && !string.IsNullOrEmpty(i.LinkedEntityType)).ToList();
        if (linked.Count == 0) return;

        var plantIds = linked.Where(i => i.LinkedEntityType == "Plant").Select(i => i.LinkedEntityId!.Value).Distinct().ToList();
        var lineIds = linked.Where(i => i.LinkedEntityType == "ProductionLine").Select(i => i.LinkedEntityId!.Value).Distinct().ToList();
        var wcIds = linked.Where(i => i.LinkedEntityType == "WorkCenter").Select(i => i.LinkedEntityId!.Value).Distinct().ToList();

        var plantNames = plantIds.Count > 0
            ? await _db.Plants.Where(p => plantIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var lineNames = lineIds.Count > 0
            ? await _db.ProductionLines.Where(p => lineIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        var wcNames = wcIds.Count > 0
            ? await _db.WorkCenters.Where(w => wcIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        foreach (var item in linked)
        {
            var id = item.LinkedEntityId!.Value;
            item.LinkedEntityName = item.LinkedEntityType switch
            {
                "Plant" => plantNames.GetValueOrDefault(id),
                "ProductionLine" => lineNames.GetValueOrDefault(id),
                "WorkCenter" => wcNames.GetValueOrDefault(id),
                _ => null
            };
        }
    }
}
