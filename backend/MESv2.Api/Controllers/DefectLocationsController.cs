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
            .Include(d => d.DefectLocationCharacteristics)
            .ThenInclude(link => link.Characteristic)
            .Include(d => d.Characteristic)
            .OrderBy(d => d.Code)
            .Select(d => MapDto(d))
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminDefectLocationDto>> Create([FromBody] CreateDefectLocationDto dto, CancellationToken cancellationToken)
    {
        var characteristicIds = dto.CharacteristicIds.Distinct().ToList();

        var loc = new DefectLocation
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            DefaultLocationDetail = dto.DefaultLocationDetail
        };
        _db.DefectLocations.Add(loc);

        if (characteristicIds.Count > 0)
        {
            _db.DefectLocationCharacteristics.AddRange(characteristicIds.Select(charId => new DefectLocationCharacteristic
            {
                Id = Guid.NewGuid(),
                DefectLocationId = loc.Id,
                CharacteristicId = charId
            }));
        }

        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.DefectLocations
            .Include(d => d.DefectLocationCharacteristics)
            .ThenInclude(link => link.Characteristic)
            .Include(d => d.Characteristic)
            .SingleAsync(d => d.Id == loc.Id, cancellationToken);

        return Ok(MapDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminDefectLocationDto>> Update(Guid id, [FromBody] UpdateDefectLocationDto dto, CancellationToken cancellationToken)
    {
        var loc = await _db.DefectLocations.FindAsync(new object[] { id }, cancellationToken);
        if (loc == null) return NotFound();

        loc.Code = dto.Code;
        loc.Name = dto.Name;
        loc.DefaultLocationDetail = dto.DefaultLocationDetail;
        loc.IsActive = dto.IsActive;

        var desiredCharacteristicIds = dto.CharacteristicIds.Distinct().ToHashSet();
        var existingLinks = await _db.DefectLocationCharacteristics
            .Where(link => link.DefectLocationId == id)
            .ToListAsync(cancellationToken);

        _db.DefectLocationCharacteristics.RemoveRange(existingLinks);
        if (desiredCharacteristicIds.Count > 0)
        {
            _db.DefectLocationCharacteristics.AddRange(desiredCharacteristicIds.Select(charId => new DefectLocationCharacteristic
            {
                Id = Guid.NewGuid(),
                DefectLocationId = loc.Id,
                CharacteristicId = charId
            }));
        }

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.DefectLocations
            .Include(d => d.DefectLocationCharacteristics)
            .ThenInclude(link => link.Characteristic)
            .Include(d => d.Characteristic)
            .SingleAsync(d => d.Id == id, cancellationToken);

        return Ok(MapDto(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminDefectLocationDto>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var loc = await _db.DefectLocations.FindAsync(new object[] { id }, cancellationToken);
        if (loc == null) return NotFound();

        loc.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        var updated = await _db.DefectLocations
            .Include(d => d.DefectLocationCharacteristics)
            .ThenInclude(link => link.Characteristic)
            .Include(d => d.Characteristic)
            .SingleAsync(d => d.Id == id, cancellationToken);

        return Ok(MapDto(updated));
    }

    private static AdminDefectLocationDto MapDto(DefectLocation loc)
    {
        var links = loc.DefectLocationCharacteristics
            .OrderBy(link => link.Characteristic.Name)
            .ToList();
        var fallbackCharacteristicIds = !links.Any() && loc.CharacteristicId.HasValue
            ? new List<Guid> { loc.CharacteristicId.Value }
            : new List<Guid>();
        var fallbackCharacteristicNames = !links.Any() && loc.Characteristic != null
            ? new List<string> { loc.Characteristic.Name }
            : new List<string>();

        return new AdminDefectLocationDto
        {
            Id = loc.Id,
            Code = loc.Code,
            Name = loc.Name,
            DefaultLocationDetail = loc.DefaultLocationDetail,
            CharacteristicIds = links.Any() ? links.Select(link => link.CharacteristicId).ToList() : fallbackCharacteristicIds,
            CharacteristicNames = links.Any() ? links.Select(link => link.Characteristic.Name).ToList() : fallbackCharacteristicNames,
            IsActive = loc.IsActive
        };
    }
}
