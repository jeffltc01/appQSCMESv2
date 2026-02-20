namespace MESv2.Api.DTOs;

public class PlantDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ProductionLineDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
}

public class WorkCenterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
    public Guid WorkCenterTypeId { get; set; }
    public string WorkCenterTypeName { get; set; } = string.Empty;
    public bool RequiresWelder { get; set; }
    public Guid? ProductionLineId { get; set; }
    public string? DataEntryType { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }
}

public class AssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
}

public class MaterialQueueItemDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public string? ShellSize { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public string CoilNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? CardId { get; set; }
    public string? CardColor { get; set; }
}

public class DefectCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Severity { get; set; }
}

public class DefectLocationDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class CharacteristicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class InspectionRecordResponseDto
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid OperatorId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<DefectEntryResponseDto> Defects { get; set; } = new();
}

public class BarcodeCardDto
{
    public Guid Id { get; set; }
    public string CardValue { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? ColorName { get; set; }
    public bool IsAssigned { get; set; }
}

public class DefectEntryResponseDto
{
    public Guid DefectCodeId { get; set; }
    public string? DefectCodeName { get; set; }
    public Guid CharacteristicId { get; set; }
    public string? CharacteristicName { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationName { get; set; }
}
