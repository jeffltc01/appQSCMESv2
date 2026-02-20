namespace MESv2.Api.DTOs;

public class TabletSetupDto
{
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid? AssetId { get; set; }
}
