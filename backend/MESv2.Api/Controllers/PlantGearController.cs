using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/plant-gear")]
public class PlantGearController : ControllerBase
{
    private readonly MesDbContext _db;

    public PlantGearController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlantWithGearDto>>> GetAll(CancellationToken cancellationToken)
    {
        var plants = await _db.Plants
            .OrderBy(p => p.Code)
            .Select(p => new PlantWithGearDto
            {
                PlantId = p.Id,
                PlantName = p.Name,
                PlantCode = p.Code,
                CurrentPlantGearId = p.CurrentPlantGearId,
                LimbleLocationId = p.LimbleLocationId,
                Gears = _db.PlantGears
                    .Where(g => g.PlantId == p.Id)
                    .OrderBy(g => g.Level)
                    .Select(g => new PlantGearDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Level = g.Level,
                        PlantId = g.PlantId
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var plant in plants)
        {
            if (plant.CurrentPlantGearId.HasValue)
            {
                plant.CurrentGearLevel = plant.Gears
                    .FirstOrDefault(g => g.Id == plant.CurrentPlantGearId)?.Level;
            }
        }

        return Ok(plants);
    }

    [HttpPut("{plantId:guid}")]
    public async Task<ActionResult> SetGear(Guid plantId, [FromBody] SetPlantGearDto dto, CancellationToken cancellationToken)
    {
        var plant = await _db.Plants.FindAsync(new object[] { plantId }, cancellationToken);
        if (plant == null) return NotFound();

        var gear = await _db.PlantGears.FindAsync(new object[] { dto.PlantGearId }, cancellationToken);
        if (gear == null || gear.PlantId != plantId)
            return BadRequest(new { message = "Invalid gear for this plant." });

        plant.CurrentPlantGearId = dto.PlantGearId;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{plantId:guid}/limble")]
    public async Task<ActionResult> SetLimbleLocationId(Guid plantId, [FromBody] UpdatePlantLimbleDto dto, CancellationToken cancellationToken)
    {
        var plant = await _db.Plants.FindAsync(new object[] { plantId }, cancellationToken);
        if (plant == null) return NotFound();

        plant.LimbleLocationId = dto.LimbleLocationId;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
