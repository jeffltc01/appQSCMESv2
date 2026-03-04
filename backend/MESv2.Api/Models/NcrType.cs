namespace MESv2.Api.Models;

public class NcrType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsVendorRelated { get; set; }
    public string? Description { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
}
