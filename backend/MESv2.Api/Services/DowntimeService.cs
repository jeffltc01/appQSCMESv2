using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class DowntimeService : IDowntimeService
{
    private readonly MesDbContext _db;

    public DowntimeService(MesDbContext db)
    {
        _db = db;
    }

    // ---- Reason Categories ----

    public async Task<IReadOnlyList<DowntimeReasonCategoryDto>> GetCategoriesAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        return await _db.DowntimeReasonCategories
            .Where(c => c.PlantId == plantId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new DowntimeReasonCategoryDto
            {
                Id = c.Id,
                PlantId = c.PlantId,
                Name = c.Name,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                Reasons = c.DowntimeReasons
                    .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
                    .Select(r => new DowntimeReasonDto
                    {
                        Id = r.Id,
                        DowntimeReasonCategoryId = r.DowntimeReasonCategoryId,
                        CategoryName = c.Name,
                        Name = r.Name,
                        IsActive = r.IsActive,
                        CountsAsDowntime = r.CountsAsDowntime,
                        SortOrder = r.SortOrder
                    }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DowntimeReasonCategoryDto> CreateCategoryAsync(CreateDowntimeReasonCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new DowntimeReasonCategory
        {
            Id = Guid.NewGuid(),
            PlantId = dto.PlantId,
            Name = dto.Name,
            SortOrder = dto.SortOrder,
            IsActive = true
        };
        _db.DowntimeReasonCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new DowntimeReasonCategoryDto
        {
            Id = entity.Id,
            PlantId = entity.PlantId,
            Name = entity.Name,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    public async Task<DowntimeReasonCategoryDto?> UpdateCategoryAsync(Guid id, UpdateDowntimeReasonCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DowntimeReasonCategories.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return null;

        entity.Name = dto.Name;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        return new DowntimeReasonCategoryDto
        {
            Id = entity.Id,
            PlantId = entity.PlantId,
            Name = entity.Name,
            IsActive = entity.IsActive,
            SortOrder = entity.SortOrder
        };
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DowntimeReasonCategories.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ---- Reasons ----

    public async Task<IReadOnlyList<DowntimeReasonDto>> GetReasonsAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        return await _db.DowntimeReasons
            .Where(r => r.DowntimeReasonCategory.PlantId == plantId)
            .OrderBy(r => r.DowntimeReasonCategory.SortOrder)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .Select(r => new DowntimeReasonDto
            {
                Id = r.Id,
                DowntimeReasonCategoryId = r.DowntimeReasonCategoryId,
                CategoryName = r.DowntimeReasonCategory.Name,
                Name = r.Name,
                IsActive = r.IsActive,
                CountsAsDowntime = r.CountsAsDowntime,
                SortOrder = r.SortOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DowntimeReasonDto> CreateReasonAsync(CreateDowntimeReasonDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _db.DowntimeReasonCategories.FindAsync(new object[] { dto.DowntimeReasonCategoryId }, cancellationToken);

        var entity = new DowntimeReason
        {
            Id = Guid.NewGuid(),
            DowntimeReasonCategoryId = dto.DowntimeReasonCategoryId,
            Name = dto.Name,
            CountsAsDowntime = dto.CountsAsDowntime,
            SortOrder = dto.SortOrder,
            IsActive = true
        };
        _db.DowntimeReasons.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new DowntimeReasonDto
        {
            Id = entity.Id,
            DowntimeReasonCategoryId = entity.DowntimeReasonCategoryId,
            CategoryName = category?.Name ?? "",
            Name = entity.Name,
            IsActive = entity.IsActive,
            CountsAsDowntime = entity.CountsAsDowntime,
            SortOrder = entity.SortOrder
        };
    }

    public async Task<DowntimeReasonDto?> UpdateReasonAsync(Guid id, UpdateDowntimeReasonDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DowntimeReasons
            .Include(r => r.DowntimeReasonCategory)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity == null) return null;

        entity.Name = dto.Name;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
        entity.CountsAsDowntime = dto.CountsAsDowntime;

        await _db.SaveChangesAsync(cancellationToken);

        return new DowntimeReasonDto
        {
            Id = entity.Id,
            DowntimeReasonCategoryId = entity.DowntimeReasonCategoryId,
            CategoryName = entity.DowntimeReasonCategory.Name,
            Name = entity.Name,
            IsActive = entity.IsActive,
            CountsAsDowntime = entity.CountsAsDowntime,
            SortOrder = entity.SortOrder
        };
    }

    public async Task<bool> DeleteReasonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.DowntimeReasons.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ---- Downtime Config ----

    public async Task<DowntimeConfigDto?> GetDowntimeConfigAsync(Guid wcId, Guid productionLineId, CancellationToken cancellationToken = default)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .Include(x => x.WorkCenterProductionLineDowntimeReasons)
                .ThenInclude(x => x.DowntimeReason)
                    .ThenInclude(r => r.DowntimeReasonCategory)
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == productionLineId, cancellationToken);

        if (wcpl == null) return null;

        return new DowntimeConfigDto
        {
            DowntimeTrackingEnabled = wcpl.DowntimeTrackingEnabled,
            DowntimeThresholdMinutes = wcpl.DowntimeThresholdMinutes,
            ApplicableReasons = wcpl.WorkCenterProductionLineDowntimeReasons
                .Where(x => x.DowntimeReason.IsActive)
                .OrderBy(x => x.DowntimeReason.DowntimeReasonCategory.SortOrder)
                .ThenBy(x => x.DowntimeReason.SortOrder)
                .Select(x => new DowntimeReasonDto
                {
                    Id = x.DowntimeReason.Id,
                    DowntimeReasonCategoryId = x.DowntimeReason.DowntimeReasonCategoryId,
                    CategoryName = x.DowntimeReason.DowntimeReasonCategory.Name,
                    Name = x.DowntimeReason.Name,
                    IsActive = x.DowntimeReason.IsActive,
                    CountsAsDowntime = x.DowntimeReason.CountsAsDowntime,
                    SortOrder = x.DowntimeReason.SortOrder
                })
                .ToList()
        };
    }

    public async Task<DowntimeConfigDto?> UpdateDowntimeConfigAsync(Guid wcId, Guid productionLineId, UpdateDowntimeConfigDto dto, CancellationToken cancellationToken = default)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == productionLineId, cancellationToken);

        if (wcpl == null) return null;

        wcpl.DowntimeTrackingEnabled = dto.DowntimeTrackingEnabled;
        wcpl.DowntimeThresholdMinutes = dto.DowntimeThresholdMinutes;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetDowntimeConfigAsync(wcId, productionLineId, cancellationToken);
    }

    public async Task<bool> SetDowntimeReasonsAsync(Guid wcId, Guid productionLineId, List<Guid> reasonIds, CancellationToken cancellationToken = default)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .Include(x => x.WorkCenterProductionLineDowntimeReasons)
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == productionLineId, cancellationToken);

        if (wcpl == null) return false;

        _db.WorkCenterProductionLineDowntimeReasons.RemoveRange(wcpl.WorkCenterProductionLineDowntimeReasons);

        foreach (var reasonId in reasonIds)
        {
            _db.WorkCenterProductionLineDowntimeReasons.Add(new WorkCenterProductionLineDowntimeReason
            {
                Id = Guid.NewGuid(),
                WorkCenterProductionLineId = wcpl.Id,
                DowntimeReasonId = reasonId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ---- Downtime Events ----

    public async Task<DowntimeEventDto> CreateDowntimeEventAsync(CreateDowntimeEventDto dto, Guid initiatedByUserId, CancellationToken cancellationToken = default)
    {
        var duration = (decimal)(dto.EndedAt - dto.StartedAt).TotalMinutes;

        var entity = new DowntimeEvent
        {
            Id = Guid.NewGuid(),
            WorkCenterProductionLineId = dto.WorkCenterProductionLineId,
            OperatorUserId = dto.OperatorUserId,
            DowntimeReasonId = dto.DowntimeReasonId,
            StartedAt = dto.StartedAt,
            EndedAt = dto.EndedAt,
            DurationMinutes = Math.Round(duration, 2),
            IsAutoGenerated = dto.IsAutoGenerated,
            CreatedAt = DateTime.UtcNow
        };

        _db.DowntimeEvents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (dto.IsAutoGenerated)
        {
            var correctionNeededType = await _db.AnnotationTypes
                .FirstOrDefaultAsync(at => at.Name == "Correction Needed", cancellationToken);

            if (correctionNeededType != null)
            {
                _db.Annotations.Add(new Annotation
                {
                    Id = Guid.NewGuid(),
                    DowntimeEventId = entity.Id,
                    AnnotationTypeId = correctionNeededType.Id,
                    Status = AnnotationStatus.Open,
                    Notes = "Auto-generated downtime event — reason unknown. Please review and assign the correct reason.",
                    SystemTypeInfo = "Auto-logout downtime — reason unknown",
                    InitiatedByUserId = initiatedByUserId,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var operatorUser = await _db.Users.FindAsync(new object[] { dto.OperatorUserId }, cancellationToken);
        DowntimeReason? reason = dto.DowntimeReasonId.HasValue
            ? await _db.DowntimeReasons
                .Include(r => r.DowntimeReasonCategory)
                .FirstOrDefaultAsync(r => r.Id == dto.DowntimeReasonId.Value, cancellationToken)
            : null;

        return new DowntimeEventDto
        {
            Id = entity.Id,
            WorkCenterProductionLineId = entity.WorkCenterProductionLineId,
            OperatorUserId = entity.OperatorUserId,
            OperatorName = operatorUser?.DisplayName ?? "",
            DowntimeReasonId = entity.DowntimeReasonId,
            DowntimeReasonName = reason?.Name,
            DowntimeReasonCategoryName = reason?.DowntimeReasonCategory.Name,
            StartedAt = entity.StartedAt,
            EndedAt = entity.EndedAt,
            DurationMinutes = entity.DurationMinutes,
            IsAutoGenerated = entity.IsAutoGenerated,
            CreatedAt = entity.CreatedAt
        };
    }
}
