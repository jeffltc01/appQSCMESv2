using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IWorkflowEngineService
{
    Task<IReadOnlyList<WorkflowDefinitionDto>> GetDefinitionsAsync(string? workflowType, CancellationToken ct = default);
    Task<WorkflowDefinitionDto> UpsertDefinitionAsync(UpsertWorkflowDefinitionDto dto, Guid? actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationRuleDto>> GetNotificationRulesAsync(string? workflowType, CancellationToken ct = default);
    Task<NotificationRuleDto> UpsertNotificationRuleAsync(NotificationRuleDto dto, CancellationToken ct = default);
    Task<WorkflowInstanceDto> StartWorkflowAsync(StartWorkflowRequestDto dto, CancellationToken ct = default);
    Task<WorkflowInstanceDto> AdvanceStepAsync(AdvanceStepRequestDto dto, CancellationToken ct = default);
    Task<WorkflowInstanceDto> ApproveStepAsync(ApproveRejectRequestDto dto, CancellationToken ct = default);
    Task<WorkflowInstanceDto> RejectStepAsync(ApproveRejectRequestDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<WorkItemDto>> GetOpenWorkItemsAsync(Guid userId, IReadOnlyList<decimal> roleTiers, CancellationToken ct = default);
    Task<WorkflowInstanceDto> CompleteWorkItemAsync(CompleteWorkItemRequestDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowEventDto>> GetWorkflowEventsAsync(Guid workflowInstanceId, CancellationToken ct = default);
    Task CancelOpenWorkItemsAsync(Guid workflowInstanceId, Guid? actorUserId, string reason, CancellationToken ct = default);
}
