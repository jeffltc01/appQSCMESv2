using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/control-plans")]
public class ControlPlansController : ControllerBase
{
    private static readonly HashSet<string> ValidResultTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PassFail", "AcceptReject", "GoNoGo", "NumericInt", "NumericDecimal", "Text"
    };

    private readonly MesDbContext _db;

    public ControlPlansController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminControlPlanDto>>> GetAll(
        [FromQuery] Guid? siteId,
        CancellationToken cancellationToken)
    {
        var query = _db.ControlPlans
            .Include(cp => cp.Characteristic)
            .Include(cp => cp.WorkCenterProductionLine).ThenInclude(wcpl => wcpl.WorkCenter)
            .Include(cp => cp.WorkCenterProductionLine).ThenInclude(wcpl => wcpl.ProductionLine).ThenInclude(pl => pl.Plant)
            .AsQueryable();

        if (siteId.HasValue)
            query = query.Where(cp => cp.WorkCenterProductionLine.ProductionLine.PlantId == siteId.Value);

        var list = await query
            .OrderBy(cp => cp.Characteristic.Name)
                .ThenBy(cp => cp.WorkCenterProductionLine.WorkCenter.Name)
            .Select(cp => new AdminControlPlanDto
            {
                Id = cp.Id,
                CharacteristicId = cp.CharacteristicId,
                CharacteristicName = cp.Characteristic.Name,
                WorkCenterProductionLineId = cp.WorkCenterProductionLineId,
                WorkCenterName = cp.WorkCenterProductionLine.WorkCenter.Name,
                ProductionLineName = cp.WorkCenterProductionLine.ProductionLine.Name,
                PlantId = cp.WorkCenterProductionLine.ProductionLine.PlantId,
                PlantName = cp.WorkCenterProductionLine.ProductionLine.Plant.Name,
                PlantCode = cp.WorkCenterProductionLine.ProductionLine.Plant.Code,
                IsEnabled = cp.IsEnabled,
                ResultType = cp.ResultType,
                IsGateCheck = cp.IsGateCheck,
                CodeRequired = cp.CodeRequired,
                IsActive = cp.IsActive
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminControlPlanDto>> Create([FromBody] CreateControlPlanDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
            return Forbid();

        if (!ValidResultTypes.Contains(dto.ResultType))
            return BadRequest($"Invalid ResultType. Must be one of: {string.Join(", ", ValidResultTypes)}");

        if (dto.CodeRequired && !dto.IsGateCheck)
            return BadRequest("CodeRequired control plans must also be gate checks.");

        if (dto.CodeRequired && !dto.IsEnabled)
            return BadRequest("CodeRequired control plans cannot be disabled.");

        var cp = new ControlPlan
        {
            Id = Guid.NewGuid(),
            CharacteristicId = dto.CharacteristicId,
            WorkCenterProductionLineId = dto.WorkCenterProductionLineId,
            IsEnabled = dto.IsEnabled,
            ResultType = dto.ResultType,
            IsGateCheck = dto.IsGateCheck,
            CodeRequired = dto.CodeRequired
        };
        _db.ControlPlans.Add(cp);
        await _db.SaveChangesAsync(cancellationToken);

        var wcpl = await _db.WorkCenterProductionLines
            .Include(w => w.WorkCenter)
            .Include(w => w.ProductionLine).ThenInclude(pl => pl.Plant)
            .FirstAsync(w => w.Id == cp.WorkCenterProductionLineId, cancellationToken);
        var charName = (await _db.Characteristics.FindAsync(new object[] { cp.CharacteristicId }, cancellationToken))?.Name ?? "";

        return Ok(new AdminControlPlanDto
        {
            Id = cp.Id,
            CharacteristicId = cp.CharacteristicId,
            CharacteristicName = charName,
            WorkCenterProductionLineId = cp.WorkCenterProductionLineId,
            WorkCenterName = wcpl.WorkCenter.Name,
            ProductionLineName = wcpl.ProductionLine.Name,
            PlantId = wcpl.ProductionLine.PlantId,
            PlantName = wcpl.ProductionLine.Plant.Name,
            PlantCode = wcpl.ProductionLine.Plant.Code,
            IsEnabled = cp.IsEnabled,
            ResultType = cp.ResultType,
            IsGateCheck = cp.IsGateCheck,
            CodeRequired = cp.CodeRequired,
            IsActive = cp.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminControlPlanDto>> Update(Guid id, [FromBody] UpdateControlPlanDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
            return Forbid();

        if (!ValidResultTypes.Contains(dto.ResultType))
            return BadRequest($"Invalid ResultType. Must be one of: {string.Join(", ", ValidResultTypes)}");

        if (dto.CodeRequired && !dto.IsGateCheck)
            return BadRequest("CodeRequired control plans must also be gate checks.");

        if (dto.CodeRequired && !dto.IsEnabled)
            return BadRequest("CodeRequired control plans cannot be disabled.");

        var cp = await _db.ControlPlans
            .Include(x => x.Characteristic)
            .Include(x => x.WorkCenterProductionLine).ThenInclude(w => w.WorkCenter)
            .Include(x => x.WorkCenterProductionLine).ThenInclude(w => w.ProductionLine).ThenInclude(pl => pl.Plant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cp == null) return NotFound();

        cp.IsEnabled = dto.IsEnabled;
        cp.ResultType = dto.ResultType;
        cp.IsGateCheck = dto.IsGateCheck;
        cp.CodeRequired = dto.CodeRequired;
        cp.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminControlPlanDto
        {
            Id = cp.Id,
            CharacteristicId = cp.CharacteristicId,
            CharacteristicName = cp.Characteristic.Name,
            WorkCenterProductionLineId = cp.WorkCenterProductionLineId,
            WorkCenterName = cp.WorkCenterProductionLine.WorkCenter.Name,
            ProductionLineName = cp.WorkCenterProductionLine.ProductionLine.Name,
            PlantId = cp.WorkCenterProductionLine.ProductionLine.PlantId,
            PlantName = cp.WorkCenterProductionLine.ProductionLine.Plant.Name,
            PlantCode = cp.WorkCenterProductionLine.ProductionLine.Plant.Code,
            IsEnabled = cp.IsEnabled,
            ResultType = cp.ResultType,
            IsGateCheck = cp.IsGateCheck,
            CodeRequired = cp.CodeRequired,
            IsActive = cp.IsActive
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminControlPlanDto>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAdmin())
            return Forbid();

        var cp = await _db.ControlPlans
            .Include(x => x.Characteristic)
            .Include(x => x.WorkCenterProductionLine).ThenInclude(w => w.WorkCenter)
            .Include(x => x.WorkCenterProductionLine).ThenInclude(w => w.ProductionLine).ThenInclude(pl => pl.Plant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cp == null) return NotFound();

        cp.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminControlPlanDto
        {
            Id = cp.Id,
            CharacteristicId = cp.CharacteristicId,
            CharacteristicName = cp.Characteristic.Name,
            WorkCenterProductionLineId = cp.WorkCenterProductionLineId,
            WorkCenterName = cp.WorkCenterProductionLine.WorkCenter.Name,
            ProductionLineName = cp.WorkCenterProductionLine.ProductionLine.Name,
            PlantId = cp.WorkCenterProductionLine.ProductionLine.PlantId,
            PlantName = cp.WorkCenterProductionLine.ProductionLine.Plant.Name,
            PlantCode = cp.WorkCenterProductionLine.ProductionLine.Plant.Code,
            IsEnabled = cp.IsEnabled,
            ResultType = cp.ResultType,
            IsGateCheck = cp.IsGateCheck,
            CodeRequired = cp.CodeRequired,
            IsActive = cp.IsActive
        });
    }

    private bool IsAdmin()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 1m;
        return false;
    }
}
