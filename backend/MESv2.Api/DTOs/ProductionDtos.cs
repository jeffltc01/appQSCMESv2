namespace MESv2.Api.DTOs;

public class CreateProductionRecordDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid OperatorId { get; set; }
    public List<Guid> WelderIds { get; set; } = new();
    public string? ShellSize { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilNumber { get; set; }
}

public class CreateProductionRecordResponseDto
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Warning { get; set; }
}

public class CreateInspectionRecordDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid OperatorId { get; set; }
    public List<InspectionResultEntryDto> Results { get; set; } = new();
    public List<DefectEntryDto> Defects { get; set; } = new();
}

public class InspectionResultEntryDto
{
    public Guid ControlPlanId { get; set; }
    public string ResultText { get; set; } = string.Empty;
}

public class DefectEntryDto
{
    public Guid DefectCodeId { get; set; }
    public Guid CharacteristicId { get; set; }
    public Guid LocationId { get; set; }
}
