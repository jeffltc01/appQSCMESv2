using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class AdminWorkCenterService : IAdminWorkCenterService
{
    private readonly MesDbContext _db;

    public AdminWorkCenterService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminWorkCenterDto>> GetAllAdminAsync(CancellationToken ct = default)
    {
        return await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .OrderBy(w => w.Name)
            .Select(w => new AdminWorkCenterDto
            {
                Id = w.Id,
                Name = w.Name,
                WorkCenterTypeName = w.WorkCenterType.Name,
                NumberOfWelders = w.NumberOfWelders,
                DataEntryType = w.DataEntryType,
                MaterialQueueForWCId = w.MaterialQueueForWCId,
                MaterialQueueForWCName = w.MaterialQueueForWC != null ? w.MaterialQueueForWC.Name : null
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminWorkCenterGroupDto>> GetAllGroupedAsync(CancellationToken ct = default)
    {
        var wcs = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .Include(w => w.MaterialQueueForWC)
            .Include(w => w.WorkCenterProductionLines)
                .ThenInclude(wpl => wpl.ProductionLine)
                .ThenInclude(pl => pl.Plant)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

        return wcs
            .Select(w =>
            {
                var plants = w.WorkCenterProductionLines
                    .Select(wpl => wpl.ProductionLine.Plant)
                    .DistinctBy(p => p.Id)
                    .ToList();

                var siteConfigs = plants.Count > 0
                    ? plants.Select(plant => new WorkCenterSiteConfigDto
                    {
                        WorkCenterId = w.Id,
                        SiteName = plant.Name,
                        NumberOfWelders = w.NumberOfWelders,
                        MaterialQueueForWCId = w.MaterialQueueForWCId,
                        MaterialQueueForWCName = w.MaterialQueueForWC?.Name,
                    }).ToList()
                    : new List<WorkCenterSiteConfigDto>
                    {
                        new()
                        {
                            WorkCenterId = w.Id,
                            SiteName = w.Name,
                            NumberOfWelders = w.NumberOfWelders,
                            MaterialQueueForWCId = w.MaterialQueueForWCId,
                            MaterialQueueForWCName = w.MaterialQueueForWC?.Name,
                        }
                    };

                return new AdminWorkCenterGroupDto
                {
                    GroupId = w.Id,
                    BaseName = w.Name,
                    WorkCenterTypeName = w.WorkCenterType.Name,
                    DataEntryType = w.DataEntryType,
                    SiteConfigs = siteConfigs
                };
            })
            .OrderBy(g => g.BaseName)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkCenterTypeDto>> GetWorkCenterTypesAsync(CancellationToken ct = default)
    {
        return await _db.WorkCenterTypes
            .OrderBy(t => t.Name)
            .Select(t => new WorkCenterTypeDto { Id = t.Id, Name = t.Name })
            .ToListAsync(ct);
    }

    public async Task<AdminWorkCenterGroupDto> CreateWorkCenterAsync(CreateWorkCenterDto dto, CancellationToken ct = default)
    {
        var wcType = await _db.WorkCenterTypes.FindAsync(new object[] { dto.WorkCenterTypeId }, ct);
        if (wcType == null)
            throw new ArgumentException("Work center type not found.");

        var duplicate = await _db.WorkCenters.AnyAsync(w => w.Name == dto.Name, ct);
        if (duplicate)
            throw new InvalidOperationException("A work center with this name already exists.");

        var wc = new Models.WorkCenter
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            WorkCenterTypeId = dto.WorkCenterTypeId,
            DataEntryType = dto.DataEntryType,
            MaterialQueueForWCId = dto.MaterialQueueForWCId,
        };

        _db.WorkCenters.Add(wc);
        await _db.SaveChangesAsync(ct);

        string? mqName = null;
        if (wc.MaterialQueueForWCId.HasValue)
            mqName = (await _db.WorkCenters.FindAsync(new object[] { wc.MaterialQueueForWCId.Value }, ct))?.Name;

        return new AdminWorkCenterGroupDto
        {
            GroupId = wc.Id,
            BaseName = wc.Name,
            WorkCenterTypeName = wcType.Name,
            DataEntryType = wc.DataEntryType,
            SiteConfigs = new List<WorkCenterSiteConfigDto>
            {
                new()
                {
                    WorkCenterId = wc.Id,
                    SiteName = wc.Name,
                    NumberOfWelders = 0,
                    MaterialQueueForWCId = wc.MaterialQueueForWCId,
                    MaterialQueueForWCName = mqName,
                }
            }
        };
    }

    public async Task<AdminWorkCenterGroupDto?> UpdateGroupAsync(Guid groupId, UpdateWorkCenterGroupDto dto, CancellationToken ct = default)
    {
        var wc = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .FirstOrDefaultAsync(w => w.Id == groupId, ct);

        if (wc == null) return null;

        wc.Name = dto.BaseName;
        wc.DataEntryType = dto.DataEntryType;
        wc.MaterialQueueForWCId = dto.MaterialQueueForWCId;

        await _db.SaveChangesAsync(ct);

        string? mqName = null;
        if (wc.MaterialQueueForWCId.HasValue)
            mqName = (await _db.WorkCenters.FindAsync(new object[] { wc.MaterialQueueForWCId.Value }, ct))?.Name;

        return new AdminWorkCenterGroupDto
        {
            GroupId = groupId,
            BaseName = wc.Name,
            WorkCenterTypeName = wc.WorkCenterType.Name,
            DataEntryType = wc.DataEntryType,
            SiteConfigs = new List<WorkCenterSiteConfigDto>
            {
                new()
                {
                    WorkCenterId = wc.Id,
                    SiteName = wc.Name,
                    NumberOfWelders = wc.NumberOfWelders,
                    MaterialQueueForWCId = wc.MaterialQueueForWCId,
                    MaterialQueueForWCName = mqName,
                }
            }
        };
    }

    public async Task<AdminWorkCenterDto?> UpdateConfigAsync(Guid id, UpdateWorkCenterConfigDto dto, CancellationToken ct = default)
    {
        var wc = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        if (wc == null) return null;

        wc.NumberOfWelders = dto.NumberOfWelders;
        wc.DataEntryType = dto.DataEntryType;
        wc.MaterialQueueForWCId = dto.MaterialQueueForWCId;
        await _db.SaveChangesAsync(ct);

        string? mqName = null;
        if (wc.MaterialQueueForWCId.HasValue)
            mqName = (await _db.WorkCenters.FindAsync(new object[] { wc.MaterialQueueForWCId.Value }, ct))?.Name;

        return new AdminWorkCenterDto
        {
            Id = wc.Id,
            Name = wc.Name,
            WorkCenterTypeName = wc.WorkCenterType.Name,
            NumberOfWelders = wc.NumberOfWelders,
            DataEntryType = wc.DataEntryType,
            MaterialQueueForWCId = wc.MaterialQueueForWCId,
            MaterialQueueForWCName = mqName
        };
    }

    public async Task<IReadOnlyList<AdminWorkCenterProductionLineDto>> GetProductionLineConfigsAsync(Guid wcId, Guid? plantId = null, CancellationToken ct = default)
    {
        return await _db.WorkCenterProductionLines
            .Include(wcpl => wcpl.ProductionLine).ThenInclude(pl => pl.Plant)
            .Where(wcpl => wcpl.WorkCenterId == wcId && (!plantId.HasValue || wcpl.ProductionLine.PlantId == plantId.Value))
            .OrderBy(wcpl => wcpl.ProductionLine.Name)
            .Select(wcpl => new AdminWorkCenterProductionLineDto
            {
                Id = wcpl.Id,
                WorkCenterId = wcpl.WorkCenterId,
                ProductionLineId = wcpl.ProductionLineId,
                ProductionLineName = wcpl.ProductionLine.Name,
                PlantName = wcpl.ProductionLine.Plant.Name,
                DisplayName = wcpl.DisplayName,
                NumberOfWelders = wcpl.NumberOfWelders,
                DowntimeTrackingEnabled = wcpl.DowntimeTrackingEnabled,
                DowntimeThresholdMinutes = wcpl.DowntimeThresholdMinutes,
                EnableWorkCenterChecklist = wcpl.EnableWorkCenterChecklist,
                EnableSafetyChecklist = wcpl.EnableSafetyChecklist,
            })
            .ToListAsync(ct);
    }

    public async Task<WorkCenterProductionLineDto?> GetProductionLineConfigAsync(Guid wcId, Guid plId, CancellationToken ct = default)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .Include(x => x.ProductionLine)
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == plId, ct);

        if (wcpl == null) return null;

        return new WorkCenterProductionLineDto
        {
            Id = wcpl.Id,
            WorkCenterId = wcpl.WorkCenterId,
            ProductionLineId = wcpl.ProductionLineId,
            ProductionLineName = wcpl.ProductionLine.Name,
            DisplayName = wcpl.DisplayName,
            NumberOfWelders = wcpl.NumberOfWelders,
            EnableWorkCenterChecklist = wcpl.EnableWorkCenterChecklist,
            EnableSafetyChecklist = wcpl.EnableSafetyChecklist,
        };
    }

    public async Task<AdminWorkCenterProductionLineDto> CreateProductionLineConfigAsync(Guid wcId, CreateWorkCenterProductionLineDto dto, CancellationToken ct = default)
    {
        var wc = await _db.WorkCenters.FindAsync(new object[] { wcId }, ct);
        if (wc == null)
            throw new KeyNotFoundException("Work center not found.");

        var pl = await _db.ProductionLines.Include(p => p.Plant).FirstOrDefaultAsync(p => p.Id == dto.ProductionLineId, ct);
        if (pl == null)
            throw new ArgumentException("Production line not found.");

        var exists = await _db.WorkCenterProductionLines
            .AnyAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == dto.ProductionLineId, ct);
        if (exists)
            throw new InvalidOperationException("Configuration already exists for this work center and production line.");

        var entity = new Models.WorkCenterProductionLine
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = dto.ProductionLineId,
            DisplayName = dto.DisplayName,
            NumberOfWelders = dto.NumberOfWelders,
            EnableWorkCenterChecklist = dto.EnableWorkCenterChecklist,
            EnableSafetyChecklist = dto.EnableSafetyChecklist,
        };

        _db.WorkCenterProductionLines.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new AdminWorkCenterProductionLineDto
        {
            Id = entity.Id,
            WorkCenterId = entity.WorkCenterId,
            ProductionLineId = entity.ProductionLineId,
            ProductionLineName = pl.Name,
            PlantName = pl.Plant.Name,
            DisplayName = entity.DisplayName,
            NumberOfWelders = entity.NumberOfWelders,
            DowntimeTrackingEnabled = entity.DowntimeTrackingEnabled,
            DowntimeThresholdMinutes = entity.DowntimeThresholdMinutes,
            EnableWorkCenterChecklist = entity.EnableWorkCenterChecklist,
            EnableSafetyChecklist = entity.EnableSafetyChecklist,
        };
    }

    public async Task<AdminWorkCenterProductionLineDto?> UpdateProductionLineConfigAsync(Guid wcId, Guid plId, UpdateWorkCenterProductionLineDto dto, CancellationToken ct = default)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .Include(x => x.ProductionLine).ThenInclude(pl => pl.Plant)
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == plId, ct);

        if (wcpl == null) return null;

        wcpl.DisplayName = dto.DisplayName;
        wcpl.NumberOfWelders = dto.NumberOfWelders;
        wcpl.DowntimeTrackingEnabled = dto.DowntimeTrackingEnabled;
        wcpl.DowntimeThresholdMinutes = dto.DowntimeThresholdMinutes;
        wcpl.EnableWorkCenterChecklist = dto.EnableWorkCenterChecklist;
        wcpl.EnableSafetyChecklist = dto.EnableSafetyChecklist;
        await _db.SaveChangesAsync(ct);

        return new AdminWorkCenterProductionLineDto
        {
            Id = wcpl.Id,
            WorkCenterId = wcpl.WorkCenterId,
            ProductionLineId = wcpl.ProductionLineId,
            ProductionLineName = wcpl.ProductionLine.Name,
            PlantName = wcpl.ProductionLine.Plant.Name,
            DisplayName = wcpl.DisplayName,
            NumberOfWelders = wcpl.NumberOfWelders,
            DowntimeTrackingEnabled = wcpl.DowntimeTrackingEnabled,
            DowntimeThresholdMinutes = wcpl.DowntimeThresholdMinutes,
            EnableWorkCenterChecklist = wcpl.EnableWorkCenterChecklist,
            EnableSafetyChecklist = wcpl.EnableSafetyChecklist,
        };
    }

    public async Task<bool> DeleteProductionLineConfigAsync(Guid wcId, Guid plId, CancellationToken ct = default)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == plId, ct);

        if (wcpl == null) return false;

        _db.WorkCenterProductionLines.Remove(wcpl);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
