using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/hold-tags")]
public class HoldTagsController : ControllerBase
{
    private readonly IHoldTagService _holdTags;
    private readonly IWorkflowEngineService _workflow;

    public HoldTagsController(IHoldTagService holdTags, IWorkflowEngineService workflow)
    {
        _holdTags = holdTags;
        _workflow = workflow;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HoldTagDto>>> GetList([FromQuery] string? siteCode, CancellationToken ct)
        => Ok(await _holdTags.GetListAsync(siteCode, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HoldTagDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _holdTags.GetByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<HoldTagDto>> Create([FromBody] CreateHoldTagRequestDto dto, CancellationToken ct)
        => Ok(await _holdTags.CreateHoldTagAsync(dto, ct));

    [HttpPost("disposition")]
    public async Task<ActionResult<HoldTagDto>> SetDisposition([FromBody] SetHoldTagDispositionRequestDto dto, CancellationToken ct)
        => Ok(await _holdTags.SetDispositionAsync(dto, ct));

    [HttpPost("link-ncr")]
    public async Task<ActionResult<HoldTagDto>> LinkNcr([FromBody] LinkHoldTagNcrRequestDto dto, CancellationToken ct)
        => Ok(await _holdTags.LinkNcrAsync(dto, ct));

    [HttpPost("resolve")]
    public async Task<ActionResult<HoldTagDto>> Resolve([FromBody] ResolveHoldTagRequestDto dto, CancellationToken ct)
        => Ok(await _holdTags.ResolveAsync(dto, ct));

    [HttpPost("void")]
    public async Task<ActionResult<HoldTagDto>> Void([FromBody] VoidHoldTagRequestDto dto, CancellationToken ct)
        => Ok(await _holdTags.VoidAsync(dto, ct));

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<IEnumerable<WorkflowEventDto>>> GetEvents(Guid id, CancellationToken ct)
    {
        var item = await _holdTags.GetByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(await _workflow.GetWorkflowEventsAsync(item.WorkflowInstanceId, ct));
    }
}
