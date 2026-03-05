using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/plant-printers")]
public class PlantPrintersController : ControllerBase
{
    private readonly MesDbContext _db;
    private readonly INiceLabelService _niceLabelService;

    public PlantPrintersController(MesDbContext db, INiceLabelService niceLabelService)
    {
        _db = db;
        _niceLabelService = niceLabelService;
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
                DocumentPath = pp.DocumentPath,
                Enabled = pp.Enabled,
                PrintLocation = pp.PrintLocation,
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("nicelabel-printers")]
    public async Task<ActionResult<IEnumerable<NiceLabelPrinterDto>>> GetNiceLabelPrinters(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (success, printers, errorMessage) = await _niceLabelService.GetPrintersAsync();
        if (!success)
            return StatusCode(502, new { message = $"Failed to fetch printers from NiceLabel. {errorMessage}" });

        var dto = printers
            .Select(p => new NiceLabelPrinterDto { PrinterName = p })
            .ToList();
        return Ok(dto);
    }

    [HttpGet("nicelabel-documents")]
    public async Task<ActionResult<IEnumerable<NiceLabelDocumentDto>>> GetNiceLabelDocuments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (success, documents, errorMessage) = await _niceLabelService.GetDocumentsAsync();
        if (!success)
            return StatusCode(502, new { message = $"Failed to fetch documents from NiceLabel. {errorMessage}" });

        var dto = documents
            .Select(d => new NiceLabelDocumentDto
            {
                Name = d.Name,
                ItemPath = d.ItemPath
            })
            .ToList();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<AdminPlantPrinterDto>> Create([FromBody] CreatePlantPrinterDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var plant = await _db.Plants.FindAsync(new object[] { dto.PlantId }, cancellationToken);
        if (plant == null) return BadRequest(new { message = "Invalid plant." });

        var locationConflict = await _db.PlantPrinters.AnyAsync(
            pp => pp.PlantId == dto.PlantId && pp.PrintLocation == dto.PrintLocation,
            cancellationToken);
        if (locationConflict)
            return Conflict(new { message = "A route already exists for this plant and print location." });

        var entity = new PlantPrinter
        {
            Id = Guid.NewGuid(),
            PlantId = dto.PlantId,
            PrinterName = dto.PrinterName,
            DocumentPath = dto.DocumentPath,
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
            DocumentPath = entity.DocumentPath,
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

        var locationConflict = await _db.PlantPrinters.AnyAsync(
            pp => pp.Id != id && pp.PlantId == entity.PlantId && pp.PrintLocation == dto.PrintLocation,
            cancellationToken);
        if (locationConflict)
            return Conflict(new { message = "A route already exists for this plant and print location." });

        entity.PrinterName = dto.PrinterName;
        entity.DocumentPath = dto.DocumentPath;
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
            DocumentPath = entity.DocumentPath,
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
