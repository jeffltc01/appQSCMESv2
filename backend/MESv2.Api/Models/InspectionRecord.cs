namespace MESv2.Api.Models;

public class InspectionRecord
{
    public Guid Id { get; set; }
    public Guid SerialNumberId { get; set; }
    public Guid ProductionRecordId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? ControlPlanId { get; set; }
    public string? ResultText { get; set; }
    public decimal? ResultNumeric { get; set; }
    public Guid? SpotIncrementId { get; set; }
    public string? SystemNotes { get; set; }

    public SerialNumber SerialNumber { get; set; } = null!;
    public ProductionRecord ProductionRecord { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public User Operator { get; set; } = null!;
    public ControlPlan? ControlPlan { get; set; }
    public ICollection<DefectLog> DefectLogs { get; set; } = new List<DefectLog>();
}
