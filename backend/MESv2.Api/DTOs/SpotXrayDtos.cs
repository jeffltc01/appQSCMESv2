namespace MESv2.Api.DTOs;

public class SpotXrayQueueTankDto
{
    public int Position { get; set; }
    public Guid AssemblySerialNumberId { get; set; }
    public string AlphaCode { get; set; } = string.Empty;
    public List<string> ShellSerials { get; set; } = new();
    public int TankSize { get; set; }
    public string WeldType { get; set; } = string.Empty;
    public DateTime? RoundSeamWeldedAtUtc { get; set; }
    public string SeamWelders { get; set; } = string.Empty;
    public List<string> WelderNames { get; set; } = new();
    public List<Guid> WelderIds { get; set; } = new();
    public bool SizeChanged { get; set; }
    public bool WelderChanged { get; set; }
}

public class SpotXrayLaneDto
{
    public string LaneName { get; set; } = string.Empty;
    public int DraftCount { get; set; }
    public List<SpotXrayQueueTankDto> Tanks { get; set; } = new();
}

public class SpotXrayLaneQueuesDto
{
    public List<SpotXrayLaneDto> Lanes { get; set; } = new();
}

public class LaneSelectionDto
{
    public string LaneName { get; set; } = string.Empty;
    public List<int> SelectedPositions { get; set; } = new();
}

public class CreateSpotXrayIncrementsRequest
{
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
    public Guid? SiteId { get; set; }
    public string? SiteCode { get; set; }
    public List<LaneSelectionDto> LaneSelections { get; set; } = new();
}

public class CreateSpotXrayIncrementsResponse
{
    public List<SpotXrayIncrementSummaryDto> Increments { get; set; } = new();
}

public class SpotXrayIncrementSummaryDto
{
    public Guid Id { get; set; }
    public string IncrementNo { get; set; } = string.Empty;
    public string LaneNo { get; set; } = string.Empty;
    public int? TankSize { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
}

public class SpotXrayIncrementTankDto
{
    public Guid SerialNumberId { get; set; }
    public string AlphaCode { get; set; } = string.Empty;
    public List<string> ShellSerials { get; set; } = new();
    public int Position { get; set; }
}

public class SpotXraySeamDto
{
    public int SeamNumber { get; set; }
    public string? WelderName { get; set; }
    public Guid? WelderId { get; set; }
    public string? ShotNo { get; set; }
    public DateTime? ShotDateTime { get; set; }
    public string? Result { get; set; }
    public string? Trace1ShotNo { get; set; }
    public DateTime? Trace1DateTime { get; set; }
    public Guid? Trace1TankId { get; set; }
    public string? Trace1TankAlpha { get; set; }
    public string? Trace1Result { get; set; }
    public string? Trace2ShotNo { get; set; }
    public DateTime? Trace2DateTime { get; set; }
    public Guid? Trace2TankId { get; set; }
    public string? Trace2TankAlpha { get; set; }
    public string? Trace2Result { get; set; }
    public string? FinalShotNo { get; set; }
    public DateTime? FinalDateTime { get; set; }
    public string? FinalResult { get; set; }
}

public class SpotXrayIncrementDetailDto
{
    public Guid Id { get; set; }
    public string IncrementNo { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty;
    public string LaneNo { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public int? TankSize { get; set; }
    public int SeamCount { get; set; }
    public Guid? InspectTankId { get; set; }
    public string? InspectTankAlpha { get; set; }
    public List<SpotXrayIncrementTankDto> Tanks { get; set; } = new();
    public List<SpotXraySeamDto> Seams { get; set; } = new();
    public DateTime? CreatedDateTime { get; set; }
}

public class SaveSpotXraySeamDto
{
    public int SeamNumber { get; set; }
    public string? ShotNo { get; set; }
    public string? Result { get; set; }
    public string? Trace1ShotNo { get; set; }
    public Guid? Trace1TankId { get; set; }
    public string? Trace1Result { get; set; }
    public string? Trace2ShotNo { get; set; }
    public Guid? Trace2TankId { get; set; }
    public string? Trace2Result { get; set; }
    public string? FinalShotNo { get; set; }
    public string? FinalResult { get; set; }
}

public class SaveSpotXrayResultsRequest
{
    public Guid? InspectTankId { get; set; }
    public bool IsDraft { get; set; }
    public Guid OperatorId { get; set; }
    public List<SaveSpotXraySeamDto> Seams { get; set; } = new();
}

public class NextShotNumberRequest
{
    public Guid PlantId { get; set; }
}

public class NextShotNumberResponse
{
    public int ShotNumber { get; set; }
}
