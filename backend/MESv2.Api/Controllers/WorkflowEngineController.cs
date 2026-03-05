using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowEngineController : ControllerBase
{
    private readonly IWorkflowEngineService _workflow;

    public WorkflowEngineController(IWorkflowEngineService workflow)
    {
        _workflow = workflow;
    }

    [HttpGet("definitions")]
    public async Task<ActionResult<IEnumerable<WorkflowDefinitionDto>>> GetDefinitions([FromQuery] string? workflowType, CancellationToken ct)
        => Ok(await _workflow.GetDefinitionsAsync(workflowType, ct));

    [HttpPost("definitions")]
    public async Task<ActionResult<WorkflowDefinitionDto>> UpsertDefinition([FromBody] UpsertWorkflowDefinitionDto dto, CancellationToken ct)
    {
        var roleGuard = EnsureWorkflowDefinitionManager();
        if (roleGuard != null)
            return roleGuard;

        var actor = GetActorUserId();
        return Ok(await _workflow.UpsertDefinitionAsync(dto, actor, ct));
    }

    [HttpPost("definitions/validate")]
    public ActionResult<object> ValidateDefinition([FromBody] UpsertWorkflowDefinitionDto dto)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.WorkflowType)) errors.Add("WorkflowType is required.");
        if (string.IsNullOrWhiteSpace(dto.StartStepCode)) errors.Add("StartStepCode is required.");
        if (dto.Steps.Count == 0) errors.Add("At least one step is required.");
        if (dto.Steps.Count > 0 && !dto.Steps.Any(x => x.StepCode == dto.StartStepCode))
            errors.Add("StartStepCode must exist in steps.");

        var duplicates = dto.Steps.GroupBy(x => x.StepCode).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0) errors.Add($"Duplicate step codes: {string.Join(", ", duplicates)}");

        return Ok(new
        {
            isExecutable = errors.Count == 0,
            errors
        });
    }

    [HttpGet("notification-rules")]
    public async Task<ActionResult<IEnumerable<NotificationRuleDto>>> GetNotificationRules([FromQuery] string? workflowType, CancellationToken ct)
        => Ok(await _workflow.GetNotificationRulesAsync(workflowType, ct));

    [HttpPost("notification-rules")]
    public async Task<ActionResult<NotificationRuleDto>> UpsertNotificationRule([FromBody] NotificationRuleDto dto, CancellationToken ct)
    {
        var roleGuard = EnsureWorkflowDefinitionManager();
        if (roleGuard != null)
            return roleGuard;

        return Ok(await _workflow.UpsertNotificationRuleAsync(dto, ct));
    }

    [HttpPost("start")]
    public async Task<ActionResult<WorkflowInstanceDto>> Start([FromBody] StartWorkflowRequestDto dto, CancellationToken ct)
        => Ok(await _workflow.StartWorkflowAsync(dto, ct));

    [HttpPost("advance")]
    public async Task<ActionResult<WorkflowInstanceDto>> Advance([FromBody] AdvanceStepRequestDto dto, CancellationToken ct)
        => Ok(await _workflow.AdvanceStepAsync(dto, ct));

    [HttpPost("approve")]
    public async Task<ActionResult<WorkflowInstanceDto>> Approve([FromBody] ApproveRejectRequestDto dto, CancellationToken ct)
        => Ok(await _workflow.ApproveStepAsync(dto, ct));

    [HttpPost("reject")]
    public async Task<ActionResult<WorkflowInstanceDto>> Reject([FromBody] ApproveRejectRequestDto dto, CancellationToken ct)
        => Ok(await _workflow.RejectStepAsync(dto, ct));

    [HttpGet("work-items/open")]
    public async Task<ActionResult<IEnumerable<WorkItemDto>>> GetOpenWorkItems([FromQuery] Guid userId, [FromQuery] List<decimal> roleTiers, CancellationToken ct)
        => Ok(await _workflow.GetOpenWorkItemsAsync(userId, roleTiers, ct));

    [HttpPost("work-items/complete")]
    public async Task<ActionResult<WorkflowInstanceDto>> CompleteWorkItem([FromBody] CompleteWorkItemRequestDto dto, CancellationToken ct)
        => Ok(await _workflow.CompleteWorkItemAsync(dto, ct));

    [HttpGet("{workflowInstanceId:guid}/events")]
    public async Task<ActionResult<IEnumerable<WorkflowEventDto>>> GetEvents(Guid workflowInstanceId, CancellationToken ct)
        => Ok(await _workflow.GetWorkflowEventsAsync(workflowInstanceId, ct));

    private Guid? GetActorUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var actor))
            return actor;
        return null;
    }

    private ActionResult? EnsureWorkflowDefinitionManager()
    {
        if (!Request.Headers.TryGetValue("X-User-Role-Tier", out var headerValue) ||
            !decimal.TryParse(headerValue.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var callerRoleTier))
            return BadRequest(new { message = "Missing X-User-Role-Tier header." });
        // Security tiers are inclusive upward authority (lower number = higher privilege):
        // allow Administrator (1.0) and Directors (2.0).
        if (callerRoleTier > 2m)
            return Forbid();
        return null;
    }
}
