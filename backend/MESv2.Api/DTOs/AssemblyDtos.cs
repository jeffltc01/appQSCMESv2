namespace MESv2.Api.DTOs;

public class CreateAssemblyDto
{
    public List<string> Shells { get; set; } = new();
    public string LeftHeadLotId { get; set; } = string.Empty;
    public string RightHeadLotId { get; set; } = string.Empty;
    public string? LeftHeadHeatNumber { get; set; }
    public string? LeftHeadCoilNumber { get; set; }
    public string? LeftHeadLotNumber { get; set; }
    public string? RightHeadHeatNumber { get; set; }
    public string? RightHeadCoilNumber { get; set; }
    public string? RightHeadLotNumber { get; set; }
    public int TankSize { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid? AssetId { get; set; }
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
    public string OperationType { get; set; } = "replace"; // replace | split
    public ReassemblyAssemblyDto PrimaryAssembly { get; set; } = new();
    public ReassemblyAssemblyDto? SecondaryAssembly { get; set; } // required for split
    public Guid WorkCenterId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
    public List<Guid> WelderIds { get; set; } = new();
}

public class ReassemblyAssemblyDto
{
    public List<string> Shells { get; set; } = new();
    public int TankSize { get; set; }
    public ReassemblyHeadDto? LeftHead { get; set; }
    public ReassemblyHeadDto? RightHead { get; set; }
}

public class ReassemblyHeadDto
{
    public string? LotId { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilNumber { get; set; }
    public string? LotNumber { get; set; }
}

public class ReassembleResponseDto
{
    public string SourceAlphaCode { get; set; } = string.Empty;
    public List<CreateAssemblyResponseDto> CreatedAssemblies { get; set; } = new();
}
