namespace MESv2.Api.Models;

public static class DemoShellStage
{
    public const string Rolls = "Rolls";
    public const string LongSeam = "LongSeam";
    public const string LongSeamInspection = "LongSeamInspection";
    public const string Completed = "Completed";
}

public class DemoShellFlow
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public int ShellNumber { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = DemoShellStage.Rolls;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime StageEnteredAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Plant Plant { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
