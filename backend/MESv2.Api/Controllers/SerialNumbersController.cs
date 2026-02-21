using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/serial-numbers")]
public class SerialNumbersController : ControllerBase
{
    private readonly ISerialNumberService _serialNumberService;

    public SerialNumbersController(ISerialNumberService serialNumberService)
    {
        _serialNumberService = serialNumberService;
    }

    [HttpGet("{serial}/context")]
    public async Task<ActionResult<SerialNumberContextDto>> GetContext(string serial, CancellationToken cancellationToken)
    {
        var result = await _serialNumberService.GetContextAsync(serial, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("{serial}/lookup")]
    public async Task<ActionResult<SerialNumberLookupDto>> GetLookup(string serial, CancellationToken cancellationToken)
    {
        var result = await _serialNumberService.GetLookupAsync(serial, cancellationToken);
        if (result == null)
            return NotFound(new { message = "Serial number not found" });
        return Ok(result);
    }
}
