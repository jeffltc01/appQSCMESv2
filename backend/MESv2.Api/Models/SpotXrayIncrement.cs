namespace MESv2.Api.Models;

public class SpotXrayIncrement
{
    public Guid Id { get; set; }
    public Guid ManufacturingLogId { get; set; }
    public string IncrementNo { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty;
    public string LaneNo { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public int? TankSize { get; set; }
    public string? InspectTank { get; set; }
    public Guid? InspectTankId { get; set; }

    public string? Seam1ShotNo { get; set; }
    public string? Seam1ShotDateTime { get; set; }
    public string? Seam1InitialResult { get; set; }
    public string? Seam1FinalResult { get; set; }
    public string? Seam2ShotNo { get; set; }
    public string? Seam2ShotDateTime { get; set; }
    public string? Seam2InitialResult { get; set; }
    public string? Seam2FinalResult { get; set; }
    public string? Seam3ShotNo { get; set; }
    public string? Seam3ShotDateTime { get; set; }
    public string? Seam3InitialResult { get; set; }
    public string? Seam3FinalResult { get; set; }
    public string? Seam4ShotNo { get; set; }
    public string? Seam4ShotDateTime { get; set; }
    public string? Seam4InitialResult { get; set; }
    public string? Seam4FinalResult { get; set; }

    public string? Welder1 { get; set; }
    public string? Welder2 { get; set; }
    public string? Welder3 { get; set; }
    public string? Welder4 { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public Guid? ModifiedByUserId { get; set; }
    public DateTime? ModifiedDateTime { get; set; }

    public ProductionRecord ProductionRecord { get; set; } = null!;
    public SerialNumber? InspectTankSn { get; set; }
    public User? CreatedByUser { get; set; }
    public User? ModifiedByUser { get; set; }
}
