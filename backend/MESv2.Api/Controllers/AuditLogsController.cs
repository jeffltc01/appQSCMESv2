using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly MesDbContext _db;

    public AuditLogsController(IAuditLogService auditLogService, MesDbContext db)
    {
        _auditLogService = auditLogService;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogPageDto>> GetLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 3.0m)
            return Forbid();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var result = await _auditLogService.GetLogsAsync(
            entityName, entityId, action, userId, from, to, page, pageSize, ct);

        return Ok(result);
    }

    [HttpGet("entity-names")]
    public async Task<ActionResult<List<string>>> GetEntityNames(CancellationToken ct)
    {
        var roleTier = await GetCallerRoleTier(ct);
        if (roleTier > 3.0m)
            return Forbid();

        var names = await _auditLogService.GetEntityNamesAsync(ct);
        return Ok(names);
    }

    private async Task<decimal> GetCallerRoleTier(CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return 99m;

        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.RoleTier })
            .FirstOrDefaultAsync(ct);

        return user?.RoleTier ?? 99m;
    }
}
