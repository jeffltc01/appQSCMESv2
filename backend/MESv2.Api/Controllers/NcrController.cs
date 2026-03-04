using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/ncr")]
public class NcrController : ControllerBase
{
    private readonly INcrService _ncrService;
    private readonly IWorkflowEngineService _workflow;

    public NcrController(INcrService ncrService, IWorkflowEngineService workflow)
    {
        _ncrService = ncrService;
        _workflow = workflow;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NcrDto>>> GetList([FromQuery] string? siteCode, CancellationToken ct)
        => Ok(await _ncrService.GetNcrsAsync(siteCode, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NcrDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _ncrService.GetNcrByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<NcrDto>> Create([FromBody] CreateNcrRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.CreateNcrAsync(dto, ct));

    [HttpPut("data")]
    public async Task<ActionResult<NcrDto>> UpdateData([FromBody] UpdateNcrDataRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.UpdateNcrDataAsync(dto, ct));

    [HttpPost("submit")]
    public async Task<ActionResult<NcrDto>> SubmitStep([FromBody] SubmitNcrStepRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.SubmitNcrStepAsync(dto, ct));

    [HttpPost("approve")]
    public async Task<ActionResult<NcrDto>> Approve([FromBody] NcrDecisionRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.ApproveNcrStepAsync(dto, ct));

    [HttpPost("reject")]
    public async Task<ActionResult<NcrDto>> Reject([FromBody] NcrDecisionRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.RejectNcrStepAsync(dto, ct));

    [HttpPost("void")]
    public async Task<ActionResult<NcrDto>> VoidNcr([FromBody] VoidNcrRequestDto dto, CancellationToken ct)
        => Ok(await _ncrService.VoidNcrAsync(dto, ct));

    [HttpPost("attachments")]
    public async Task<ActionResult> AddAttachment([FromBody] AddNcrAttachmentRequestDto dto, CancellationToken ct)
    {
        await _ncrService.AddAttachmentAsync(dto, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<IEnumerable<WorkflowEventDto>>> GetEvents(Guid id, CancellationToken ct)
    {
        var item = await _ncrService.GetNcrByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(await _workflow.GetWorkflowEventsAsync(item.WorkflowInstanceId, ct));
    }
}
