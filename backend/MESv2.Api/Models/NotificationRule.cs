namespace MESv2.Api.Models;

public class NotificationRule
{
    public Guid Id { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string RecipientMode { get; set; } = string.Empty;
    public string RecipientConfigJson { get; set; } = "{}";
    public string TemplateKey { get; set; } = string.Empty;
    public string ClearPolicy { get; set; } = "None";
    public bool IsActive { get; set; } = true;
}
