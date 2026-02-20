using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api")]
public class HydroRecordsController : ControllerBase
{
    private readonly IHydroService _hydroService;

    public HydroRecordsController(IHydroService hydroService)
    {
        _hydroService = hydroService;
    }

    [HttpPost("hydro-records")]
    public async Task<ActionResult<HydroRecordResponseDto>> Create([FromBody] CreateHydroRecordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _hydroService.CreateAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("characteristics/{id:guid}/locations")]
    public async Task<ActionResult<IEnumerable<DefectLocationDto>>> GetLocationsByCharacteristic(Guid id, CancellationToken cancellationToken)
    {
        var result = await _hydroService.GetLocationsByCharacteristicAsync(id, cancellationToken);
        return Ok(result);
    }
}
