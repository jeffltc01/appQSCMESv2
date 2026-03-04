using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/production-records")]
public class ProductionRecordsController : ControllerBase
{
    private readonly IProductionRecordService _productionRecordService;

    public ProductionRecordsController(IProductionRecordService productionRecordService)
    {
        _productionRecordService = productionRecordService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateProductionRecordResponseDto>> Create([FromBody] CreateProductionRecordDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productionRecordService.CreateAsync(dto, cancellationToken);
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
