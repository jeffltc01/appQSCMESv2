namespace MESv2.Api.DTOs;

public class DigitalTwinSnapshotDto
{
    public List<StationStatusDto> Stations { get; set; } = new();
    public List<MaterialFeedDto> MaterialFeeds { get; set; } = new();
    public LineThroughputDto Throughput { get; set; } = new();
    public decimal AvgCycleTimeMinutes { get; set; }
    public decimal LineEfficiencyPercent { get; set; }
    public List<UnitPositionDto> UnitTracker { get; set; } = new();
}

public class StationStatusDto
{
    public Guid WorkCenterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public int WipCount { get; set; }
    public string Status { get; set; } = "Idle";
    public bool IsBottleneck { get; set; }
    public bool IsGateCheck { get; set; }
    public string? CurrentOperator { get; set; }
    public int UnitsToday { get; set; }
    public decimal? AvgCycleTimeMinutes { get; set; }
    public decimal? FirstPassYieldPercent { get; set; }
}

public class MaterialFeedDto
{
    public string WorkCenterName { get; set; } = string.Empty;
    public string QueueLabel { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string FeedsIntoStation { get; set; } = string.Empty;
}

public class LineThroughputDto
{
    public int UnitsToday { get; set; }
    public int UnitsDelta { get; set; }
    public decimal UnitsPerHour { get; set; }
}

public class UnitPositionDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string CurrentStationName { get; set; } = string.Empty;
    public int CurrentStationSequence { get; set; }
    public DateTime EnteredCurrentStationAt { get; set; }
    public bool IsAssembly { get; set; }
}
