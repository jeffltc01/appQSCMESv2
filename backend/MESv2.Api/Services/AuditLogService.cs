using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class AuditLogService : IAuditLogService
{
    private readonly MesDbContext _db;

    public AuditLogService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLogPageDto> GetLogsAsync(
        string? entityName,
        Guid? entityId,
        string? action,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (userId.HasValue)
            query = query.Where(a => a.ChangedByUserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.ChangedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.ChangedAtUtc <= to.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.ChangedAtUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Changes = a.Changes,
                ChangedByUserName = a.ChangedByUser != null ? a.ChangedByUser.DisplayName : null,
                ChangedByUserId = a.ChangedByUserId,
                ChangedAtUtc = a.ChangedAtUtc,
            })
            .ToListAsync(ct);

        return new AuditLogPageDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<List<string>> GetEntityNamesAsync(CancellationToken ct)
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
    }
}
