namespace MESv2.Api.Models;

public class DefectLocation
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultLocationDetail { get; set; }
    public Guid? CharacteristicId { get; set; }

    public Characteristic? Characteristic { get; set; }
    public ICollection<DefectLog> DefectLogs { get; set; } = new List<DefectLog>();
}
