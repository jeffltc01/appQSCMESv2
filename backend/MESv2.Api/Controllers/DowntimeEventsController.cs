using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/downtime-events")]
public class DowntimeEventsController : ControllerBase
{
    private readonly IDowntimeService _downtimeService;

    public DowntimeEventsController(IDowntimeService downtimeService)
    {
        _downtimeService = downtimeService;
    }

    [HttpPost]
    public async Task<ActionResult<DowntimeEventDto>> Create([FromBody] CreateDowntimeEventDto dto, CancellationToken cancellationToken)
    {
        var initiatedByUserId = GetUserId();
        if (initiatedByUserId == Guid.Empty)
            return StatusCode(401, new { message = "User not identified." });

        var result = await _downtimeService.CreateDowntimeEventAsync(dto, initiatedByUserId, cancellationToken);
        return StatusCode(201, result);
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var token = authHeader.ToString().Replace("Bearer ", "");
            if (Guid.TryParse(token, out var userId))
                return userId;
        }
        return Guid.Empty;
    }
}
