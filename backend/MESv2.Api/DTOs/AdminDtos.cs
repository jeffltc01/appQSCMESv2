namespace MESv2.Api.DTOs;

// ---- Products ----
public class AdminProductDto
{
    public Guid Id { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public string? SiteNumbers { get; set; }
    public Guid ProductTypeId { get; set; }
    public string ProductTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateProductDto
{
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public string? SiteNumbers { get; set; }
    public Guid ProductTypeId { get; set; }
}

public class UpdateProductDto
{
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public string? SiteNumbers { get; set; }
    public Guid ProductTypeId { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---- Users ----
public class AdminUserDto
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal RoleTier { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid DefaultSiteId { get; set; }
    public string DefaultSiteName { get; set; } = string.Empty;
    public bool IsCertifiedWelder { get; set; }
    public bool RequirePinForLogin { get; set; }
    public bool HasPin { get; set; }
    public int UserType { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUserDto
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal RoleTier { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid DefaultSiteId { get; set; }
    public bool IsCertifiedWelder { get; set; }
    public bool RequirePinForLogin { get; set; }
    public string? Pin { get; set; }
    public int UserType { get; set; }
}

public class UpdateUserDto
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal RoleTier { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid DefaultSiteId { get; set; }
    public bool IsCertifiedWelder { get; set; }
    public bool RequirePinForLogin { get; set; }
    public string? Pin { get; set; }
    public int UserType { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ChangePinDto
{
    public string? CurrentPin { get; set; }
    public string NewPin { get; set; } = string.Empty;
}

// ---- Vendors ----
public class AdminVendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? PlantIds { get; set; }
    public bool IsActive { get; set; }
}

public class CreateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? PlantIds { get; set; }
}

public class UpdateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? PlantIds { get; set; }
    public bool IsActive { get; set; }
}

// ---- Defect Codes ----
public class AdminDefectCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? SystemType { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class CreateDefectCodeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? SystemType { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
}

public class UpdateDefectCodeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? SystemType { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

// ---- Defect Locations ----
public class AdminDefectLocationDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultLocationDetail { get; set; }
    public Guid? CharacteristicId { get; set; }
    public string? CharacteristicName { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateDefectLocationDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultLocationDetail { get; set; }
    public Guid? CharacteristicId { get; set; }
}

public class UpdateDefectLocationDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultLocationDetail { get; set; }
    public Guid? CharacteristicId { get; set; }
    public bool IsActive { get; set; } = true;
}

// ---- Work Centers ----
public class AdminWorkCenterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkCenterTypeName { get; set; } = string.Empty;
    public int NumberOfWelders { get; set; }
    public string? DataEntryType { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }
    public string? MaterialQueueForWCName { get; set; }
}

public class UpdateWorkCenterConfigDto
{
    public int NumberOfWelders { get; set; }
    public string? DataEntryType { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }
}

public class AdminWorkCenterGroupDto
{
    public Guid GroupId { get; set; }
    public string BaseName { get; set; } = string.Empty;
    public string WorkCenterTypeName { get; set; } = string.Empty;
    public string? DataEntryType { get; set; }
    public List<WorkCenterSiteConfigDto> SiteConfigs { get; set; } = new();
}

public class WorkCenterSiteConfigDto
{
    public Guid WorkCenterId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public int NumberOfWelders { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }
    public string? MaterialQueueForWCName { get; set; }
}

public class UpdateWorkCenterGroupDto
{
    public string BaseName { get; set; } = string.Empty;
    public string? DataEntryType { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }
}

public class CreateWorkCenterDto
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterTypeId { get; set; }
    public string? DataEntryType { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }
}

public class WorkCenterTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ---- Work Center Production Lines ----
public class AdminWorkCenterProductionLineDto
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public string ProductionLineName { get; set; } = string.Empty;
    public string PlantName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int NumberOfWelders { get; set; }
    public bool DowntimeTrackingEnabled { get; set; }
    public int DowntimeThresholdMinutes { get; set; }
}

public class CreateWorkCenterProductionLineDto
{
    public Guid ProductionLineId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int NumberOfWelders { get; set; }
}

public class UpdateWorkCenterProductionLineDto
{
    public string DisplayName { get; set; } = string.Empty;
    public int NumberOfWelders { get; set; }
    public bool DowntimeTrackingEnabled { get; set; }
    public int DowntimeThresholdMinutes { get; set; } = 5;
}

// ---- Characteristics ----
public class AdminCharacteristicDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? SpecHigh { get; set; }
    public decimal? SpecLow { get; set; }
    public decimal? SpecTarget { get; set; }
    public int? MinTankSize { get; set; }
    public Guid? ProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
    public bool IsActive { get; set; }
}

public class CreateCharacteristicDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? SpecHigh { get; set; }
    public decimal? SpecLow { get; set; }
    public decimal? SpecTarget { get; set; }
    public int? MinTankSize { get; set; }
    public Guid? ProductTypeId { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
}

public class UpdateCharacteristicDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? SpecHigh { get; set; }
    public decimal? SpecLow { get; set; }
    public decimal? SpecTarget { get; set; }
    public int? MinTankSize { get; set; }
    public Guid? ProductTypeId { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
    public bool IsActive { get; set; }
}

// ---- Control Plans ----
public class AdminControlPlanDto
{
    public Guid Id { get; set; }
    public Guid CharacteristicId { get; set; }
    public string CharacteristicName { get; set; } = string.Empty;
    public Guid WorkCenterProductionLineId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public string ProductionLineName { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string PlantCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public bool IsGateCheck { get; set; }
    public bool CodeRequired { get; set; }
    public bool IsActive { get; set; }
}

public class CreateControlPlanDto
{
    public Guid CharacteristicId { get; set; }
    public Guid WorkCenterProductionLineId { get; set; }
    public bool IsEnabled { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public bool IsGateCheck { get; set; }
    public bool CodeRequired { get; set; }
}

public class UpdateControlPlanDto
{
    public bool IsEnabled { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public bool IsGateCheck { get; set; }
    public bool CodeRequired { get; set; }
    public bool IsActive { get; set; }
}

public class OperatorControlPlanDto
{
    public Guid Id { get; set; }
    public string CharacteristicName { get; set; } = string.Empty;
    public string ResultType { get; set; } = string.Empty;
    public bool IsGateCheck { get; set; }
}

// ---- Assets ----
public class AdminAssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public Guid ProductionLineId { get; set; }
    public string ProductionLineName { get; set; } = string.Empty;
    public string? LimbleIdentifier { get; set; }
    public string? LaneName { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAssetDto
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public string? LimbleIdentifier { get; set; }
    public string? LaneName { get; set; }
}

public class UpdateAssetDto
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public string? LimbleIdentifier { get; set; }
    public string? LaneName { get; set; }
    public bool IsActive { get; set; }
}

// ---- Kanban Cards ----
public class AdminBarcodeCardDto
{
    public Guid Id { get; set; }
    public string CardValue { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class CreateBarcodeCardDto
{
    public string CardValue { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

// ---- Plant Gear ----
public class PlantGearDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid PlantId { get; set; }
}

public class PlantWithGearDto
{
    public Guid PlantId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string PlantCode { get; set; } = string.Empty;
    public Guid? CurrentPlantGearId { get; set; }
    public int? CurrentGearLevel { get; set; }
    public string? LimbleLocationId { get; set; }
    public List<PlantGearDto> Gears { get; set; } = new();
}

public class SetPlantGearDto
{
    public Guid PlantGearId { get; set; }
}

public class UpdatePlantLimbleDto
{
    public string? LimbleLocationId { get; set; }
}

// ---- Active Sessions ----
public class ActiveSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
    public Guid ProductionLineId { get; set; }
    public string ProductionLineName { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public DateTime LoginDateTime { get; set; }
    public DateTime LastHeartbeatDateTime { get; set; }
    public bool IsStale { get; set; }
}

public class CreateActiveSessionDto
{
    public Guid WorkCenterId { get; set; }
    public Guid ProductionLineId { get; set; }
    public Guid? AssetId { get; set; }
    public Guid PlantId { get; set; }
}

// ---- Product Types (reference) ----
public class ProductTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ---- Production Lines ----
public class CreateProductionLineDto
{
    public string Name { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
}

public class UpdateProductionLineDto
{
    public string Name { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
}

// ---- Role reference ----
public class RoleOptionDto
{
    public decimal Tier { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ---- Annotation Types ----
public class AdminAnnotationTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public bool RequiresResolution { get; set; }
    public bool OperatorCanCreate { get; set; }
    public string? DisplayColor { get; set; }
}

public class CreateAnnotationTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public bool RequiresResolution { get; set; }
    public bool OperatorCanCreate { get; set; }
    public string? DisplayColor { get; set; }
}

public class UpdateAnnotationTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public bool RequiresResolution { get; set; }
    public bool OperatorCanCreate { get; set; }
    public string? DisplayColor { get; set; }
}

// ---- Plant Printers ----
public class AdminPlantPrinterDto
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string PlantCode { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string PrintLocation { get; set; } = string.Empty;
}

public class CreatePlantPrinterDto
{
    public Guid PlantId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string PrintLocation { get; set; } = string.Empty;
}

public class UpdatePlantPrinterDto
{
    public string PrinterName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string PrintLocation { get; set; } = string.Empty;
}

// ---- Annotations ----
public class AdminAnnotationDto
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string AnnotationTypeName { get; set; } = string.Empty;
    public Guid AnnotationTypeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string InitiatedByName { get; set; } = string.Empty;
    public string? ResolvedByName { get; set; }
    public string? ResolvedNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
    public string? LinkedEntityName { get; set; }
}

public class CreateAnnotationDto
{
    public Guid AnnotationTypeId { get; set; }
    public string? Notes { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string? LinkedEntityType { get; set; }
    public Guid? LinkedEntityId { get; set; }
}

public class UpdateAnnotationDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ResolvedNotes { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}

// ---- AI Review ----
public class AIReviewRecordDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string SerialOrIdentifier { get; set; } = string.Empty;
    public string? TankSize { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public bool AlreadyReviewed { get; set; }
}

public class CreateAIReviewRequest
{
    public List<Guid> ProductionRecordIds { get; set; } = new();
    public string? Comment { get; set; }
}

public class AIReviewResultDto
{
    public int AnnotationsCreated { get; set; }
}
