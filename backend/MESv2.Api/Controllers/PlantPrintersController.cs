using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/plant-printers")]
public class PlantPrintersController : ControllerBase
{
    private readonly MesDbContext _db;

    public PlantPrintersController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminPlantPrinterDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.PlantPrinters
            .Include(pp => pp.Plant)
            .OrderBy(pp => pp.Plant.Code)
            .ThenBy(pp => pp.PrinterName)
            .Select(pp => new AdminPlantPrinterDto
            {
                Id = pp.Id,
                PlantId = pp.PlantId,
                PlantName = pp.Plant.Name,
                PlantCode = pp.Plant.Code,
                PrinterName = pp.PrinterName,
                Enabled = pp.Enabled,
                PrintLocation = pp.PrintLocation,
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminPlantPrinterDto>> Create([FromBody] CreatePlantPrinterDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var plant = await _db.Plants.FindAsync(new object[] { dto.PlantId }, cancellationToken);
        if (plant == null) return BadRequest(new { message = "Invalid plant." });

        var entity = new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = dto.PlantId,
            PrinterName = dto.PrinterName,
            Enabled = dto.Enabled,
            PrintLocation = dto.PrintLocation,
        };
        _db.PlantPrinters.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminPlantPrinterDto
        {
            Id = entity.Id,
            PlantId = entity.PlantId,
            PlantName = plant.Name,
            PlantCode = plant.Code,
            PrinterName = entity.PrinterName,
            Enabled = entity.Enabled,
            PrintLocation = entity.PrintLocation,
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminPlantPrinterDto>> Update(Guid id, [FromBody] UpdatePlantPrinterDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var entity = await _db.PlantPrinters
            .Include(pp => pp.Plant)
            .FirstOrDefaultAsync(pp => pp.Id == id, cancellationToken);
        if (entity == null) return NotFound();

        entity.PrinterName = dto.PrinterName;
        entity.Enabled = dto.Enabled;
        entity.PrintLocation = dto.PrintLocation;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminPlantPrinterDto
        {
            Id = entity.Id,
            PlantId = entity.PlantId,
            PlantName = entity.Plant.Name,
            PlantCode = entity.Plant.Code,
            PrinterName = entity.PrinterName,
            Enabled = entity.Enabled,
            PrintLocation = entity.PrintLocation,
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var entity = await _db.PlantPrinters.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return NotFound();

        _db.PlantPrinters.Remove(entity);
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
}
