namespace MESv2.Api.Models;

public class WorkflowStepDefinition
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string RequiredFieldsJson { get; set; } = "[]";
    public string RequiredChecklistTemplateIdsJson { get; set; } = "[]";
    public string ApprovalMode { get; set; } = "None";
    public string ApprovalAssignmentsJson { get; set; } = "[]";
    public bool AllowReject { get; set; }
    public string? OnApproveNextStepCode { get; set; }
    public string? OnRejectTargetStepCode { get; set; }

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}
