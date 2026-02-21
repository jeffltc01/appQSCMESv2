namespace MESv2.Api.DTOs;

public class SellableTankStatusDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string? RtXrayResult { get; set; }
    public string? SpotXrayResult { get; set; }
    public string? HydroResult { get; set; }
}
