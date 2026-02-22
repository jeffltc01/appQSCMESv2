namespace MESv2.Api.Models;

public class ProductionRecord
{
    public Guid Id { get; set; }
    public Guid SerialNumberId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? ProductInId { get; set; }
    public Guid? ProductOutId { get; set; }
    public Guid? PlantGearId { get; set; }

    public SerialNumber SerialNumber { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public Asset? Asset { get; set; }
    public ProductionLine ProductionLine { get; set; } = null!;
    public User Operator { get; set; } = null!;
    public PlantGear? PlantGear { get; set; }
    public ICollection<WelderLog> WelderLogs { get; set; } = new List<WelderLog>();
    public ICollection<DefectLog> DefectLogs { get; set; } = new List<DefectLog>();
    public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
}
