using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/defect-codes")]
public class DefectCodesController : ControllerBase
{
    private readonly MesDbContext _db;

    public DefectCodesController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminDefectCodeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var codes = await _db.DefectCodes
            .OrderBy(d => d.Code)
            .Select(d => new AdminDefectCodeDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Severity = d.Severity,
                SystemType = d.SystemType,
                WorkCenterIds = _db.DefectWorkCenters
                    .Where(dw => dw.DefectCodeId == d.Id)
                    .Select(dw => dw.WorkCenterId)
                    .ToList()
            })
            .ToListAsync(cancellationToken);
        return Ok(codes);
    }

    [HttpPost]
    public async Task<ActionResult<AdminDefectCodeDto>> Create([FromBody] CreateDefectCodeDto dto, CancellationToken cancellationToken)
    {
        var code = new DefectCode
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            Severity = dto.Severity,
            SystemType = dto.SystemType
        };
        _db.DefectCodes.Add(code);

        foreach (var wcId in dto.WorkCenterIds)
        {
            _db.DefectWorkCenters.Add(new DefectWorkCenter
            {
                Id = Guid.NewGuid(),
                DefectCodeId = code.Id,
                WorkCenterId = wcId,
                EarliestDetectionWorkCenterId = wcId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminDefectCodeDto
        {
            Id = code.Id, Code = code.Code, Name = code.Name,
            Severity = code.Severity, SystemType = code.SystemType,
            WorkCenterIds = dto.WorkCenterIds
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminDefectCodeDto>> Update(Guid id, [FromBody] UpdateDefectCodeDto dto, CancellationToken cancellationToken)
    {
        var code = await _db.DefectCodes.FindAsync(new object[] { id }, cancellationToken);
        if (code == null) return NotFound();

        code.Code = dto.Code;
        code.Name = dto.Name;
        code.Severity = dto.Severity;
        code.SystemType = dto.SystemType;

        var existing = await _db.DefectWorkCenters.Where(dw => dw.DefectCodeId == id).ToListAsync(cancellationToken);
        _db.DefectWorkCenters.RemoveRange(existing);

        foreach (var wcId in dto.WorkCenterIds)
        {
            _db.DefectWorkCenters.Add(new DefectWorkCenter
            {
                Id = Guid.NewGuid(),
                DefectCodeId = id,
                WorkCenterId = wcId,
                EarliestDetectionWorkCenterId = wcId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminDefectCodeDto
        {
            Id = code.Id, Code = code.Code, Name = code.Name,
            Severity = code.Severity, SystemType = code.SystemType,
            WorkCenterIds = dto.WorkCenterIds
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var code = await _db.DefectCodes.FindAsync(new object[] { id }, cancellationToken);
        if (code == null) return NotFound();

        var junctions = await _db.DefectWorkCenters.Where(dw => dw.DefectCodeId == id).ToListAsync(cancellationToken);
        _db.DefectWorkCenters.RemoveRange(junctions);
        _db.DefectCodes.Remove(code);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
