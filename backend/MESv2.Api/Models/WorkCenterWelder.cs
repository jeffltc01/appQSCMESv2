namespace MESv2.Api.Models;

public class WorkCenterWelder
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AssignedAt { get; set; }

    public WorkCenter WorkCenter { get; set; } = null!;
    public User User { get; set; } = null!;
}
