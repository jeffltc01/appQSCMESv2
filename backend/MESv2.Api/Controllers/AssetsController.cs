using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly MesDbContext _db;

    public AssetsController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets([FromQuery] Guid workCenterId, CancellationToken cancellationToken)
    {
        var list = await _db.Assets
            .Where(a => a.WorkCenterId == workCenterId)
            .OrderBy(a => a.Name)
            .Select(a => new AssetDto { Id = a.Id, Name = a.Name, WorkCenterId = a.WorkCenterId })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }
}
