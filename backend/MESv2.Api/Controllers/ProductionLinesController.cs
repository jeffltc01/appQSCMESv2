using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/productionlines")]
public class ProductionLinesController : ControllerBase
{
    private readonly MesDbContext _db;

    public ProductionLinesController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductionLineDto>>> GetProductionLines([FromQuery] string siteCode, CancellationToken cancellationToken)
    {
        var list = await _db.ProductionLines
            .Include(l => l.Plant)
            .Where(l => l.Plant.Code == siteCode)
            .OrderBy(l => l.Name)
            .Select(l => new ProductionLineDto { Id = l.Id, Name = l.Name, PlantId = l.PlantId })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ProductionLineAdminDto>>> GetAllProductionLines(CancellationToken cancellationToken)
    {
        var list = await _db.ProductionLines
            .Include(l => l.Plant)
            .OrderBy(l => l.Plant.Code).ThenBy(l => l.Name)
            .Select(l => new ProductionLineAdminDto { Id = l.Id, Name = l.Name, PlantId = l.PlantId, PlantName = l.Plant.Name })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ProductionLineAdminDto>> Create([FromBody] CreateProductionLineDto dto, CancellationToken cancellationToken)
    {
        var plant = await _db.Plants.FindAsync(new object[] { dto.PlantId }, cancellationToken);
        if (plant == null) return BadRequest(new { message = "Invalid PlantId." });

        var entity = new ProductionLine
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            PlantId = dto.PlantId,
        };
        _db.ProductionLines.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ProductionLineAdminDto
        {
            Id = entity.Id,
            Name = entity.Name,
            PlantId = entity.PlantId,
            PlantName = plant.Name,
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductionLineAdminDto>> Update(Guid id, [FromBody] UpdateProductionLineDto dto, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductionLines.Include(l => l.Plant).FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (entity == null) return NotFound();

        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier) && callerTier > 2m)
        {
            if (!Request.Headers.TryGetValue("X-User-Site-Id", out var siteHeader) ||
                !Guid.TryParse(siteHeader, out var callerSiteId) ||
                entity.PlantId != callerSiteId)
                return StatusCode(403, new { message = "You do not have permission to modify this production line." });
        }

        var plant = await _db.Plants.FindAsync(new object[] { dto.PlantId }, cancellationToken);
        if (plant == null) return BadRequest(new { message = "Invalid PlantId." });

        entity.Name = dto.Name;
        entity.PlantId = dto.PlantId;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ProductionLineAdminDto
        {
            Id = entity.Id,
            Name = entity.Name,
            PlantId = entity.PlantId,
            PlantName = plant.Name,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.ProductionLines.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return NotFound();

        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier) && callerTier > 2m)
        {
            if (!Request.Headers.TryGetValue("X-User-Site-Id", out var siteHeader) ||
                !Guid.TryParse(siteHeader, out var callerSiteId) ||
                entity.PlantId != callerSiteId)
                return StatusCode(403, new { message = "You do not have permission to delete this production line." });
        }

        _db.ProductionLines.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
