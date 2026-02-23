namespace MESv2.Api.Models;

public class XrayShotCounter
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public DateOnly CounterDate { get; set; }
    public int LastShotNumber { get; set; }
    public int LastIncrementNumber { get; set; }

    public Plant Plant { get; set; } = null!;
}
