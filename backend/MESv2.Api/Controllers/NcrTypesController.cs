using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/ncr-types")]
public class NcrTypesController : ControllerBase
{
    private readonly INcrService _ncrService;

    public NcrTypesController(INcrService ncrService)
    {
        _ncrService = ncrService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NcrTypeDto>>> GetList([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _ncrService.GetNcrTypesAsync(includeInactive, ct));

    [HttpPost]
    public async Task<ActionResult<NcrTypeDto>> Upsert([FromBody] UpsertNcrTypeRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.UpsertNcrTypeAsync(dto, ct));
}
