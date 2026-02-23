using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MESv2.Api.Models;

namespace MESv2.Api.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> ExcludedEntityTypes = new()
    {
        typeof(AuditLog),
        typeof(ChangeLog),
        typeof(ActiveSession),
        typeof(XrayShotCounter),
        typeof(PrintLog),
        typeof(QueueTransaction),
    };

    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(User.PinHash),
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not MesDbContext db)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var userId = GetCurrentUserId();
        var utcNow = DateTime.UtcNow;

        var entries = db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => !ExcludedEntityTypes.Contains(e.Entity.GetType()))
            .ToList();

        foreach (var entry in entries)
        {
            var entityId = GetEntityId(entry);
            if (entityId == Guid.Empty)
                continue;

            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => null,
            };

            if (action is null)
                continue;

            var changes = BuildChangesJson(entry);

            if (action == "Updated" && changes is null)
                continue;

            db.AuditLogs.Add(new AuditLog
            {
                Action = action,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entityId,
                Changes = changes,
                ChangedByUserId = userId,
                ChangedAtUtc = utcNow,
            });
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static Guid GetEntityId(EntityEntry entry)
    {
        var pk = entry.Properties
            .FirstOrDefault(p => p.Metadata.IsPrimaryKey());

        if (pk?.CurrentValue is Guid guid)
            return guid;

        return Guid.Empty;
    }

    private static string? BuildChangesJson(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        switch (entry.State)
        {
            case EntityState.Added:
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey()) continue;
                    if (IsNavigationOrSensitive(prop)) continue;
                    dict[prop.Metadata.Name] = new { old = (object?)null, @new = Stringify(prop.CurrentValue) };
                }
                break;

            case EntityState.Modified:
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified) continue;
                    if (prop.Metadata.IsPrimaryKey()) continue;
                    if (IsNavigationOrSensitive(prop)) continue;

                    var orig = Stringify(prop.OriginalValue);
                    var curr = Stringify(prop.CurrentValue);
                    if (orig == curr) continue;

                    dict[prop.Metadata.Name] = new { old = orig, @new = curr };
                }
                break;

            case EntityState.Deleted:
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey()) continue;
                    if (IsNavigationOrSensitive(prop)) continue;
                    dict[prop.Metadata.Name] = new { old = Stringify(prop.OriginalValue), @new = (object?)null };
                }
                break;
        }

        if (dict.Count == 0)
            return null;

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private static bool IsNavigationOrSensitive(PropertyEntry prop)
    {
        return SensitiveProperties.Contains(prop.Metadata.Name);
    }

    private static string? Stringify(object? value)
    {
        return value switch
        {
            null => null,
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            bool b => b.ToString().ToLowerInvariant(),
            _ => value.ToString(),
        };
    }
}
