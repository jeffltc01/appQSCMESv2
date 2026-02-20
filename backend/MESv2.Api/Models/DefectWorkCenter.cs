namespace MESv2.Api.Models;

public class DefectWorkCenter
{
    public Guid Id { get; set; }
    public Guid DefectCodeId { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? EarliestDetectionWorkCenterId { get; set; }

    public DefectCode DefectCode { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
    public WorkCenter? EarliestDetectionWorkCenter { get; set; }
}
