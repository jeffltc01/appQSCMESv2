using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/inspection-records")]
public class InspectionRecordsController : ControllerBase
{
    private readonly IInspectionRecordService _inspectionRecordService;

    public InspectionRecordsController(IInspectionRecordService inspectionRecordService)
    {
        _inspectionRecordService = inspectionRecordService;
    }

    [HttpPost]
    public async Task<ActionResult<InspectionRecordResponseDto>> Create([FromBody] CreateInspectionRecordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inspectionRecordService.CreateAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (SerialProcessingBlockedException ex)
        {
            return Conflict(new
            {
                message = "Serial is blocked for processing due to open Hold Tag/NCR.",
                serialNumber = ex.SerialNumber,
                openHoldTagNumbers = ex.BlockResult.OpenHoldTagNumbers,
                openNcrNumbers = ex.BlockResult.OpenNcrNumbers,
                reasons = ex.BlockResult.Reasons
            });
        }
    }
}
