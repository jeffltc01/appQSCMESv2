namespace MESv2.Api.Models;

public class ControlPlan
{
    public Guid Id { get; set; }
    public Guid CharacteristicId { get; set; }
    public Guid WorkCenterId { get; set; }
    public bool IsEnabled { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public bool IsGateCheck { get; set; }

    public Characteristic Characteristic { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
}
