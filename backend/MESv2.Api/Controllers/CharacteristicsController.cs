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
            .OrderBy(c => c.Code)
            .Select(c => new AdminCharacteristicDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                SpecHigh = c.SpecHigh,
                SpecLow = c.SpecLow,
                SpecTarget = c.SpecTarget,
                MinTankSize = c.MinTankSize,
                ProductTypeId = c.ProductTypeId,
                ProductTypeName = c.ProductType != null ? c.ProductType.Name : null,
                WorkCenterIds = _db.CharacteristicWorkCenters
                    .Where(cw => cw.CharacteristicId == c.Id)
                    .Select(cw => cw.WorkCenterId)
                    .ToList(),
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCharacteristicDto>> Create([FromBody] CreateCharacteristicDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
            return Forbid();

        var c = new Characteristic
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            SpecHigh = dto.SpecHigh,
            SpecLow = dto.SpecLow,
            SpecTarget = dto.SpecTarget,
            MinTankSize = dto.MinTankSize,
            ProductTypeId = dto.ProductTypeId
        };
        _db.Characteristics.Add(c);

        foreach (var wcId in dto.WorkCenterIds)
        {
            _db.CharacteristicWorkCenters.Add(new CharacteristicWorkCenter
            {
                Id = Guid.NewGuid(),
                CharacteristicId = c.Id,
                WorkCenterId = wcId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var ptName = dto.ProductTypeId.HasValue
            ? (await _db.ProductTypes.FindAsync(new object[] { dto.ProductTypeId.Value }, cancellationToken))?.Name
            : null;

        return Ok(new AdminCharacteristicDto
        {
            Id = c.Id, Code = c.Code, Name = c.Name,
            SpecHigh = c.SpecHigh, SpecLow = c.SpecLow, SpecTarget = c.SpecTarget,
            MinTankSize = c.MinTankSize,
            ProductTypeId = c.ProductTypeId, ProductTypeName = ptName,
            WorkCenterIds = dto.WorkCenterIds,
            IsActive = c.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminCharacteristicDto>> Update(Guid id, [FromBody] UpdateCharacteristicDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
            return Forbid();

        var c = await _db.Characteristics.Include(x => x.ProductType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (c == null) return NotFound();

        c.Code = dto.Code;
        c.Name = dto.Name;
        c.SpecHigh = dto.SpecHigh;
        c.SpecLow = dto.SpecLow;
        c.SpecTarget = dto.SpecTarget;
        c.MinTankSize = dto.MinTankSize;
        c.ProductTypeId = dto.ProductTypeId;
        c.IsActive = dto.IsActive;

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
            Id = c.Id, Code = c.Code, Name = c.Name,
            SpecHigh = c.SpecHigh, SpecLow = c.SpecLow, SpecTarget = c.SpecTarget,
            MinTankSize = c.MinTankSize,
            ProductTypeId = c.ProductTypeId, ProductTypeName = ptName,
            WorkCenterIds = dto.WorkCenterIds,
            IsActive = c.IsActive
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminCharacteristicDto>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
            return Forbid();

        var c = await _db.Characteristics.FindAsync(new object[] { id }, cancellationToken);
        if (c == null) return NotFound();

        c.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        var ptName = c.ProductTypeId.HasValue
            ? (await _db.ProductTypes.FindAsync(new object[] { c.ProductTypeId.Value }, cancellationToken))?.Name
            : null;
        var wcIds = await _db.CharacteristicWorkCenters
            .Where(cw => cw.CharacteristicId == id)
            .Select(cw => cw.WorkCenterId)
            .ToListAsync(cancellationToken);

        return Ok(new AdminCharacteristicDto
        {
            Id = c.Id, Code = c.Code, Name = c.Name,
            SpecHigh = c.SpecHigh, SpecLow = c.SpecLow, SpecTarget = c.SpecTarget,
            MinTankSize = c.MinTankSize,
            ProductTypeId = c.ProductTypeId, ProductTypeName = ptName,
            WorkCenterIds = wcIds,
            IsActive = c.IsActive
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

    private bool IsAdmin()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 1m;
        return false;
    }
}
