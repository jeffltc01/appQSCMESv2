using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/sites")]
public class SitesController : ControllerBase
{
    private readonly MesDbContext _db;

    public SitesController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlantDto>>> GetSites(CancellationToken cancellationToken)
    {
        var list = await _db.Plants.OrderBy(p => p.Code).ToListAsync(cancellationToken);
        return Ok(list.Select(p => new PlantDto { Id = p.Id, Code = p.Code, Name = p.Name, TimeZoneId = p.TimeZoneId, NextTankAlphaCode = p.NextTankAlphaCode }));
    }
}
