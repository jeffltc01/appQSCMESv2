namespace MESv2.Api.DTOs;

public class SerialProcessingBlockResultDto
{
    public bool IsBlocked { get; set; }
    public List<int> OpenHoldTagNumbers { get; set; } = new();
    public List<int> OpenNcrNumbers { get; set; } = new();
    public List<string> Reasons { get; set; } = new();
}
