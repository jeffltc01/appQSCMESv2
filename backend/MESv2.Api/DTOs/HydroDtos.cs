namespace MESv2.Api.DTOs;

public class CreateHydroRecordDto
{
    public string AssemblyAlphaCode { get; set; } = string.Empty;
    public string NameplateSerialNumber { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid OperatorId { get; set; }
    public List<DefectEntryDto> Defects { get; set; } = new();
}

public class HydroRecordResponseDto
{
    public Guid Id { get; set; }
    public string AssemblyAlphaCode { get; set; } = string.Empty;
    public string NameplateSerialNumber { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
