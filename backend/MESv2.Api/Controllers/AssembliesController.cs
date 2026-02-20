using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/assemblies")]
public class AssembliesController : ControllerBase
{
    private readonly IAssemblyService _assemblyService;

    public AssembliesController(IAssemblyService assemblyService)
    {
        _assemblyService = assemblyService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateAssemblyResponseDto>> Create([FromBody] CreateAssemblyDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _assemblyService.CreateAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{alphaCode}/reassemble")]
    public async Task<ActionResult<CreateAssemblyResponseDto>> Reassemble(string alphaCode, [FromBody] ReassemblyDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _assemblyService.ReassembleAsync(alphaCode, dto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
