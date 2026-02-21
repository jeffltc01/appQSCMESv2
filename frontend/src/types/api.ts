export interface LoginConfigResponse {
  requiresPin: boolean;
  defaultSiteId: string;
  allowSiteSelection: boolean;
  isWelder: boolean;
  userName: string;
}

export interface LoginRequest {
  employeeNumber: string;
  pin?: string;
  siteId: string;
  isWelder: boolean;
}

export interface LoginResponse {
  token: string;
  user: {
    id: string;
    employeeNumber: string;
    displayName: string;
    roleTier: number;
    roleName: string;
    defaultSiteId: string;
    isCertifiedWelder: boolean;
    userType: number;
    plantCode: string;
    plantName: string;
    plantTimeZoneId: string;
  };
}

export interface TabletSetupRequest {
  workCenterId: string;
  productionLineId: string;
  assetId: string | null;
}

export interface CreateProductionRecordRequest {
  serialNumber: string;
  workCenterId: string;
  assetId?: string;
  productionLineId: string;
  operatorId: string;
  welderIds: string[];
  inspectionResult?: 'pass' | 'fail';
  shellSize?: string;
  heatNumber?: string;
  coilNumber?: string;
}

export interface CreateProductionRecordResponse {
  id: string;
  serialNumber: string;
  timestamp: string;
  warning?: string;
}

export interface CreateInspectionRecordRequest {
  serialNumber: string;
  workCenterId: string;
  operatorId: string;
  defects: {
    defectCodeId: string;
    characteristicId: string;
    locationId: string;
  }[];
}

export interface CreateAssemblyRequest {
  shells: string[];
  leftHeadLotId: string;
  rightHeadLotId: string;
  tankSize: number;
  workCenterId: string;
  assetId: string;
  productionLineId: string;
  operatorId: string;
  welderIds: string[];
}

export interface CreateAssemblyResponse {
  id: string;
  alphaCode: string;
  timestamp: string;
}

export interface SerialNumberContextResponse {
  serialNumber: string;
  tankSize: number;
  shellSize?: string;
  existingAssembly?: {
    alphaCode: string;
    tankSize: number;
    shells: string[];
    leftHeadInfo?: { heatNumber: string; coilNumber: string; productDescription: string };
    rightHeadInfo?: { heatNumber: string; coilNumber: string; productDescription: string };
  };
}

export interface QueueAdvanceResponse {
  shellSize: string;
  heatNumber: string;
  coilNumber: string;
  quantity: number;
  productDescription: string;
}

export interface KanbanCardLookupResponse {
  heatNumber: string;
  coilNumber: string;
  productDescription: string;
  cardColor?: string;
}

export interface CreateMaterialQueueItemRequest {
  productId: string;
  vendorMillId?: string;
  vendorProcessorId?: string;
  heatNumber: string;
  coilNumber: string;
  lotNumber?: string;
  quantity: number;
}

export interface CreateFitupQueueItemRequest {
  productId: string;
  vendorHeadId: string;
  lotNumber?: string;
  heatNumber?: string;
  coilSlabNumber?: string;
  cardCode: string;
}

export interface AddXrayQueueItemRequest {
  serialNumber: string;
  operatorId: string;
}

export interface CreateRoundSeamSetupRequest {
  tankSize: number;
  rs1WelderId?: string;
  rs2WelderId?: string;
  rs3WelderId?: string;
  rs4WelderId?: string;
}

export interface CreateRoundSeamRecordRequest {
  serialNumber: string;
  workCenterId: string;
  assetId: string;
  productionLineId: string;
  operatorId: string;
}

export interface CreateNameplateRecordRequest {
  serialNumber: string;
  productId: string;
  workCenterId: string;
  operatorId: string;
}

export interface CreateHydroRecordRequest {
  assemblyAlphaCode: string;
  nameplateSerialNumber: string;
  result: string;
  workCenterId: string;
  assetId?: string;
  operatorId: string;
  defects: { defectCodeId: string; characteristicId: string; locationId: string }[];
}

export interface HydroRecordResponse {
  id: string;
  assemblyAlphaCode: string;
  nameplateSerialNumber: string;
  result: string;
  timestamp: string;
}

export interface ApiError {
  message: string;
  code?: string;
}

// ---- Admin request types ----

export interface CreateProductRequest {
  productNumber: string;
  tankSize: number;
  tankType: string;
  sageItemNumber?: string;
  nameplateNumber?: string;
  siteNumbers?: string;
  productTypeId: string;
}

export interface UpdateProductRequest {
  productNumber: string;
  tankSize: number;
  tankType: string;
  sageItemNumber?: string;
  nameplateNumber?: string;
  siteNumbers?: string;
  productTypeId: string;
  isActive: boolean;
}

export interface CreateUserRequest {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  displayName: string;
  roleTier: number;
  roleName: string;
  defaultSiteId: string;
  isCertifiedWelder: boolean;
  requirePinForLogin: boolean;
  pin?: string;
  userType: number;
}

export interface UpdateUserRequest {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  displayName: string;
  roleTier: number;
  roleName: string;
  defaultSiteId: string;
  isCertifiedWelder: boolean;
  requirePinForLogin: boolean;
  pin?: string;
  userType: number;
  isActive: boolean;
}

export interface CreateVendorRequest {
  name: string;
  vendorType: string;
  plantIds?: string;
}

export interface UpdateVendorRequest {
  name: string;
  vendorType: string;
  plantIds?: string;
  isActive: boolean;
}

export interface CreateDefectCodeRequest {
  code: string;
  name: string;
  severity?: string;
  systemType?: string;
  workCenterIds: string[];
}

export interface UpdateDefectCodeRequest {
  code: string;
  name: string;
  severity?: string;
  systemType?: string;
  workCenterIds: string[];
  isActive: boolean;
}

export interface CreateDefectLocationRequest {
  code: string;
  name: string;
  defaultLocationDetail?: string;
  characteristicId?: string;
}

export interface UpdateDefectLocationRequest {
  code: string;
  name: string;
  defaultLocationDetail?: string;
  characteristicId?: string;
  isActive: boolean;
}

export interface UpdateWorkCenterConfigRequest {
  numberOfWelders: number;
  dataEntryType?: string;
  materialQueueForWCId?: string;
}

export interface UpdateWorkCenterGroupRequest {
  baseName: string;
  dataEntryType?: string;
  materialQueueForWCId?: string;
}

export interface CreateWorkCenterProductionLineRequest {
  productionLineId: string;
  displayName: string;
  numberOfWelders: number;
}

export interface UpdateWorkCenterProductionLineRequest {
  displayName: string;
  numberOfWelders: number;
}

export interface UpdateCharacteristicRequest {
  name: string;
  specHigh?: number;
  specLow?: number;
  specTarget?: number;
  productTypeId?: string;
  workCenterIds: string[];
}

export interface UpdateControlPlanRequest {
  isEnabled: boolean;
  resultType: string;
  isGateCheck: boolean;
}

export interface CreateAssetRequest {
  name: string;
  workCenterId: string;
  productionLineId: string;
  limbleIdentifier?: string;
}

export interface UpdateAssetRequest {
  name: string;
  workCenterId: string;
  productionLineId: string;
  limbleIdentifier?: string;
  isActive: boolean;
}

export interface CreateBarcodeCardRequest {
  cardValue: string;
  color?: string;
  description?: string;
}

export interface SetPlantGearRequest {
  plantGearId: string;
}

export interface CreateActiveSessionRequest {
  workCenterId: string;
  productionLineId: string;
  assetId?: string;
  plantId: string;
}

export interface CreateProductionLineRequest {
  name: string;
  plantId: string;
}

export interface UpdateProductionLineRequest {
  name: string;
  plantId: string;
}

export interface CreateAnnotationTypeRequest {
  name: string;
  abbreviation?: string;
  requiresResolution: boolean;
  operatorCanCreate: boolean;
  displayColor?: string;
}

export interface UpdateAnnotationTypeRequest {
  name: string;
  abbreviation?: string;
  requiresResolution: boolean;
  operatorCanCreate: boolean;
  displayColor?: string;
}

export interface UpdateAnnotationRequest {
  flag: boolean;
  notes?: string;
  resolvedNotes?: string;
  resolvedByUserId?: string;
}
