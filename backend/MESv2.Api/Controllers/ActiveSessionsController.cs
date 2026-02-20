using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/active-sessions")]
public class ActiveSessionsController : ControllerBase
{
    private readonly MesDbContext _db;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    public ActiveSessionsController(MesDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActiveSessionDto>>> GetBySite([FromQuery] string siteCode, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var list = await _db.ActiveSessions
            .Include(s => s.User)
            .Include(s => s.WorkCenter)
            .Include(s => s.ProductionLine)
            .Where(s => s.SiteCode == siteCode)
            .OrderBy(s => s.ProductionLine.Name).ThenBy(s => s.WorkCenter.Name)
            .Select(s => new ActiveSessionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserDisplayName = s.User.DisplayName,
                EmployeeNumber = s.User.EmployeeNumber,
                SiteCode = s.SiteCode,
                ProductionLineId = s.ProductionLineId,
                ProductionLineName = s.ProductionLine.Name,
                WorkCenterId = s.WorkCenterId,
                WorkCenterName = s.WorkCenter.Name,
                LoginDateTime = s.LoginDateTime,
                LastHeartbeatDateTime = s.LastHeartbeatDateTime,
                IsStale = (now - s.LastHeartbeatDateTime) > StaleThreshold
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult> Upsert([FromBody] CreateActiveSessionDto dto, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized();

        var existing = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.UserId == userId.Value, cancellationToken);

        var now = DateTime.UtcNow;
        if (existing != null)
        {
            existing.SiteCode = dto.SiteCode;
            existing.ProductionLineId = dto.ProductionLineId;
            existing.WorkCenterId = dto.WorkCenterId;
            existing.AssetId = dto.AssetId;
            existing.LoginDateTime = now;
            existing.LastHeartbeatDateTime = now;
        }
        else
        {
            _db.ActiveSessions.Add(new ActiveSession
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                SiteCode = dto.SiteCode,
                ProductionLineId = dto.ProductionLineId,
                WorkCenterId = dto.WorkCenterId,
                AssetId = dto.AssetId,
                LoginDateTime = now,
                LastHeartbeatDateTime = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("heartbeat")]
    public async Task<ActionResult> Heartbeat(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized();

        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.UserId == userId.Value, cancellationToken);

        if (session == null) return NotFound();

        session.LastHeartbeatDateTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<ActionResult> EndSession(CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized();

        var session = await _db.ActiveSessions
            .FirstOrDefaultAsync(s => s.UserId == userId.Value, cancellationToken);

        if (session != null)
        {
            _db.ActiveSessions.Remove(session);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    private Guid? GetUserIdFromToken()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;
        return null;
    }
}
