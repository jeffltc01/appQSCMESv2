namespace MESv2.Api.Models;

public class RoundSeamSetup
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public int TankSize { get; set; }
    public Guid? Rs1WelderId { get; set; }
    public Guid? Rs2WelderId { get; set; }
    public Guid? Rs3WelderId { get; set; }
    public Guid? Rs4WelderId { get; set; }
    public DateTime CreatedAt { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public User? Rs1Welder { get; set; }
    public User? Rs2Welder { get; set; }
    public User? Rs3Welder { get; set; }
    public User? Rs4Welder { get; set; }
}
