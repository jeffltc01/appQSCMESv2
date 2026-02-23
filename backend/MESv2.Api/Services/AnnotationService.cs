using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class AnnotationService : IAnnotationService
{
    private readonly MesDbContext _db;

    public AnnotationService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<List<AdminAnnotationDto>> GetAllAsync(Guid? siteId, CancellationToken ct = default)
    {
        var query = _db.Annotations
            .Include(a => a.AnnotationType)
            .Include(a => a.InitiatedByUser)
            .Include(a => a.ResolvedByUser)
            .Include(a => a.ProductionRecord)
                .ThenInclude(pr => pr!.SerialNumber)
            .Include(a => a.ProductionRecord)
                .ThenInclude(pr => pr!.ProductionLine)
            .AsQueryable();

        if (siteId.HasValue)
        {
            query = query.Where(a =>
                (a.ProductionRecordId != null && a.ProductionRecord!.ProductionLine.PlantId == siteId.Value) ||
                (a.DowntimeEventId != null) ||
                (a.SerialNumberId != null) ||
                (a.LinkedEntityId != null));
        }

        var rawList = await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdminAnnotationDto
            {
                Id = a.Id,
                SerialNumber = a.ProductionRecord != null && a.ProductionRecord.SerialNumber != null
                    ? a.ProductionRecord.SerialNumber.Serial : "",
                AnnotationTypeName = a.AnnotationType.Name,
                AnnotationTypeId = a.AnnotationTypeId,
                Status = a.Status.ToString(),
                Notes = a.Notes,
                InitiatedByName = a.InitiatedByUser.DisplayName,
                ResolvedByName = a.ResolvedByUser != null ? a.ResolvedByUser.DisplayName : null,
                ResolvedNotes = a.ResolvedNotes,
                CreatedAt = a.CreatedAt,
                LinkedEntityType = a.LinkedEntityType,
                LinkedEntityId = a.LinkedEntityId,
            })
            .ToListAsync(ct);

        await ResolveLinkedEntityNames(rawList, ct);

        return rawList;
    }

    public async Task<AdminAnnotationDto> CreateAsync(CreateAnnotationDto dto, CancellationToken ct = default)
    {
        var annotationType = await _db.AnnotationTypes.FindAsync(new object[] { dto.AnnotationTypeId }, ct);
        if (annotationType == null)
            throw new ArgumentException("Invalid annotation type.");

        var user = await _db.Users.FindAsync(new object[] { dto.InitiatedByUserId }, ct);
        if (user == null)
            throw new ArgumentException("Invalid user.");

        if (!string.IsNullOrEmpty(dto.LinkedEntityType) && dto.LinkedEntityId.HasValue)
        {
            var valid = dto.LinkedEntityType switch
            {
                "Plant" => await _db.Plants.AnyAsync(p => p.Id == dto.LinkedEntityId, ct),
                "ProductionLine" => await _db.ProductionLines.AnyAsync(p => p.Id == dto.LinkedEntityId, ct),
                "WorkCenter" => await _db.WorkCenters.AnyAsync(w => w.Id == dto.LinkedEntityId, ct),
                _ => false
            };
            if (!valid)
                throw new ArgumentException($"{dto.LinkedEntityType} not found.");
        }

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            AnnotationTypeId = dto.AnnotationTypeId,
            Status = AnnotationStatus.Open,
            Notes = dto.Notes,
            InitiatedByUserId = dto.InitiatedByUserId,
            CreatedAt = DateTime.UtcNow,
            LinkedEntityType = dto.LinkedEntityType,
            LinkedEntityId = dto.LinkedEntityId,
        };

        _db.Annotations.Add(annotation);
        await _db.SaveChangesAsync(ct);

        var result = new AdminAnnotationDto
        {
            Id = annotation.Id,
            SerialNumber = "",
            AnnotationTypeName = annotationType.Name,
            AnnotationTypeId = annotationType.Id,
            Status = annotation.Status.ToString(),
            Notes = annotation.Notes,
            InitiatedByName = user.DisplayName,
            CreatedAt = annotation.CreatedAt,
            LinkedEntityType = annotation.LinkedEntityType,
            LinkedEntityId = annotation.LinkedEntityId,
        };

        await ResolveLinkedEntityNames(new List<AdminAnnotationDto> { result }, ct);

        return result;
    }

    public async Task<AdminAnnotationDto> CreateForProductionRecordAsync(CreateLogAnnotationDto dto, CancellationToken ct = default)
    {
        var productionRecord = await _db.ProductionRecords
            .FirstOrDefaultAsync(r => r.Id == dto.ProductionRecordId, ct);
        if (productionRecord == null)
            throw new InvalidOperationException("Production record not found.");

        var annotationType = await _db.AnnotationTypes
            .FirstOrDefaultAsync(t => t.Id == dto.AnnotationTypeId, ct);
        if (annotationType == null)
            throw new ArgumentException("Invalid annotation type.");

        var user = await _db.Users.FindAsync(new object[] { dto.InitiatedByUserId }, ct);
        if (user == null)
            throw new ArgumentException("Invalid user.");

        var annotation = new Annotation
        {
            Id = Guid.NewGuid(),
            ProductionRecordId = dto.ProductionRecordId,
            AnnotationTypeId = dto.AnnotationTypeId,
            Status = AnnotationStatus.Open,
            Notes = dto.Notes,
            InitiatedByUserId = dto.InitiatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Annotations.Add(annotation);
        await _db.SaveChangesAsync(ct);

        var serial = await _db.SerialNumbers
            .Where(s => s.Id == productionRecord.SerialNumberId)
            .Select(s => s.Serial)
            .FirstOrDefaultAsync(ct);

        return new AdminAnnotationDto
        {
            Id = annotation.Id,
            SerialNumber = serial ?? "",
            AnnotationTypeName = annotationType.Name,
            AnnotationTypeId = annotationType.Id,
            Status = annotation.Status.ToString(),
            Notes = annotation.Notes,
            InitiatedByName = user.DisplayName,
            CreatedAt = annotation.CreatedAt
        };
    }

    public async Task<AdminAnnotationDto?> UpdateAsync(Guid id, UpdateAnnotationDto dto, CancellationToken ct = default)
    {
        var annotation = await _db.Annotations
            .Include(a => a.AnnotationType)
            .Include(a => a.InitiatedByUser)
            .Include(a => a.ProductionRecord)
                .ThenInclude(pr => pr!.SerialNumber)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (annotation == null) return null;

        if (Enum.TryParse<AnnotationStatus>(dto.Status, true, out var parsed))
            annotation.Status = parsed;
        annotation.Notes = dto.Notes;
        annotation.ResolvedNotes = dto.ResolvedNotes;
        if (dto.ResolvedByUserId.HasValue)
            annotation.ResolvedByUserId = dto.ResolvedByUserId;

        await _db.SaveChangesAsync(ct);

        var resolvedUser = annotation.ResolvedByUserId.HasValue
            ? await _db.Users.FindAsync(new object[] { annotation.ResolvedByUserId.Value }, ct)
            : null;

        var result = new AdminAnnotationDto
        {
            Id = annotation.Id,
            SerialNumber = annotation.ProductionRecord?.SerialNumber?.Serial ?? "",
            AnnotationTypeName = annotation.AnnotationType.Name,
            AnnotationTypeId = annotation.AnnotationTypeId,
            Status = annotation.Status.ToString(),
            Notes = annotation.Notes,
            InitiatedByName = annotation.InitiatedByUser.DisplayName,
            ResolvedByName = resolvedUser?.DisplayName,
            ResolvedNotes = annotation.ResolvedNotes,
            CreatedAt = annotation.CreatedAt,
            LinkedEntityType = annotation.LinkedEntityType,
            LinkedEntityId = annotation.LinkedEntityId,
        };

        await ResolveLinkedEntityNames(new List<AdminAnnotationDto> { result }, ct);

        return result;
    }

    private async Task ResolveLinkedEntityNames(
        List<AdminAnnotationDto> items,
        CancellationToken ct)
    {
        var linked = items.Where(i => i.LinkedEntityId.HasValue && !string.IsNullOrEmpty(i.LinkedEntityType)).ToList();
        if (linked.Count == 0) return;

        var plantIds = linked.Where(i => i.LinkedEntityType == "Plant").Select(i => i.LinkedEntityId!.Value).Distinct().ToList();
        var lineIds = linked.Where(i => i.LinkedEntityType == "ProductionLine").Select(i => i.LinkedEntityId!.Value).Distinct().ToList();
        var wcIds = linked.Where(i => i.LinkedEntityType == "WorkCenter").Select(i => i.LinkedEntityId!.Value).Distinct().ToList();

        var plantNames = plantIds.Count > 0
            ? await _db.Plants.Where(p => plantIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();

        var lineNames = lineIds.Count > 0
            ? await _db.ProductionLines.Where(p => lineIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();

        var wcNames = wcIds.Count > 0
            ? await _db.WorkCenters.Where(w => wcIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name, ct)
            : new Dictionary<Guid, string>();

        foreach (var item in linked)
        {
            var eid = item.LinkedEntityId!.Value;
            item.LinkedEntityName = item.LinkedEntityType switch
            {
                "Plant" => plantNames.GetValueOrDefault(eid),
                "ProductionLine" => lineNames.GetValueOrDefault(eid),
                "WorkCenter" => wcNames.GetValueOrDefault(eid),
                _ => null
            };
        }
    }
}
