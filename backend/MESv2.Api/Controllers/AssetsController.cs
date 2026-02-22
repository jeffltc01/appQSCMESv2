using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

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
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets([FromQuery] Guid? workCenterId, [FromQuery] Guid? productionLineId, CancellationToken cancellationToken)
    {
        var query = _db.Assets.Where(a => a.IsActive);
        if (workCenterId.HasValue)
            query = query.Where(a => a.WorkCenterId == workCenterId.Value);
        if (productionLineId.HasValue)
            query = query.Where(a => a.ProductionLineId == productionLineId.Value);

        var list = await query
            .OrderBy(a => a.Name)
            .Select(a => new AssetDto { Id = a.Id, Name = a.Name, WorkCenterId = a.WorkCenterId })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminAssetDto>>> GetAllAssets(CancellationToken cancellationToken)
    {
        var list = await _db.Assets
            .Include(a => a.WorkCenter)
            .Include(a => a.ProductionLine)
            .OrderBy(a => a.WorkCenter.Name).ThenBy(a => a.Name)
            .Select(a => new AdminAssetDto
            {
                Id = a.Id,
                Name = a.Name,
                WorkCenterId = a.WorkCenterId,
                WorkCenterName = a.WorkCenter.Name,
                ProductionLineId = a.ProductionLineId,
                ProductionLineName = a.ProductionLine.Name + " (" + a.ProductionLine.Plant.Name + ")",
                LimbleIdentifier = a.LimbleIdentifier,
                LaneName = a.LaneName,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminAssetDto>> CreateAsset([FromBody] CreateAssetDto dto, CancellationToken cancellationToken)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            WorkCenterId = dto.WorkCenterId,
            ProductionLineId = dto.ProductionLineId,
            LimbleIdentifier = dto.LimbleIdentifier,
            LaneName = dto.LaneName
        };
        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);

        var wc = await _db.WorkCenters.FindAsync(new object[] { dto.WorkCenterId }, cancellationToken);
        var pl = await _db.ProductionLines.Include(p => p.Plant).FirstOrDefaultAsync(p => p.Id == dto.ProductionLineId, cancellationToken);
        return Ok(new AdminAssetDto { Id = asset.Id, Name = asset.Name, WorkCenterId = asset.WorkCenterId, WorkCenterName = wc?.Name ?? "", ProductionLineId = asset.ProductionLineId, ProductionLineName = pl != null ? $"{pl.Name} ({pl.Plant.Name})" : "", LimbleIdentifier = asset.LimbleIdentifier, LaneName = asset.LaneName, IsActive = asset.IsActive });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAssetDto>> UpdateAsset(Guid id, [FromBody] UpdateAssetDto dto, CancellationToken cancellationToken)
    {
        var asset = await _db.Assets.FindAsync(new object[] { id }, cancellationToken);
        if (asset == null) return NotFound();

        asset.Name = dto.Name;
        asset.WorkCenterId = dto.WorkCenterId;
        asset.ProductionLineId = dto.ProductionLineId;
        asset.LimbleIdentifier = dto.LimbleIdentifier;
        asset.LaneName = dto.LaneName;
        asset.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        var wc = await _db.WorkCenters.FindAsync(new object[] { dto.WorkCenterId }, cancellationToken);
        var pl = await _db.ProductionLines.Include(p => p.Plant).FirstOrDefaultAsync(p => p.Id == dto.ProductionLineId, cancellationToken);
        return Ok(new AdminAssetDto { Id = asset.Id, Name = asset.Name, WorkCenterId = asset.WorkCenterId, WorkCenterName = wc?.Name ?? "", ProductionLineId = asset.ProductionLineId, ProductionLineName = pl != null ? $"{pl.Name} ({pl.Plant.Name})" : "", LimbleIdentifier = asset.LimbleIdentifier, LaneName = asset.LaneName, IsActive = asset.IsActive });
    }
}
