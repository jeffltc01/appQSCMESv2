namespace MESv2.Api.Models;

public class WorkCenterCapacityTarget
{
    public Guid Id { get; set; }
    public Guid WorkCenterProductionLineId { get; set; }
    public int? TankSize { get; set; }
    public Guid PlantGearId { get; set; }
    public decimal TargetUnitsPerHour { get; set; }

    public WorkCenterProductionLine WorkCenterProductionLine { get; set; } = null!;
    public PlantGear PlantGear { get; set; } = null!;
}
