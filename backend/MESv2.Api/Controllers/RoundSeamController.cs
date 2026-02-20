using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api")]
public class RoundSeamController : ControllerBase
{
    private readonly IRoundSeamService _roundSeamService;

    public RoundSeamController(IRoundSeamService roundSeamService)
    {
        _roundSeamService = roundSeamService;
    }

    [HttpGet("workcenters/{id:guid}/round-seam-setup")]
    public async Task<ActionResult<RoundSeamSetupDto>> GetSetup(Guid id, CancellationToken cancellationToken)
    {
        var setup = await _roundSeamService.GetSetupAsync(id, cancellationToken);
        if (setup == null)
            return Ok(new RoundSeamSetupDto { IsComplete = false });
        return Ok(setup);
    }

    [HttpPost("workcenters/{id:guid}/round-seam-setup")]
    public async Task<ActionResult<RoundSeamSetupDto>> SaveSetup(Guid id, [FromBody] CreateRoundSeamSetupDto dto, CancellationToken cancellationToken)
    {
        var result = await _roundSeamService.SaveSetupAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("production-records/round-seam")]
    public async Task<ActionResult<CreateProductionRecordResponseDto>> CreateRecord([FromBody] CreateRoundSeamRecordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roundSeamService.CreateRoundSeamRecordAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("serial-numbers/{serial}/assembly")]
    public async Task<ActionResult<AssemblyLookupDto>> GetAssemblyByShell(string serial, CancellationToken cancellationToken)
    {
        var result = await _roundSeamService.GetAssemblyByShellAsync(serial, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Shell is not part of any assembly" });
        return Ok(result);
    }
}
