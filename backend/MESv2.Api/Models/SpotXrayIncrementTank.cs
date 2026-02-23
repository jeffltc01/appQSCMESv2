namespace MESv2.Api.Models;

public class SpotXrayIncrementTank
{
    public Guid Id { get; set; }
    public Guid SpotXrayIncrementId { get; set; }
    public Guid SerialNumberId { get; set; }
    public int Position { get; set; }

    public SpotXrayIncrement SpotXrayIncrement { get; set; } = null!;
    public SerialNumber SerialNumber { get; set; } = null!;
}
