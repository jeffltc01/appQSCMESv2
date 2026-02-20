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
    public Guid ProductTypeId { get; set; }
    public string ProductTypeName { get; set; } = string.Empty;
}

public class CreateProductDto
{
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public Guid ProductTypeId { get; set; }
}

public class UpdateProductDto
{
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public Guid ProductTypeId { get; set; }
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
}

public class UpdateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal RoleTier { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid DefaultSiteId { get; set; }
    public bool IsCertifiedWelder { get; set; }
    public bool RequirePinForLogin { get; set; }
}

// ---- Vendors ----
public class AdminVendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? SiteCode { get; set; }
    public bool IsActive { get; set; }
}

public class CreateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? SiteCode { get; set; }
}

public class UpdateVendorDto
{
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? SiteCode { get; set; }
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
}

// ---- Work Centers ----
public class AdminWorkCenterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WorkCenterTypeName { get; set; } = string.Empty;
    public string PlantName { get; set; } = string.Empty;
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

// ---- Characteristics ----
public class AdminCharacteristicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? SpecHigh { get; set; }
    public decimal? SpecLow { get; set; }
    public decimal? SpecTarget { get; set; }
    public Guid? ProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
}

public class UpdateCharacteristicDto
{
    public string Name { get; set; } = string.Empty;
    public decimal? SpecHigh { get; set; }
    public decimal? SpecLow { get; set; }
    public decimal? SpecTarget { get; set; }
    public Guid? ProductTypeId { get; set; }
    public List<Guid> WorkCenterIds { get; set; } = new();
}

// ---- Control Plans ----
public class AdminControlPlanDto
{
    public Guid Id { get; set; }
    public Guid CharacteristicId { get; set; }
    public string CharacteristicName { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string ResultType { get; set; } = string.Empty;
    public bool IsGateCheck { get; set; }
}

public class UpdateControlPlanDto
{
    public bool IsEnabled { get; set; }
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
    public string? LimbleIdentifier { get; set; }
}

public class CreateAssetDto
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public string? LimbleIdentifier { get; set; }
}

public class UpdateAssetDto
{
    public string Name { get; set; } = string.Empty;
    public Guid WorkCenterId { get; set; }
    public string? LimbleIdentifier { get; set; }
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
    public List<PlantGearDto> Gears { get; set; } = new();
}

public class SetPlantGearDto
{
    public Guid PlantGearId { get; set; }
}

// ---- Active Sessions ----
public class ActiveSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
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
    public string SiteCode { get; set; } = string.Empty;
}

// ---- Product Types (reference) ----
public class ProductTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ---- Role reference ----
public class RoleOptionDto
{
    public decimal Tier { get; set; }
    public string Name { get; set; } = string.Empty;
}
