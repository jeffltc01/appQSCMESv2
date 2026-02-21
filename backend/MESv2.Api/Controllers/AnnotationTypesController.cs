using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/annotation-types")]
public class AnnotationTypesController : ControllerBase
{
    private readonly MesDbContext _db;

    public AnnotationTypesController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminAnnotationTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.AnnotationTypes
            .OrderBy(a => a.Name)
            .Select(a => new AdminAnnotationTypeDto
            {
                Id = a.Id,
                Name = a.Name,
                Abbreviation = a.Abbreviation,
                RequiresResolution = a.RequiresResolution,
                OperatorCanCreate = a.OperatorCanCreate,
                DisplayColor = a.DisplayColor,
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminAnnotationTypeDto>> Create([FromBody] CreateAnnotationTypeDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var entity = new AnnotationType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Abbreviation = dto.Abbreviation,
            RequiresResolution = dto.RequiresResolution,
            OperatorCanCreate = dto.OperatorCanCreate,
            DisplayColor = dto.DisplayColor,
        };
        _db.AnnotationTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAnnotationTypeDto>> Update(Guid id, [FromBody] UpdateAnnotationTypeDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var entity = await _db.AnnotationTypes.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return NotFound();

        entity.Name = dto.Name;
        entity.Abbreviation = dto.Abbreviation;
        entity.RequiresResolution = dto.RequiresResolution;
        entity.OperatorCanCreate = dto.OperatorCanCreate;
        entity.DisplayColor = dto.DisplayColor;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(entity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var entity = await _db.AnnotationTypes.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return NotFound();

        _db.AnnotationTypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private bool IsAdmin()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 1m;
        return false;
    }

    private static AdminAnnotationTypeDto MapToDto(AnnotationType a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Abbreviation = a.Abbreviation,
        RequiresResolution = a.RequiresResolution,
        OperatorCanCreate = a.OperatorCanCreate,
        DisplayColor = a.DisplayColor,
    };
}
