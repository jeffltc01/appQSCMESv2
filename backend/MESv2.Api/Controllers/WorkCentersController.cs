using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/workcenters")]
public class WorkCentersController : ControllerBase
{
    private readonly IWorkCenterService _workCenterService;
    private readonly IXrayQueueService _xrayQueueService;
    private readonly IDowntimeService _downtimeService;
    private readonly MesDbContext _db;
    private readonly ILogger<WorkCentersController> _logger;

    public WorkCentersController(IWorkCenterService workCenterService, IXrayQueueService xrayQueueService, IDowntimeService downtimeService, MesDbContext db, ILogger<WorkCentersController> logger)
    {
        _workCenterService = workCenterService;
        _xrayQueueService = xrayQueueService;
        _downtimeService = downtimeService;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkCenterDto>>> GetWorkCenters(CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetWorkCentersAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/welders")]
    public async Task<ActionResult<IEnumerable<WelderDto>>> GetWelders(Guid id, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetWeldersAsync(id, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/welders/lookup")]
    public async Task<ActionResult<WelderDto>> LookupWelder(Guid id, [FromQuery] string empNo, CancellationToken cancellationToken)
    {
        var welder = await _workCenterService.LookupWelderAsync(empNo, cancellationToken);
        if (welder == null) return NotFound();
        return Ok(welder);
    }

    [HttpPost("{id:guid}/welders")]
    public async Task<ActionResult<WelderDto>> AddWelder(Guid id, [FromBody] AddWelderDto dto, CancellationToken cancellationToken)
    {
        var welder = await _workCenterService.AddWelderAsync(id, dto.EmployeeNumber, cancellationToken);
        if (welder == null)
            return NotFound();
        return Ok(welder);
    }

    [HttpDelete("{id:guid}/welders/{userId:guid}")]
    public async Task<ActionResult> RemoveWelder(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var removed = await _workCenterService.RemoveWelderAsync(id, userId, cancellationToken);
        if (!removed)
            return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<WCHistoryDto>> GetHistory(Guid id, [FromQuery] Guid plantId, [FromQuery] string? date = null, [FromQuery] int limit = 5, [FromQuery] Guid? assetId = null, CancellationToken cancellationToken = default)
    {
        var result = await _workCenterService.GetHistoryAsync(id, plantId, date, limit, assetId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/queue-transactions")]
    public async Task<ActionResult<IEnumerable<QueueTransactionDto>>> GetQueueTransactions(Guid id, [FromQuery] int limit = 5, [FromQuery] Guid? plantId = null, CancellationToken cancellationToken = default)
    {
        var result = await _workCenterService.GetQueueTransactionsAsync(id, limit, plantId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/material-queue")]
    public async Task<ActionResult<IEnumerable<MaterialQueueItemDto>>> GetMaterialQueue(Guid id, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetMaterialQueueAsync(id, type, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/queue/advance")]
    public async Task<ActionResult<QueueAdvanceResponseDto>> AdvanceQueue(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workCenterService.AdvanceQueueAsync(id, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost("{id:guid}/faults")]
    public async Task<ActionResult> ReportFault(Guid id, [FromBody] FaultReportDto dto, CancellationToken cancellationToken)
    {
        await _workCenterService.ReportFaultAsync(id, dto.Description, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/defect-codes")]
    public async Task<ActionResult<IEnumerable<DefectCodeDto>>> GetDefectCodes(Guid id, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetDefectCodesAsync(id, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/defect-locations")]
    public async Task<ActionResult<IEnumerable<DefectLocationDto>>> GetDefectLocations(Guid id, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetDefectLocationsAsync(id, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/characteristics")]
    public async Task<ActionResult<IEnumerable<CharacteristicDto>>> GetCharacteristics(Guid id, [FromQuery] int? tankSize, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetCharacteristicsAsync(id, tankSize, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/material-queue")]
    public async Task<ActionResult<MaterialQueueItemDto>> AddMaterialQueueItem(Guid id, [FromBody] CreateMaterialQueueItemDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workCenterService.AddMaterialQueueItemAsync(id, dto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add material queue item for WorkCenter {WorkCenterId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/material-queue/{itemId:guid}")]
    public async Task<ActionResult<MaterialQueueItemDto>> UpdateMaterialQueueItem(Guid id, Guid itemId, [FromBody] UpdateMaterialQueueItemDto dto, CancellationToken cancellationToken)
    {
        var result = await _workCenterService.UpdateMaterialQueueItemAsync(id, itemId, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:guid}/material-queue/{itemId:guid}")]
    public async Task<ActionResult> DeleteMaterialQueueItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var deleted = await _workCenterService.DeleteMaterialQueueItemAsync(id, itemId, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/fitup-queue")]
    public async Task<ActionResult<MaterialQueueItemDto>> AddFitupQueueItem(Guid id, [FromBody] CreateFitupQueueItemDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workCenterService.AddFitupQueueItemAsync(id, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/fitup-queue/{itemId:guid}")]
    public async Task<ActionResult<MaterialQueueItemDto>> UpdateFitupQueueItem(Guid id, Guid itemId, [FromBody] UpdateFitupQueueItemDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _workCenterService.UpdateFitupQueueItemAsync(id, itemId, dto, cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/fitup-queue/{itemId:guid}")]
    public async Task<ActionResult> DeleteFitupQueueItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var deleted = await _workCenterService.DeleteFitupQueueItemAsync(id, itemId, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/xray-queue")]
    public async Task<ActionResult<IEnumerable<XrayQueueItemDto>>> GetXrayQueue(Guid id, CancellationToken cancellationToken)
    {
        var list = await _xrayQueueService.GetQueueAsync(id, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/xray-queue")]
    public async Task<ActionResult<XrayQueueItemDto>> AddXrayQueueItem(Guid id, [FromBody] AddXrayQueueItemDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _xrayQueueService.AddAsync(id, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/xray-queue/{itemId:guid}")]
    public async Task<ActionResult> RemoveXrayQueueItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        var removed = await _xrayQueueService.RemoveAsync(id, itemId, cancellationToken);
        if (!removed) return NotFound();
        return NoContent();
    }

    [HttpGet("{id:guid}/barcode-cards")]
    public async Task<ActionResult<IEnumerable<BarcodeCardDto>>> GetBarcodeCards(Guid id, [FromQuery] Guid? plantId, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetBarcodeCardsAsync(plantId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminWorkCenterDto>>> GetAllAdmin(CancellationToken cancellationToken)
    {
        var list = await _db.WorkCenters
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
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("admin/grouped")]
    public async Task<ActionResult<IEnumerable<AdminWorkCenterGroupDto>>> GetAllGrouped(CancellationToken cancellationToken)
    {
        var wcs = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .Include(w => w.MaterialQueueForWC)
            .Include(w => w.WorkCenterProductionLines)
                .ThenInclude(wpl => wpl.ProductionLine)
                .ThenInclude(pl => pl.Plant)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        var groups = wcs
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

        return Ok(groups);
    }

    [HttpPut("admin/group/{groupId:guid}")]
    public async Task<ActionResult<AdminWorkCenterGroupDto>> UpdateGroup(Guid groupId, [FromBody] UpdateWorkCenterGroupDto dto, CancellationToken cancellationToken)
    {
        var wc = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .FirstOrDefaultAsync(w => w.Id == groupId, cancellationToken);

        if (wc == null) return NotFound();

        wc.Name = dto.BaseName;
        wc.DataEntryType = dto.DataEntryType;
        wc.MaterialQueueForWCId = dto.MaterialQueueForWCId;

        await _db.SaveChangesAsync(cancellationToken);

        string? mqName = null;
        if (wc.MaterialQueueForWCId.HasValue)
        {
            mqName = (await _db.WorkCenters.FindAsync(new object[] { wc.MaterialQueueForWCId.Value }, cancellationToken))?.Name;
        }

        return Ok(new AdminWorkCenterGroupDto
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
        });
    }

    [HttpPut("{id:guid}/config")]
    public async Task<ActionResult<AdminWorkCenterDto>> UpdateConfig(Guid id, [FromBody] UpdateWorkCenterConfigDto dto, CancellationToken cancellationToken)
    {
        var wc = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (wc == null) return NotFound();

        wc.NumberOfWelders = dto.NumberOfWelders;
        wc.DataEntryType = dto.DataEntryType;
        wc.MaterialQueueForWCId = dto.MaterialQueueForWCId;
        await _db.SaveChangesAsync(cancellationToken);

        string? mqName = null;
        if (wc.MaterialQueueForWCId.HasValue)
        {
            mqName = (await _db.WorkCenters.FindAsync(new object[] { wc.MaterialQueueForWCId.Value }, cancellationToken))?.Name;
        }

        return Ok(new AdminWorkCenterDto
        {
            Id = wc.Id, Name = wc.Name,
            WorkCenterTypeName = wc.WorkCenterType.Name,
            NumberOfWelders = wc.NumberOfWelders,
            DataEntryType = wc.DataEntryType,
            MaterialQueueForWCId = wc.MaterialQueueForWCId,
            MaterialQueueForWCName = mqName
        });
    }

    // ---- Work Center Production Line endpoints ----

    [HttpGet("{wcId:guid}/production-lines")]
    public async Task<ActionResult<IEnumerable<AdminWorkCenterProductionLineDto>>> GetProductionLineConfigs(Guid wcId, CancellationToken cancellationToken)
    {
        var list = await _db.WorkCenterProductionLines
            .Include(wcpl => wcpl.ProductionLine).ThenInclude(pl => pl.Plant)
            .Where(wcpl => wcpl.WorkCenterId == wcId)
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
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{wcId:guid}/production-lines/{plId:guid}")]
    public async Task<ActionResult<WorkCenterProductionLineDto>> GetProductionLineConfig(Guid wcId, Guid plId, CancellationToken cancellationToken)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .Include(x => x.ProductionLine)
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == plId, cancellationToken);

        if (wcpl == null) return NotFound();

        return Ok(new WorkCenterProductionLineDto
        {
            Id = wcpl.Id,
            WorkCenterId = wcpl.WorkCenterId,
            ProductionLineId = wcpl.ProductionLineId,
            ProductionLineName = wcpl.ProductionLine.Name,
            DisplayName = wcpl.DisplayName,
            NumberOfWelders = wcpl.NumberOfWelders,
        });
    }

    [HttpPost("{wcId:guid}/production-lines")]
    public async Task<ActionResult<AdminWorkCenterProductionLineDto>> CreateProductionLineConfig(Guid wcId, [FromBody] CreateWorkCenterProductionLineDto dto, CancellationToken cancellationToken)
    {
        var wc = await _db.WorkCenters.FindAsync(new object[] { wcId }, cancellationToken);
        if (wc == null) return NotFound();

        var pl = await _db.ProductionLines.Include(p => p.Plant).FirstOrDefaultAsync(p => p.Id == dto.ProductionLineId, cancellationToken);
        if (pl == null) return BadRequest(new { message = "Production line not found." });

        var exists = await _db.WorkCenterProductionLines
            .AnyAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == dto.ProductionLineId, cancellationToken);
        if (exists) return Conflict(new { message = "Configuration already exists for this work center and production line." });

        var entity = new MESv2.Api.Models.WorkCenterProductionLine
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = dto.ProductionLineId,
            DisplayName = dto.DisplayName,
            NumberOfWelders = dto.NumberOfWelders,
        };

        _db.WorkCenterProductionLines.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminWorkCenterProductionLineDto
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
        });
    }

    [HttpPut("{wcId:guid}/production-lines/{plId:guid}")]
    public async Task<ActionResult<AdminWorkCenterProductionLineDto>> UpdateProductionLineConfig(Guid wcId, Guid plId, [FromBody] UpdateWorkCenterProductionLineDto dto, CancellationToken cancellationToken)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .Include(x => x.ProductionLine).ThenInclude(pl => pl.Plant)
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == plId, cancellationToken);

        if (wcpl == null) return NotFound();

        wcpl.DisplayName = dto.DisplayName;
        wcpl.NumberOfWelders = dto.NumberOfWelders;
        wcpl.DowntimeTrackingEnabled = dto.DowntimeTrackingEnabled;
        wcpl.DowntimeThresholdMinutes = dto.DowntimeThresholdMinutes;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new AdminWorkCenterProductionLineDto
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
        });
    }

    [HttpDelete("{wcId:guid}/production-lines/{plId:guid}")]
    public async Task<ActionResult> DeleteProductionLineConfig(Guid wcId, Guid plId, CancellationToken cancellationToken)
    {
        var wcpl = await _db.WorkCenterProductionLines
            .FirstOrDefaultAsync(x => x.WorkCenterId == wcId && x.ProductionLineId == plId, cancellationToken);

        if (wcpl == null) return NotFound();

        _db.WorkCenterProductionLines.Remove(wcpl);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ---- Downtime Config ----

    [HttpGet("{wcId:guid}/production-lines/{plId:guid}/downtime-config")]
    public async Task<ActionResult<DowntimeConfigDto>> GetDowntimeConfig(Guid wcId, Guid plId, CancellationToken cancellationToken)
    {
        var config = await _downtimeService.GetDowntimeConfigAsync(wcId, plId, cancellationToken);
        if (config == null) return NotFound();
        return Ok(config);
    }

    [HttpPut("{wcId:guid}/production-lines/{plId:guid}/downtime-config")]
    public async Task<ActionResult<DowntimeConfigDto>> UpdateDowntimeConfig(Guid wcId, Guid plId, [FromBody] UpdateDowntimeConfigDto dto, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var config = await _downtimeService.UpdateDowntimeConfigAsync(wcId, plId, dto, cancellationToken);
        if (config == null) return NotFound();
        return Ok(config);
    }

    [HttpPut("{wcId:guid}/production-lines/{plId:guid}/downtime-reasons")]
    public async Task<ActionResult> SetDowntimeReasons(Guid wcId, Guid plId, [FromBody] SetDowntimeReasonsDto dto, CancellationToken cancellationToken)
    {
        if (!IsQualityManagerOrAbove()) return StatusCode(403, new { message = "Quality Manager or above required." });

        var success = await _downtimeService.SetDowntimeReasonsAsync(wcId, plId, dto.ReasonIds, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }

    private bool IsQualityManagerOrAbove()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 3m;
        return false;
    }
}
