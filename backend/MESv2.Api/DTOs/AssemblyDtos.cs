namespace MESv2.Api.DTOs;

public class CreateAssemblyDto
{
    public List<string> Shells { get; set; } = new();
    public string LeftHeadLotId { get; set; } = string.Empty;
    public string RightHeadLotId { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid AssetId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
    public List<Guid> WelderIds { get; set; } = new();
}

public class CreateAssemblyResponseDto
{
    public Guid Id { get; set; }
    public string AlphaCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ReassemblyDto
{
    public List<string>? Shells { get; set; }
    public string? LeftHeadLotId { get; set; }
    public string? RightHeadLotId { get; set; }
    public Guid? OperatorId { get; set; }
    public List<Guid>? WelderIds { get; set; }
}
