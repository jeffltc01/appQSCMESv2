namespace MESv2.Api.Models;

public class DefectCode
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? SystemType { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DefectLog> DefectLogs { get; set; } = new List<DefectLog>();
}
