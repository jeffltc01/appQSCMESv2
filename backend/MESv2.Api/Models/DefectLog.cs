namespace MESv2.Api.Models;

public class DefectLog
{
    public Guid Id { get; set; }
    public Guid? ProductionRecordId { get; set; }
    public Guid? InspectionRecordId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid DefectCodeId { get; set; }
    public Guid CharacteristicId { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationDetail { get; set; }
    public bool IsRepaired { get; set; }
    public Guid? RepairedByUserId { get; set; }
    public DateTime Timestamp { get; set; }

    public ProductionRecord? ProductionRecord { get; set; }
    public InspectionRecord? InspectionRecord { get; set; }
    public DefectCode DefectCode { get; set; } = null!;
    public Characteristic Characteristic { get; set; } = null!;
    public DefectLocation Location { get; set; } = null!;
    public User? RepairedByUser { get; set; }
}
