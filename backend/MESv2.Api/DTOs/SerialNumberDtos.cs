namespace MESv2.Api.DTOs;

public class SerialNumberContextDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string? ShellSize { get; set; }
    public ExistingAssemblyDto? ExistingAssembly { get; set; }
}

public class ExistingAssemblyDto
{
    public string AlphaCode { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public List<string> Shells { get; set; } = new();
    public HeadLotInfoDto? LeftHeadInfo { get; set; }
    public HeadLotInfoDto? RightHeadInfo { get; set; }
}

public class HeadLotInfoDto
{
    public string HeatNumber { get; set; } = string.Empty;
    public string CoilNumber { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
}
