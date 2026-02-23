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

    // Seam 1
    public string? Seam1ShotNo { get; set; }
    public DateTime? Seam1ShotDateTime { get; set; }
    public string? Seam1Result { get; set; }
    public string? Seam1Trace1ShotNo { get; set; }
    public DateTime? Seam1Trace1DateTime { get; set; }
    public Guid? Seam1Trace1TankId { get; set; }
    public string? Seam1Trace1Result { get; set; }
    public string? Seam1Trace2ShotNo { get; set; }
    public DateTime? Seam1Trace2DateTime { get; set; }
    public Guid? Seam1Trace2TankId { get; set; }
    public string? Seam1Trace2Result { get; set; }
    public string? Seam1FinalShotNo { get; set; }
    public DateTime? Seam1FinalDateTime { get; set; }
    public string? Seam1FinalResult { get; set; }

    // Seam 2
    public string? Seam2ShotNo { get; set; }
    public DateTime? Seam2ShotDateTime { get; set; }
    public string? Seam2Result { get; set; }
    public string? Seam2Trace1ShotNo { get; set; }
    public DateTime? Seam2Trace1DateTime { get; set; }
    public Guid? Seam2Trace1TankId { get; set; }
    public string? Seam2Trace1Result { get; set; }
    public string? Seam2Trace2ShotNo { get; set; }
    public DateTime? Seam2Trace2DateTime { get; set; }
    public Guid? Seam2Trace2TankId { get; set; }
    public string? Seam2Trace2Result { get; set; }
    public string? Seam2FinalShotNo { get; set; }
    public DateTime? Seam2FinalDateTime { get; set; }
    public string? Seam2FinalResult { get; set; }

    // Seam 3
    public string? Seam3ShotNo { get; set; }
    public DateTime? Seam3ShotDateTime { get; set; }
    public string? Seam3Result { get; set; }
    public string? Seam3Trace1ShotNo { get; set; }
    public DateTime? Seam3Trace1DateTime { get; set; }
    public Guid? Seam3Trace1TankId { get; set; }
    public string? Seam3Trace1Result { get; set; }
    public string? Seam3Trace2ShotNo { get; set; }
    public DateTime? Seam3Trace2DateTime { get; set; }
    public Guid? Seam3Trace2TankId { get; set; }
    public string? Seam3Trace2Result { get; set; }
    public string? Seam3FinalShotNo { get; set; }
    public DateTime? Seam3FinalDateTime { get; set; }
    public string? Seam3FinalResult { get; set; }

    // Seam 4
    public string? Seam4ShotNo { get; set; }
    public DateTime? Seam4ShotDateTime { get; set; }
    public string? Seam4Result { get; set; }
    public string? Seam4Trace1ShotNo { get; set; }
    public DateTime? Seam4Trace1DateTime { get; set; }
    public Guid? Seam4Trace1TankId { get; set; }
    public string? Seam4Trace1Result { get; set; }
    public string? Seam4Trace2ShotNo { get; set; }
    public DateTime? Seam4Trace2DateTime { get; set; }
    public Guid? Seam4Trace2TankId { get; set; }
    public string? Seam4Trace2Result { get; set; }
    public string? Seam4FinalShotNo { get; set; }
    public DateTime? Seam4FinalDateTime { get; set; }
    public string? Seam4FinalResult { get; set; }

    // Welders (FK to User — one per round seam position)
    public Guid? Welder1Id { get; set; }
    public Guid? Welder2Id { get; set; }
    public Guid? Welder3Id { get; set; }
    public Guid? Welder4Id { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public Guid? ModifiedByUserId { get; set; }
    public DateTime? ModifiedDateTime { get; set; }

    // Navigation properties
    public ProductionRecord ProductionRecord { get; set; } = null!;
    public SerialNumber? InspectTankSn { get; set; }
    public SerialNumber? Seam1Trace1Tank { get; set; }
    public SerialNumber? Seam1Trace2Tank { get; set; }
    public SerialNumber? Seam2Trace1Tank { get; set; }
    public SerialNumber? Seam2Trace2Tank { get; set; }
    public SerialNumber? Seam3Trace1Tank { get; set; }
    public SerialNumber? Seam3Trace2Tank { get; set; }
    public SerialNumber? Seam4Trace1Tank { get; set; }
    public SerialNumber? Seam4Trace2Tank { get; set; }
    public User? Welder1 { get; set; }
    public User? Welder2 { get; set; }
    public User? Welder3 { get; set; }
    public User? Welder4 { get; set; }
    public User? CreatedByUser { get; set; }
    public User? ModifiedByUser { get; set; }
    public ICollection<SpotXrayIncrementTank> IncrementTanks { get; set; } = new List<SpotXrayIncrementTank>();
}
