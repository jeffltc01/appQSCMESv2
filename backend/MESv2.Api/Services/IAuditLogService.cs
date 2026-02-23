using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IAuditLogService
{
    Task<AuditLogPageDto> GetLogsAsync(
        string? entityName,
        Guid? entityId,
        string? action,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<List<string>> GetEntityNamesAsync(CancellationToken ct);
}
