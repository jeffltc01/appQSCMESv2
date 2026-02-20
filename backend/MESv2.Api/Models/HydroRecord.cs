namespace MESv2.Api.Models;

public class HydroRecord
{
    public Guid Id { get; set; }
    public string AssemblyAlphaCode { get; set; } = string.Empty;
    public string NameplateSerialNumber { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty; // ACCEPTED, REJECTED
    public Guid WorkCenterId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime Timestamp { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public Asset? Asset { get; set; }
    public User Operator { get; set; } = null!;
    public ICollection<DefectLog> DefectLogs { get; set; } = new List<DefectLog>();
}
