using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/workcenters")]
public class WorkCentersController : ControllerBase
{
    private readonly IWorkCenterService _workCenterService;

    public WorkCentersController(IWorkCenterService workCenterService)
    {
        _workCenterService = workCenterService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkCenterDto>>> GetWorkCenters([FromQuery] string siteCode, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetWorkCentersAsync(siteCode, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}/welders")]
    public async Task<ActionResult<IEnumerable<WelderDto>>> GetWelders(Guid id, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetWeldersAsync(id, cancellationToken);
        return Ok(list);
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
    public async Task<ActionResult<WCHistoryDto>> GetHistory(Guid id, [FromQuery] string date, [FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        var result = await _workCenterService.GetHistoryAsync(id, date, limit, cancellationToken);
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
    public async Task<ActionResult<IEnumerable<CharacteristicDto>>> GetCharacteristics(Guid id, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetCharacteristicsAsync(id, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/material-queue")]
    public async Task<ActionResult<MaterialQueueItemDto>> AddMaterialQueueItem(Guid id, [FromBody] CreateMaterialQueueItemDto dto, CancellationToken cancellationToken)
    {
        var result = await _workCenterService.AddMaterialQueueItemAsync(id, dto, cancellationToken);
        return Ok(result);
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

    [HttpGet("{id:guid}/barcode-cards")]
    public async Task<ActionResult<IEnumerable<BarcodeCardDto>>> GetBarcodeCards(Guid id, [FromQuery] string? siteCode, CancellationToken cancellationToken)
    {
        var list = await _workCenterService.GetBarcodeCardsAsync(siteCode, cancellationToken);
        return Ok(list);
    }
}
