using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class FrontendTelemetryService : IFrontendTelemetryService
{
    private const int MaxMessageLength = 2048;
    private const int MaxStackLength = 8000;
    private const int MaxMetadataLength = 8000;
    private const int ArchiveBatchSize = 5000;
    private readonly MesDbContext _db;

    public FrontendTelemetryService(MesDbContext db)
    {
        _db = db;
    }

    public async Task IngestAsync(FrontendTelemetryIngestDto dto, CancellationToken ct)
    {
        var occurredAtUtc = dto.OccurredAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var now = DateTime.UtcNow;
        var entity = new FrontendTelemetryEvent
        {
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = now,
            Category = Normalize(dto.Category, "unknown", 64),
            Source = Normalize(dto.Source, "unknown", 64),
            Severity = Normalize(dto.Severity, "error", 32),
            IsReactRuntimeOverlayCandidate = dto.IsReactRuntimeOverlayCandidate,
            Route = NormalizeOptional(dto.Route, 256),
            Screen = NormalizeOptional(dto.Screen, 128),
            Message = Normalize(dto.Message, "No message provided", MaxMessageLength),
            Stack = NormalizeOptional(dto.Stack, MaxStackLength),
            MetadataJson = NormalizeOptional(dto.MetadataJson, MaxMetadataLength),
            SessionId = NormalizeOptional(dto.SessionId, 128),
            CorrelationId = NormalizeOptional(dto.CorrelationId, 128),
            ApiPath = NormalizeOptional(dto.ApiPath, 256),
            HttpMethod = NormalizeOptional(dto.HttpMethod, 16),
            HttpStatus = dto.HttpStatus,
            UserId = dto.UserId,
            WorkCenterId = dto.WorkCenterId,
            ProductionLineId = dto.ProductionLineId,
            PlantId = dto.PlantId
        };

        _db.FrontendTelemetryEvents.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<FrontendTelemetryPageDto> GetEventsAsync(
        string? category,
        string? source,
        string? severity,
        Guid? userId,
        Guid? workCenterId,
        DateTime? from,
        DateTime? to,
        bool reactRuntimeOnly,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.FrontendTelemetryEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(e => e.Source == source);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => e.Severity == severity);

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        if (workCenterId.HasValue)
            query = query.Where(e => e.WorkCenterId == workCenterId.Value);

        if (from.HasValue)
            query = query.Where(e => e.OccurredAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAtUtc <= to.Value);

        if (reactRuntimeOnly)
            query = query.Where(e => e.IsReactRuntimeOverlayCandidate);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new FrontendTelemetryEventDto
            {
                Id = e.Id,
                OccurredAtUtc = e.OccurredAtUtc,
                ReceivedAtUtc = e.ReceivedAtUtc,
                Category = e.Category,
                Source = e.Source,
                Severity = e.Severity,
                IsReactRuntimeOverlayCandidate = e.IsReactRuntimeOverlayCandidate,
                Message = e.Message,
                Stack = e.Stack,
                Route = e.Route,
                Screen = e.Screen,
                MetadataJson = e.MetadataJson,
                SessionId = e.SessionId,
                CorrelationId = e.CorrelationId,
                ApiPath = e.ApiPath,
                HttpMethod = e.HttpMethod,
                HttpStatus = e.HttpStatus,
                UserId = e.UserId,
                UserDisplayName = e.User != null ? e.User.DisplayName : null,
                WorkCenterId = e.WorkCenterId,
                WorkCenterName = e.WorkCenter != null ? e.WorkCenter.Name : null,
                ProductionLineId = e.ProductionLineId,
                ProductionLineName = e.ProductionLine != null ? e.ProductionLine.Name : null,
                PlantId = e.PlantId,
                PlantName = e.Plant != null ? e.Plant.Name : null,
            })
            .ToListAsync(ct);

        return new FrontendTelemetryPageDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<FrontendTelemetryFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct)
    {
        var categories = await _db.FrontendTelemetryEvents
            .AsNoTracking()
            .Select(e => e.Category)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(ct);

        var sources = await _db.FrontendTelemetryEvents
            .AsNoTracking()
            .Select(e => e.Source)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(ct);

        var severities = await _db.FrontendTelemetryEvents
            .AsNoTracking()
            .Select(e => e.Severity)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(ct);

        return new FrontendTelemetryFilterOptionsDto
        {
            Categories = categories,
            Sources = sources,
            Severities = severities,
        };
    }

    public async Task<FrontendTelemetryCountDto> GetCountAsync(long warningThreshold, CancellationToken ct)
    {
        var rowCount = await _db.FrontendTelemetryEvents.LongCountAsync(ct);
        return new FrontendTelemetryCountDto
        {
            RowCount = rowCount,
            WarningThreshold = warningThreshold
        };
    }

    public async Task<FrontendTelemetryArchiveResultDto> ArchiveOldestAsync(int keepRows, CancellationToken ct)
    {
        var keep = Math.Max(1, keepRows);
        var totalRows = await _db.FrontendTelemetryEvents.LongCountAsync(ct);
        var deleteRows = totalRows - keep;
        if (deleteRows <= 0)
        {
            return new FrontendTelemetryArchiveResultDto
            {
                DeletedRows = 0,
                RemainingRows = totalRows
            };
        }

        var deletedRows = 0;
        var remainingToDelete = deleteRows;

        while (remainingToDelete > 0)
        {
            var batchSize = (int)Math.Min(ArchiveBatchSize, remainingToDelete);
            var batch = await _db.FrontendTelemetryEvents
                .OrderBy(e => e.OccurredAtUtc)
                .ThenBy(e => e.Id)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            _db.FrontendTelemetryEvents.RemoveRange(batch);
            var affected = await _db.SaveChangesAsync(ct);

            if (affected == 0)
                break;

            deletedRows += affected;
            remainingToDelete -= affected;
        }

        var remainingRows = await _db.FrontendTelemetryEvents.LongCountAsync(ct);
        return new FrontendTelemetryArchiveResultDto
        {
            DeletedRows = deletedRows,
            RemainingRows = remainingRows
        };
    }

    private static string Normalize(string? value, string fallback, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Trim()[..Math.Min(value.Trim().Length, maxLen)];
    }

    private static string? NormalizeOptional(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim()[..Math.Min(value.Trim().Length, maxLen)];
    }
}
