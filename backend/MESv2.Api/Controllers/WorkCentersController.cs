using Microsoft.AspNetCore.Mvc;
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
    private readonly IAdminWorkCenterService _adminService;
    private readonly ILogger<WorkCentersController> _logger;

    public WorkCentersController(IWorkCenterService workCenterService, IXrayQueueService xrayQueueService, IDowntimeService downtimeService, IAdminWorkCenterService adminService, ILogger<WorkCentersController> logger)
    {
        _workCenterService = workCenterService;
        _xrayQueueService = xrayQueueService;
        _downtimeService = downtimeService;
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkCenterDto>>> GetWorkCenters(CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetWorkCentersAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/welders/lookup")]
    public async Task<ActionResult<WelderDto>> LookupWelder(Guid id, [FromQuery] string empNo, CancellationToken cancellationToken)
    {
        var welder = await _workCenterService.LookupWelderAsync(empNo, cancellationToken);
        if (welder == null) return NotFound();
        return Ok(welder);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<WCHistoryDto>> GetHistory(Guid id, [FromQuery] Guid plantId, [FromQuery] string? date = null, [FromQuery] int limit = 5, [FromQuery] Guid? assetId = null, CancellationToken cancellationToken = default)
    {
        var result = await _workCenterService.GetHistoryAsync(id, plantId, date, limit, assetId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/queue-transactions")]
    public async Task<ActionResult<IEnumerable<QueueTransactionDto>>> GetQueueTransactions(Guid id, [FromQuery] int limit = 5, [FromQuery] Guid? plantId = null, [FromQuery] string? action = null, CancellationToken cancellationToken = default)
    {
        var result = await _workCenterService.GetQueueTransactionsAsync(id, limit, plantId, action, cancellationToken);
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
        var list = await _adminService.GetAllAdminAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("admin/grouped")]
    public async Task<ActionResult<IEnumerable<AdminWorkCenterGroupDto>>> GetAllGrouped(CancellationToken cancellationToken)
    {
        var groups = await _adminService.GetAllGroupedAsync(cancellationToken);
        return Ok(groups);
    }

    [HttpGet("admin/types")]
    public async Task<ActionResult<IEnumerable<WorkCenterTypeDto>>> GetWorkCenterTypes(CancellationToken cancellationToken)
    {
        var types = await _adminService.GetWorkCenterTypesAsync(cancellationToken);
        return Ok(types);
    }

    [HttpPost("admin")]
    public async Task<ActionResult<AdminWorkCenterGroupDto>> CreateWorkCenter([FromBody] CreateWorkCenterDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        try
        {
            var created = await _adminService.CreateWorkCenterAsync(dto, cancellationToken);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("admin/group/{groupId:guid}")]
    public async Task<ActionResult<AdminWorkCenterGroupDto>> UpdateGroup(Guid groupId, [FromBody] UpdateWorkCenterGroupDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var result = await _adminService.UpdateGroupAsync(groupId, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id:guid}/config")]
    public async Task<ActionResult<AdminWorkCenterDto>> UpdateConfig(Guid id, [FromBody] UpdateWorkCenterConfigDto dto, CancellationToken cancellationToken)
    {
        if (!IsAdmin()) return StatusCode(403, new { message = "Admin role required." });

        var result = await _adminService.UpdateConfigAsync(id, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // ---- Work Center Production Line endpoints ----

    [HttpGet("{wcId:guid}/production-lines")]
    public async Task<ActionResult<IEnumerable<AdminWorkCenterProductionLineDto>>> GetProductionLineConfigs(Guid wcId, CancellationToken cancellationToken, [FromQuery] Guid? plantId = null)
    {
        var list = await _adminService.GetProductionLineConfigsAsync(wcId, plantId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{wcId:guid}/production-lines/{plId:guid}")]
    public async Task<ActionResult<WorkCenterProductionLineDto>> GetProductionLineConfig(Guid wcId, Guid plId, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetProductionLineConfigAsync(wcId, plId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{wcId:guid}/production-lines")]
    public async Task<ActionResult<AdminWorkCenterProductionLineDto>> CreateProductionLineConfig(Guid wcId, [FromBody] CreateWorkCenterProductionLineDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _adminService.CreateProductionLineConfigAsync(wcId, dto, cancellationToken);
            return Ok(created);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{wcId:guid}/production-lines/{plId:guid}")]
    public async Task<ActionResult<AdminWorkCenterProductionLineDto>> UpdateProductionLineConfig(Guid wcId, Guid plId, [FromBody] UpdateWorkCenterProductionLineDto dto, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateProductionLineConfigAsync(wcId, plId, dto, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{wcId:guid}/production-lines/{plId:guid}")]
    public async Task<ActionResult> DeleteProductionLineConfig(Guid wcId, Guid plId, CancellationToken cancellationToken)
    {
        var deleted = await _adminService.DeleteProductionLineConfigAsync(wcId, plId, cancellationToken);
        if (!deleted) return NotFound();
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

    private bool IsAdmin()
    {
        if (Request.Headers.TryGetValue("X-User-Role-Tier", out var tierHeader) &&
            decimal.TryParse(tierHeader, out var callerTier))
            return callerTier <= 1m;
        return false;
    }
}
