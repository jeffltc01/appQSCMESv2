using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/control-plans")]
public class ControlPlansController : ControllerBase
{
    private readonly MesDbContext _db;

    public ControlPlansController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminControlPlanDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _db.ControlPlans
            .Include(cp => cp.Characteristic)
            .Include(cp => cp.WorkCenter)
            .OrderBy(cp => cp.Characteristic.Name).ThenBy(cp => cp.WorkCenter.Name)
            .Select(cp => new AdminControlPlanDto
            {
                Id = cp.Id,
                CharacteristicId = cp.CharacteristicId,
                CharacteristicName = cp.Characteristic.Name,
                WorkCenterId = cp.WorkCenterId,
                WorkCenterName = cp.WorkCenter.Name,
                IsEnabled = cp.IsEnabled,
                ResultType = cp.ResultType,
                IsGateCheck = cp.IsGateCheck
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminControlPlanDto>> Update(Guid id, [FromBody] UpdateControlPlanDto dto, CancellationToken cancellationToken)
    {
        var cp = await _db.ControlPlans
            .Include(x => x.Characteristic)
            .Include(x => x.WorkCenter)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cp == null) return NotFound();

        cp.IsEnabled = dto.IsEnabled;
        cp.ResultType = dto.ResultType;
        cp.IsGateCheck = dto.IsGateCheck;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminControlPlanDto
        {
            Id = cp.Id,
            CharacteristicId = cp.CharacteristicId,
            CharacteristicName = cp.Characteristic.Name,
            WorkCenterId = cp.WorkCenterId,
            WorkCenterName = cp.WorkCenter.Name,
            IsEnabled = cp.IsEnabled,
            ResultType = cp.ResultType,
            IsGateCheck = cp.IsGateCheck
        });
    }
}
