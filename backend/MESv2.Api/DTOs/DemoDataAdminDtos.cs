namespace MESv2.Api.DTOs;

public class DemoDataTableCountDto
{
    public string Table { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DemoDataResetSeedResultDto
{
    public DateTime ExecutedAtUtc { get; set; }
    public List<DemoDataTableCountDto> Deleted { get; set; } = new();
    public List<DemoDataTableCountDto> Inserted { get; set; } = new();
}

public class DemoDataRefreshDatesResultDto
{
    public DateTime ExecutedAtUtc { get; set; }
    public double AppliedDeltaHours { get; set; }
    public List<DemoDataTableCountDto> Updated { get; set; } = new();
}
