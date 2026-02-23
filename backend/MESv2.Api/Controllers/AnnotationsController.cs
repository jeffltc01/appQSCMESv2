using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/annotations")]
public class AnnotationsController : ControllerBase
{
    private readonly IAnnotationService _annotationService;

    public AnnotationsController(IAnnotationService annotationService)
    {
        _annotationService = annotationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminAnnotationDto>>> GetAll(
        [FromQuery] Guid? siteId, CancellationToken cancellationToken)
    {
        return Ok(await _annotationService.GetAllAsync(siteId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminAnnotationDto>> Create(
        [FromBody] CreateAnnotationDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _annotationService.CreateAsync(dto, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAnnotationDto>> Update(
        Guid id, [FromBody] UpdateAnnotationDto dto, CancellationToken cancellationToken)
    {
        var result = await _annotationService.UpdateAsync(id, dto, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }
}
