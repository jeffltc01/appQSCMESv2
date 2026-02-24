namespace MESv2.Api.DTOs;

public class DemoShellCurrentDto
{
    public string Stage { get; set; } = string.Empty;
    public bool HasCurrent { get; set; }
    public string? BarcodeRaw { get; set; }
    public string? SerialNumber { get; set; }
    public int? ShellNumber { get; set; }
    public int StageQueueCount { get; set; }
}

public class DemoShellAdvanceRequestDto
{
    public Guid WorkCenterId { get; set; }
}
