using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

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
}
