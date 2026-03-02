namespace MESv2.Api.DTOs;

public class WhereUsedResultDto
{
    public string Plant { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string ProductionNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public DateTime? HydroCompletedAt { get; set; }
}
