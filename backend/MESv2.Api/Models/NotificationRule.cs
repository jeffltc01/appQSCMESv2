namespace MESv2.Api.Models;

public class NotificationRule
{
    public Guid Id { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string TargetStepCodesJson { get; set; } = "[]";
    public string RecipientMode { get; set; } = string.Empty;
    public string RecipientConfigJson { get; set; } = "{}";
    public string TemplateKey { get; set; } = string.Empty;
    public string TemplateTitle { get; set; } = string.Empty;
    public string TemplateBody { get; set; } = string.Empty;
    public string ClearPolicy { get; set; } = "None";
    public bool IsActive { get; set; } = true;
}
