using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/limble")]
public class LimbleController : ControllerBase
{
    private readonly ILimbleService _limbleService;
    private readonly MesDbContext _db;
    private readonly ILogger<LimbleController> _logger;

    public LimbleController(ILimbleService limbleService, MesDbContext db, ILogger<LimbleController> logger)
    {
        _limbleService = limbleService;
        _db = db;
        _logger = logger;
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<List<LimbleStatusDto>>> GetStatuses(CancellationToken cancellationToken)
    {
        try
        {
            var statuses = await _limbleService.GetStatusesAsync(cancellationToken);
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Limble statuses");
            return StatusCode(502, new { message = "Failed to fetch statuses from Limble." });
        }
    }

    [HttpGet("my-requests")]
    public async Task<ActionResult<List<LimbleTaskDto>>> GetMyRequests([FromQuery] string empNo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empNo))
            return BadRequest(new { message = "empNo is required." });

        try
        {
            var tasks = await _limbleService.GetMyRequestsAsync(empNo, cancellationToken);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Limble requests for {EmpNo}", empNo);
            return StatusCode(502, new { message = "Failed to fetch requests from Limble." });
        }
    }

    [HttpPost("work-requests")]
    public async Task<ActionResult<LimbleTaskDto>> CreateWorkRequest([FromBody] CreateLimbleWorkRequestInputDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest(new { message = "Subject is required." });
        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Description is required." });

        var employeeNo = User.FindFirstValue("employeeNumber") ?? "";
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var defaultSiteId = User.FindFirstValue("defaultSiteId");

        if (string.IsNullOrWhiteSpace(employeeNo))
            return Unauthorized(new { message = "Employee number not found in token." });

        string locationId;
        if (!string.IsNullOrWhiteSpace(defaultSiteId) && Guid.TryParse(defaultSiteId, out var siteGuid))
        {
            var plant = await _db.Plants.AsNoTracking().FirstOrDefaultAsync(p => p.Id == siteGuid, cancellationToken);
            locationId = plant?.LimbleLocationId ?? "";
        }
        else
        {
            locationId = "";
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            _logger.LogWarning("No LimbleLocationId configured for plant {SiteId}", defaultSiteId);
            return BadRequest(new { message = "No Limble Location ID configured for this plant. Contact an administrator." });
        }

        try
        {
            var result = await _limbleService.CreateWorkRequestAsync(new CreateLimbleWorkRequestDto
            {
                Subject = dto.Subject,
                Description = dto.Description,
                Priority = dto.Priority,
                RequestedDueDate = dto.RequestedDueDate,
                LocationId = locationId,
                EmployeeNo = employeeNo,
                DisplayName = displayName
            }, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Limble work request");
            return StatusCode(502, new { message = "Failed to create work request in Limble." });
        }
    }
}
