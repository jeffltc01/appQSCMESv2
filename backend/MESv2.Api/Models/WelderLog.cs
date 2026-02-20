namespace MESv2.Api.Models;

public class WelderLog
{
    public Guid Id { get; set; }
    public Guid ProductionRecordId { get; set; }
    public Guid UserId { get; set; }
    public Guid? CharacteristicId { get; set; }

    public ProductionRecord ProductionRecord { get; set; } = null!;
    public User User { get; set; } = null!;
    public Characteristic? Characteristic { get; set; }
}
