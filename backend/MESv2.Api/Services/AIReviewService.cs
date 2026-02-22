using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class AIReviewService : IAIReviewService
{
    private static readonly Guid AIReviewAnnotationTypeId =
        Guid.Parse("a1000002-0000-0000-0000-000000000002");

    private readonly MesDbContext _db;
    private readonly ILogger<AIReviewService> _logger;

    public AIReviewService(MesDbContext db, ILogger<AIReviewService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AIReviewRecordDto>> GetRecordsAsync(
        Guid wcId, Guid plantId, string date, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber)
            .Include(r => r.SerialNumber!.Product)
            .Include(r => r.Operator)
            .Include(r => r.ProductionLine)
            .Where(r => r.WorkCenterId == wcId
                        && r.Timestamp >= startOfDay
                        && r.Timestamp < endOfDay
                        && r.ProductionLine.PlantId == plantId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
            return Array.Empty<AIReviewRecordDto>();

        var recordIds = records.Select(r => r.Id).ToList();
        var reviewedIds = await _db.Annotations
            .Where(a => a.ProductionRecordId != null
                        && recordIds.Contains(a.ProductionRecordId.Value)
                        && a.AnnotationTypeId == AIReviewAnnotationTypeId)
            .Select(a => a.ProductionRecordId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var reviewedSet = reviewedIds.ToHashSet();

        return records.Select(r => new AIReviewRecordDto
        {
            Id = r.Id,
            Timestamp = r.Timestamp,
            SerialOrIdentifier = r.SerialNumber?.Serial ?? r.Id.ToString("N")[..8],
            TankSize = r.SerialNumber?.Product?.TankSize.ToString(),
            OperatorName = r.Operator?.DisplayName ?? string.Empty,
            AlreadyReviewed = reviewedSet.Contains(r.Id),
        }).ToList();
    }

    public async Task<AIReviewResultDto> SubmitReviewAsync(
        Guid userId, CreateAIReviewRequest request, CancellationToken cancellationToken = default)
    {
        var existingReviewed = await _db.Annotations
            .Where(a => a.ProductionRecordId != null
                        && request.ProductionRecordIds.Contains(a.ProductionRecordId.Value)
                        && a.AnnotationTypeId == AIReviewAnnotationTypeId)
            .Select(a => a.ProductionRecordId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var alreadyReviewedSet = existingReviewed.ToHashSet();
        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var recordId in request.ProductionRecordIds)
        {
            if (alreadyReviewedSet.Contains(recordId))
                continue;

            _db.Annotations.Add(new Annotation
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = recordId,
                AnnotationTypeId = AIReviewAnnotationTypeId,
                Flag = true,
                Notes = request.Comment,
                InitiatedByUserId = userId,
                CreatedAt = now,
            });
            created++;
        }

        if (created > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AI Review: User {UserId} reviewed {Count} records", userId, created);

        return new AIReviewResultDto { AnnotationsCreated = created };
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(
        Guid plantId, CancellationToken cancellationToken)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
