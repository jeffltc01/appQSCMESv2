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

public class SerialNumberLookupDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public List<TraceabilityNodeDto> TreeNodes { get; set; } = new();
}

public class TraceabilityNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public int? TankSize { get; set; }
    public string? TankType { get; set; }
    public string? VendorName { get; set; }
    public string? CoilNumber { get; set; }
    public string? HeatNumber { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int DefectCount { get; set; }
    public int AnnotationCount { get; set; }
    public List<string> ChildSerials { get; set; } = new();
    public List<TraceabilityNodeDto> Children { get; set; } = new();
    public List<ManufacturingEventDto> Events { get; set; } = new();
}

public class ManufacturingEventDto
{
    public string SerialNumberId { get; set; } = string.Empty;
    public string SerialNumberSerial { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CompletedBy { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public string? InspectionResult { get; set; }
}
