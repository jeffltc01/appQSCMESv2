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
  leftHeadHeatNumber?: string;
  leftHeadCoilNumber?: string;
  rightHeadHeatNumber?: string;
  rightHeadCoilNumber?: string;
  tankSize: number;
  workCenterId: string;
  assetId?: string;
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
  quantityCompleted: number;
  productDescription: string;
}

export interface KanbanCardLookupResponse {
  heatNumber: string;
  coilNumber: string;
  lotNumber?: string;
  productDescription: string;
  cardColor?: string;
  tankSize?: number;
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
  productionLineId: string;
  operatorId: string;
}

export interface CreateHydroRecordRequest {
  assemblyAlphaCode: string;
  nameplateSerialNumber: string;
  result: string;
  workCenterId: string;
  productionLineId: string;
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
  downtimeTrackingEnabled: boolean;
  downtimeThresholdMinutes: number;
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

export interface UpdatePlantLimbleRequest {
  limbleLocationId?: string;
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

export interface CreatePlantPrinterRequest {
  plantId: string;
  printerName: string;
  enabled: boolean;
  printLocation: string;
}

export interface UpdatePlantPrinterRequest {
  printerName: string;
  enabled: boolean;
  printLocation: string;
}

export interface UpdateAnnotationRequest {
  flag: boolean;
  notes?: string;
  resolvedNotes?: string;
  resolvedByUserId?: string;
}

export interface CreateAdminAnnotationRequest {
  annotationTypeId: string;
  notes?: string;
  initiatedByUserId: string;
  linkedEntityType?: string;
  linkedEntityId?: string;
}

// ---- Issue Requests (GitHub) ----

export enum IssueRequestType {
  Bug = 0,
  FeatureRequest = 1,
  GeneralQuestion = 2,
}

export enum IssueRequestStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
}

export interface IssueRequestDto {
  id: string;
  type: number;
  status: number;
  title: string;
  area: string;
  bodyJson: string;
  submittedByUserId: string;
  submittedByName: string;
  submittedAt: string;
  reviewedByUserId?: string;
  reviewedByName?: string;
  reviewedAt?: string;
  reviewerNotes?: string;
  gitHubIssueNumber?: number;
  gitHubIssueUrl?: string;
}

export interface CreateIssueRequestDto {
  type: number;
  title: string;
  area: string;
  bodyJson: string;
  submittedByUserId: string;
  submitterRoleTier: number;
}

export interface ApproveIssueRequestDto {
  reviewerUserId: string;
  title?: string;
  area?: string;
  bodyJson?: string;
}

export interface RejectIssueRequestDto {
  reviewerUserId: string;
  notes?: string;
}

// ---- AI Review ----
export interface CreateAIReviewRequest {
  productionRecordIds: string[];
  comment?: string;
}

export interface AIReviewResultDto {
  annotationsCreated: number;
}

// ---- Log Viewer ----
export interface CreateLogAnnotationRequest {
  productionRecordId: string;
  annotationTypeId: string;
  notes?: string;
  initiatedByUserId: string;
}

// ---- Limble CMMS ----
export interface LimbleStatus {
  id: number;
  name: string;
}

export interface LimbleTask {
  id: number;
  name: string;
  description?: string;
  priority?: number;
  statusId?: number;
  statusName?: string;
  dueDate?: number;
  createdDate?: number;
  meta1?: string;
}

export interface CreateLimbleWorkRequest {
  subject: string;
  description: string;
  priority: number;
  requestedDueDate?: number;
}

// ---- Supervisor Dashboard ----
export interface CreateSupervisorAnnotationRequest {
  recordIds: string[];
  annotationTypeId: string;
  comment?: string;
}

export interface SupervisorAnnotationResultDto {
  annotationsCreated: number;
}

// ---- Capacity Targets (Bulk) ----

export interface BulkCapacityTargetItem {
  workCenterProductionLineId: string;
  plantGearId: string;
  tankSize: number | null;
  targetUnitsPerHour: number;
}

export interface BulkUpsertCapacityTargetsRequest {
  productionLineId: string;
  targets: BulkCapacityTargetItem[];
}

// ---- Downtime ----
export interface CreateDowntimeReasonCategoryRequest {
  plantId: string;
  name: string;
  sortOrder: number;
}

export interface UpdateDowntimeReasonCategoryRequest {
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateDowntimeReasonRequest {
  downtimeReasonCategoryId: string;
  name: string;
  sortOrder: number;
}

export interface UpdateDowntimeReasonRequest {
  name: string;
  sortOrder: number;
  isActive: boolean;
}

export interface UpdateDowntimeConfigRequest {
  downtimeTrackingEnabled: boolean;
  downtimeThresholdMinutes: number;
}

export interface SetDowntimeReasonsRequest {
  reasonIds: string[];
}

export interface CreateDowntimeEventRequest {
  workCenterProductionLineId: string;
  operatorUserId: string;
  startedAt: string;
  endedAt: string;
  downtimeReasonId?: string;
  isAutoGenerated: boolean;
}
