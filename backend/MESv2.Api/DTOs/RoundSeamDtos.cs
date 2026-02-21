namespace MESv2.Api.DTOs;

public class RoundSeamSetupDto
{
    public Guid Id { get; set; }
    public int TankSize { get; set; }
    public Guid? Rs1WelderId { get; set; }
    public Guid? Rs2WelderId { get; set; }
    public Guid? Rs3WelderId { get; set; }
    public Guid? Rs4WelderId { get; set; }
    public bool IsComplete { get; set; }
}

public class CreateRoundSeamSetupDto
{
    public int TankSize { get; set; }
    public Guid? Rs1WelderId { get; set; }
    public Guid? Rs2WelderId { get; set; }
    public Guid? Rs3WelderId { get; set; }
    public Guid? Rs4WelderId { get; set; }
}

public class CreateRoundSeamRecordDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid AssetId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
}

public class AssemblyLookupDto
{
    public string AlphaCode { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public int RoundSeamCount { get; set; }
    public List<string> Shells { get; set; } = new();
}
