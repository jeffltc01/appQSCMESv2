using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/nameplate-records")]
public class NameplateRecordsController : ControllerBase
{
    private readonly INameplateService _nameplateService;

    public NameplateRecordsController(INameplateService nameplateService)
    {
        _nameplateService = nameplateService;
    }

    [HttpPost]
    public async Task<ActionResult<NameplateRecordResponseDto>> Create([FromBody] CreateNameplateRecordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _nameplateService.CreateAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{serialNumber}")]
    public async Task<ActionResult<NameplateRecordResponseDto>> GetBySerial(string serialNumber, CancellationToken cancellationToken)
    {
        var result = await _nameplateService.GetBySerialAsync(serialNumber, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Nameplate serial number not found" });
        return Ok(result);
    }

    [HttpPost("{id:guid}/reprint")]
    public async Task<ActionResult> Reprint(Guid id, CancellationToken cancellationToken)
    {
        await _nameplateService.ReprintAsync(id, cancellationToken);
        return NoContent();
    }
}
