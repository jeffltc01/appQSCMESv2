using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/defect-locations")]
public class DefectLocationsController : ControllerBase
{
    private readonly MesDbContext _db;

    public DefectLocationsController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminDefectLocationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.DefectLocations
            .Include(d => d.Characteristic)
            .OrderBy(d => d.Code)
            .Select(d => new AdminDefectLocationDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                DefaultLocationDetail = d.DefaultLocationDetail,
                CharacteristicId = d.CharacteristicId,
                CharacteristicName = d.Characteristic != null ? d.Characteristic.Name : null,
                IsActive = d.IsActive
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminDefectLocationDto>> Create([FromBody] CreateDefectLocationDto dto, CancellationToken cancellationToken)
    {
        var loc = new DefectLocation
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            DefaultLocationDetail = dto.DefaultLocationDetail,
            CharacteristicId = dto.CharacteristicId
        };
        _db.DefectLocations.Add(loc);
        await _db.SaveChangesAsync(cancellationToken);

        var charName = dto.CharacteristicId.HasValue
            ? (await _db.Characteristics.FindAsync(new object[] { dto.CharacteristicId.Value }, cancellationToken))?.Name
            : null;

        return Ok(new AdminDefectLocationDto
        {
            Id = loc.Id, Code = loc.Code, Name = loc.Name,
            DefaultLocationDetail = loc.DefaultLocationDetail,
            CharacteristicId = loc.CharacteristicId,
            CharacteristicName = charName,
            IsActive = loc.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminDefectLocationDto>> Update(Guid id, [FromBody] UpdateDefectLocationDto dto, CancellationToken cancellationToken)
    {
        var loc = await _db.DefectLocations.FindAsync(new object[] { id }, cancellationToken);
        if (loc == null) return NotFound();

        loc.Code = dto.Code;
        loc.Name = dto.Name;
        loc.DefaultLocationDetail = dto.DefaultLocationDetail;
        loc.CharacteristicId = dto.CharacteristicId;
        loc.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        var charName = dto.CharacteristicId.HasValue
            ? (await _db.Characteristics.FindAsync(new object[] { dto.CharacteristicId.Value }, cancellationToken))?.Name
            : null;

        return Ok(new AdminDefectLocationDto
        {
            Id = loc.Id, Code = loc.Code, Name = loc.Name,
            DefaultLocationDetail = loc.DefaultLocationDetail,
            CharacteristicId = loc.CharacteristicId,
            CharacteristicName = charName,
            IsActive = loc.IsActive
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminDefectLocationDto>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var loc = await _db.DefectLocations.FindAsync(new object[] { id }, cancellationToken);
        if (loc == null) return NotFound();

        loc.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        var charName = loc.CharacteristicId.HasValue
            ? (await _db.Characteristics.FindAsync(new object[] { loc.CharacteristicId.Value }, cancellationToken))?.Name
            : null;

        return Ok(new AdminDefectLocationDto
        {
            Id = loc.Id, Code = loc.Code, Name = loc.Name,
            DefaultLocationDetail = loc.DefaultLocationDetail,
            CharacteristicId = loc.CharacteristicId,
            CharacteristicName = charName,
            IsActive = loc.IsActive
        });
    }
}
