using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/characteristics")]
public class CharacteristicsController : ControllerBase
{
    private readonly MesDbContext _db;

    public CharacteristicsController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminCharacteristicDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.Characteristics
            .Include(c => c.ProductType)
            .OrderBy(c => c.Name)
            .Select(c => new AdminCharacteristicDto
            {
                Id = c.Id,
                Name = c.Name,
                SpecHigh = c.SpecHigh,
                SpecLow = c.SpecLow,
                SpecTarget = c.SpecTarget,
                ProductTypeId = c.ProductTypeId,
                ProductTypeName = c.ProductType != null ? c.ProductType.Name : null,
                WorkCenterIds = _db.CharacteristicWorkCenters
                    .Where(cw => cw.CharacteristicId == c.Id)
                    .Select(cw => cw.WorkCenterId)
                    .ToList()
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCharacteristicDto>> Update(Guid id, [FromBody] UpdateCharacteristicDto dto, CancellationToken cancellationToken)
    {
        var c = await _db.Characteristics.Include(x => x.ProductType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (c == null) return NotFound();

        c.Name = dto.Name;
        c.SpecHigh = dto.SpecHigh;
        c.SpecLow = dto.SpecLow;
        c.SpecTarget = dto.SpecTarget;
        c.ProductTypeId = dto.ProductTypeId;

        var existingLinks = await _db.CharacteristicWorkCenters.Where(cw => cw.CharacteristicId == id).ToListAsync(cancellationToken);
        _db.CharacteristicWorkCenters.RemoveRange(existingLinks);

        foreach (var wcId in dto.WorkCenterIds)
        {
            _db.CharacteristicWorkCenters.Add(new CharacteristicWorkCenter
            {
                Id = Guid.NewGuid(),
                CharacteristicId = id,
                WorkCenterId = wcId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var ptName = dto.ProductTypeId.HasValue
            ? (await _db.ProductTypes.FindAsync(new object[] { dto.ProductTypeId.Value }, cancellationToken))?.Name
            : null;

        return Ok(new AdminCharacteristicDto
        {
            Id = c.Id, Name = c.Name, SpecHigh = c.SpecHigh, SpecLow = c.SpecLow, SpecTarget = c.SpecTarget,
            ProductTypeId = c.ProductTypeId, ProductTypeName = ptName,
            WorkCenterIds = dto.WorkCenterIds
        });
    }

    [HttpGet("{id:guid}/locations")]
    public async Task<ActionResult<IEnumerable<DefectLocationDto>>> GetLocations(Guid id, CancellationToken cancellationToken)
    {
        var list = await _db.DefectLocations
            .Where(d => d.CharacteristicId == id || d.CharacteristicId == null)
            .OrderBy(d => d.Code)
            .Select(d => new DefectLocationDto { Id = d.Id, Code = d.Code, Name = d.Name })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }
}
