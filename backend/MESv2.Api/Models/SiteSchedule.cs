namespace MESv2.Api.Models;

public class SiteSchedule
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityComplete { get; set; }
    public int TankSize { get; set; }
    public string? TankType { get; set; }
    public string? ItemNo { get; set; }
    public string? ColorName { get; set; }
    public string? ColorValue { get; set; }
    public DateTime? BuildDate { get; set; }
    public DateTime? DispatchDate { get; set; }
    public string? Comments { get; set; }
    public string? Status { get; set; }
    public Guid? MasterScheduleId { get; set; }
}
