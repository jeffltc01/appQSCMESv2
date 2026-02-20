namespace MESv2.Api.Models;

public class CharacteristicWorkCenter
{
    public Guid Id { get; set; }
    public Guid CharacteristicId { get; set; }
    public Guid WorkCenterId { get; set; }

    public Characteristic Characteristic { get; set; } = null!;
    public WorkCenter WorkCenter { get; set; } = null!;
}
