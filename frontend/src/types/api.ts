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
    plantCode: string;
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
