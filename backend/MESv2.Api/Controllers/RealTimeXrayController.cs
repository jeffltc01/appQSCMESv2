using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/external/xray-inspection")]
public class RealTimeXrayController : ControllerBase
{
    private readonly IRealTimeXrayService _service;

    public RealTimeXrayController(IRealTimeXrayService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<XrayInspectionResponseDto>> Submit(
        [FromBody] XrayInspectionRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.ProcessInspectionAsync(dto, cancellationToken);
        return Ok(result);
    }
}
