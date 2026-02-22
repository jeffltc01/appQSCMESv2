import { api } from './apiClient.ts';
import type {
  LoginConfigResponse,
  LoginRequest,
  LoginResponse,
  TabletSetupRequest,
  CreateProductionRecordRequest,
  CreateProductionRecordResponse,
  CreateInspectionRecordRequest,
  CreateAssemblyRequest,
  CreateAssemblyResponse,
  SerialNumberContextResponse,
  QueueAdvanceResponse,
  KanbanCardLookupResponse,
  CreateMaterialQueueItemRequest,
  CreateFitupQueueItemRequest,
  AddXrayQueueItemRequest,
  CreateRoundSeamSetupRequest,
  CreateRoundSeamRecordRequest,
  CreateNameplateRecordRequest,
  CreateHydroRecordRequest,
  HydroRecordResponse,
  CreateProductRequest,
  UpdateProductRequest,
  CreateUserRequest,
  UpdateUserRequest,
  CreateVendorRequest,
  UpdateVendorRequest,
  CreateDefectCodeRequest,
  UpdateDefectCodeRequest,
  CreateDefectLocationRequest,
  UpdateDefectLocationRequest,
  UpdateWorkCenterConfigRequest,
  UpdateWorkCenterGroupRequest,
  CreateCharacteristicRequest,
  UpdateCharacteristicRequest,
  CreateControlPlanRequest,
  UpdateControlPlanRequest,
  CreateAssetRequest,
  UpdateAssetRequest,
  CreateBarcodeCardRequest,
  SetPlantGearRequest,
  UpdatePlantLimbleRequest,
  CreateActiveSessionRequest,
  CreateWorkCenterProductionLineRequest,
  UpdateWorkCenterProductionLineRequest,
  CreateAnnotationTypeRequest,
  UpdateAnnotationTypeRequest,
  CreateProductionLineRequest,
  UpdateProductionLineRequest,
  UpdateAnnotationRequest,
  CreateAdminAnnotationRequest,
  CreatePlantPrinterRequest,
  UpdatePlantPrinterRequest,
  CreateAIReviewRequest,
  AIReviewResultDto,
  LimbleStatus,
  LimbleTask,
  CreateLimbleWorkRequest,
  IssueRequestDto,
  CreateIssueRequestDto,
  ApproveIssueRequestDto,
  RejectIssueRequestDto,
  CreateLogAnnotationRequest,
  CreateSupervisorAnnotationRequest,
  SupervisorAnnotationResultDto,
  CreateDowntimeReasonCategoryRequest,
  UpdateDowntimeReasonCategoryRequest,
  CreateDowntimeReasonRequest,
  UpdateDowntimeReasonRequest,
  UpdateDowntimeConfigRequest,
  SetDowntimeReasonsRequest,
  CreateDowntimeEventRequest,
  BulkUpsertCapacityTargetsRequest,
} from '../types/api.ts';
import type {
  Plant,
  WorkCenter,
  ProductionLine,
  ProductionLineAdmin,
  Asset,
  Welder,
  WCHistoryData,
  QueueTransaction,
  MaterialQueueItem,
  DefectCode,
  DefectLocation,
  Characteristic,
  InspectionRecord,
  Vendor,
  ProductListItem,
  XrayQueueItem,
  RoundSeamSetup,
  AssemblyLookup,
  NameplateRecordInfo,
  BarcodeCardInfo,
  AdminProduct,
  ProductType,
  AdminUser,
  RoleOption,
  AdminVendor,
  AdminDefectCode,
  AdminDefectLocation,
  AdminWorkCenter,
  AdminWorkCenterGroup,
  AdminCharacteristic,
  AdminControlPlan,
  AdminAsset,
  AdminBarcodeCard,
  PlantWithGear,
  ActiveSession,
  WorkCenterProductionLine,
  AdminAnnotationType,
  AdminAnnotation,
  AdminPlantPrinter,
  SerialNumberLookup,
  SellableTankStatus,
  AIReviewRecord,
  RollsLogEntry,
  FitupLogEntry,
  HydroLogEntry,
  RtXrayLogEntry,
  SpotXrayLogResponse,
  SupervisorDashboardMetrics,
  SupervisorRecord,
  PerformanceTableResponse,
  DowntimeReasonCategory,
  DowntimeReason,
  DowntimeConfig,
  DowntimeEvent,
  DigitalTwinSnapshot,
  ShiftSchedule,
  CreateShiftScheduleRequest,
  CapacityTarget,
  CreateCapacityTargetRequest,
} from '../types/domain.ts';

export const authApi = {
  getLoginConfig: (empNo: string) =>
    api.get<LoginConfigResponse>(`/users/login-config?empNo=${encodeURIComponent(empNo)}`),
  login: (req: LoginRequest) =>
    api.post<LoginResponse>('/auth/login', req),
};

export const siteApi = {
  getSites: () => api.get<Plant[]>('/sites'),
};

export const workCenterApi = {
  getWorkCenters: () =>
    api.get<WorkCenter[]>('/workcenters'),
  getWelders: (wcId: string) =>
    api.get<Welder[]>(`/workcenters/${wcId}/welders`),
  lookupWelder: (wcId: string, empNo: string) =>
    api.get<Welder>(`/workcenters/${wcId}/welders/lookup?empNo=${encodeURIComponent(empNo)}`),
  addWelder: (wcId: string, employeeNumber: string) =>
    api.post<Welder>(`/workcenters/${wcId}/welders`, { employeeNumber }),
  removeWelder: (wcId: string, userId: string) =>
    api.delete<void>(`/workcenters/${wcId}/welders/${userId}`),
  getHistory: (wcId: string, date: string, plantId: string, assetId?: string) =>
    api.get<WCHistoryData>(`/workcenters/${wcId}/history?plantId=${encodeURIComponent(plantId)}&date=${date}&limit=5${assetId ? `&assetId=${assetId}` : ''}`),
  getQueueTransactions: (wcId: string, plantId?: string, limit = 5) =>
    api.get<QueueTransaction[]>(`/workcenters/${wcId}/queue-transactions?limit=${limit}${plantId ? `&plantId=${plantId}` : ''}`),
  getMaterialQueue: (wcId: string, type?: string) =>
    api.get<MaterialQueueItem[]>(
      `/workcenters/${wcId}/material-queue${type ? `?type=${type}` : ''}`,
    ),
  advanceQueue: (wcId: string) =>
    api.post<QueueAdvanceResponse>(`/workcenters/${wcId}/queue/advance`),
  reportFault: (wcId: string, description: string) =>
    api.post<void>(`/workcenters/${wcId}/faults`, { description }),
  getDefectCodes: (wcId: string) =>
    api.get<DefectCode[]>(`/workcenters/${wcId}/defect-codes`),
  getDefectLocations: (wcId: string) =>
    api.get<DefectLocation[]>(`/workcenters/${wcId}/defect-locations`),
  getCharacteristics: (wcId: string, tankSize?: number) =>
    api.get<Characteristic[]>(`/workcenters/${wcId}/characteristics${tankSize != null ? `?tankSize=${tankSize}` : ''}`),
};

export const productionLineApi = {
  getProductionLines: (plantId: string) =>
    api.get<ProductionLine[]>(`/productionlines?plantId=${encodeURIComponent(plantId)}`),
  getAll: () =>
    api.get<ProductionLineAdmin[]>('/productionlines/all'),
};

export const assetApi = {
  getAssets: (workCenterId: string, productionLineId?: string) => {
    const params = new URLSearchParams({ workCenterId });
    if (productionLineId) params.set('productionLineId', productionLineId);
    return api.get<Asset[]>(`/assets?${params}`);
  },
};

export const tabletSetupApi = {
  save: (req: TabletSetupRequest) =>
    api.post<void>('/tablet-setup', req),
};

export const productionRecordApi = {
  create: (req: CreateProductionRecordRequest) =>
    api.post<CreateProductionRecordResponse>('/production-records', req),
};

export const inspectionRecordApi = {
  create: (req: CreateInspectionRecordRequest) =>
    api.post<InspectionRecord>('/inspection-records', req),
};

export const assemblyApi = {
  create: (req: CreateAssemblyRequest) =>
    api.post<CreateAssemblyResponse>('/assemblies', req),
  reassemble: (alphaCode: string, body: unknown) =>
    api.post<CreateAssemblyResponse>(`/assemblies/${alphaCode}/reassemble`, body),
};

export const serialNumberApi = {
  getContext: (serial: string) =>
    api.get<SerialNumberContextResponse>(`/serial-numbers/${encodeURIComponent(serial)}/context`),
  getLookup: (serial: string) =>
    api.get<SerialNumberLookup>(`/serial-numbers/${encodeURIComponent(serial)}/lookup`),
};

export const materialQueueApi = {
  getCardLookup: (cardId: string) =>
    api.get<KanbanCardLookupResponse>(`/material-queue/card/${encodeURIComponent(cardId)}`),
  addItem: (wcId: string, req: CreateMaterialQueueItemRequest) =>
    api.post<MaterialQueueItem>(`/workcenters/${wcId}/material-queue`, req),
  updateItem: (wcId: string, itemId: string, req: Partial<CreateMaterialQueueItemRequest>) =>
    api.put<MaterialQueueItem>(`/workcenters/${wcId}/material-queue/${itemId}`, req),
  deleteItem: (wcId: string, itemId: string) =>
    api.delete<void>(`/workcenters/${wcId}/material-queue/${itemId}`),
  addFitupItem: (wcId: string, req: CreateFitupQueueItemRequest) =>
    api.post<MaterialQueueItem>(`/workcenters/${wcId}/fitup-queue`, req),
  updateFitupItem: (wcId: string, itemId: string, req: Partial<CreateFitupQueueItemRequest>) =>
    api.put<MaterialQueueItem>(`/workcenters/${wcId}/fitup-queue/${itemId}`, req),
  deleteFitupItem: (wcId: string, itemId: string) =>
    api.delete<void>(`/workcenters/${wcId}/fitup-queue/${itemId}`),
};

export const productApi = {
  getProducts: (type?: string, plantId?: string) => {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    if (plantId) params.set('plantId', plantId);
    return api.get<ProductListItem[]>(`/products?${params}`);
  },
};

export const vendorApi = {
  getVendors: (type?: string, plantId?: string) => {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    if (plantId) params.set('plantId', plantId);
    return api.get<Vendor[]>(`/vendors?${params}`);
  },
};

export const xrayQueueApi = {
  getQueue: (wcId: string) =>
    api.get<XrayQueueItem[]>(`/workcenters/${wcId}/xray-queue`),
  addItem: (wcId: string, req: AddXrayQueueItemRequest) =>
    api.post<XrayQueueItem>(`/workcenters/${wcId}/xray-queue`, req),
  removeItem: (wcId: string, itemId: string) =>
    api.delete<void>(`/workcenters/${wcId}/xray-queue/${itemId}`),
};

export const roundSeamApi = {
  getSetup: (wcId: string) =>
    api.get<RoundSeamSetup>(`/workcenters/${wcId}/round-seam-setup`),
  saveSetup: (wcId: string, req: CreateRoundSeamSetupRequest) =>
    api.post<RoundSeamSetup>(`/workcenters/${wcId}/round-seam-setup`, req),
  createRecord: (req: CreateRoundSeamRecordRequest) =>
    api.post<CreateProductionRecordResponse>('/production-records/round-seam', req),
  getAssemblyByShell: (serial: string) =>
    api.get<AssemblyLookup>(`/serial-numbers/${encodeURIComponent(serial)}/assembly`),
};

export const nameplateApi = {
  create: (req: CreateNameplateRecordRequest) =>
    api.post<NameplateRecordInfo>('/nameplate-records', req),
  getBySerial: (serialNumber: string) =>
    api.get<NameplateRecordInfo>(`/nameplate-records/${encodeURIComponent(serialNumber)}`),
  reprint: (id: string) =>
    api.post<NameplateRecordInfo>(`/nameplate-records/${id}/reprint`),
};

export const hydroApi = {
  create: (req: CreateHydroRecordRequest) =>
    api.post<HydroRecordResponse>('/hydro-records', req),
  getLocationsByCharacteristic: (charId: string) =>
    api.get<DefectLocation[]>(`/characteristics/${charId}/locations`),
};

export const barcodeCardApi = {
  getCards: (wcId: string, plantId?: string) => {
    const params = plantId ? `?plantId=${encodeURIComponent(plantId)}` : '';
    return api.get<BarcodeCardInfo[]>(`/workcenters/${wcId}/barcode-cards${params}`);
  },
};

// ---- Admin APIs ----

export const adminProductApi = {
  getAll: () => api.get<AdminProduct[]>('/products/admin'),
  getTypes: () => api.get<ProductType[]>('/products/types'),
  create: (req: CreateProductRequest) => api.post<AdminProduct>('/products', req),
  update: (id: string, req: UpdateProductRequest) => api.put<AdminProduct>(`/products/${id}`, req),
  remove: (id: string) => api.delete<AdminProduct>(`/products/${id}`),
};

export const adminUserApi = {
  getAll: () => api.get<AdminUser[]>('/users/admin'),
  getRoles: () => api.get<RoleOption[]>('/users/roles'),
  create: (req: CreateUserRequest) => api.post<AdminUser>('/users', req),
  update: (id: string, req: UpdateUserRequest) => api.put<AdminUser>(`/users/${id}`, req),
  remove: (id: string) => api.delete<AdminUser>(`/users/${id}`),
};

export const adminVendorApi = {
  getAll: () => api.get<AdminVendor[]>('/vendors/admin'),
  create: (req: CreateVendorRequest) => api.post<AdminVendor>('/vendors', req),
  update: (id: string, req: UpdateVendorRequest) => api.put<AdminVendor>(`/vendors/${id}`, req),
  remove: (id: string) => api.delete<AdminVendor>(`/vendors/${id}`),
};

export const adminDefectCodeApi = {
  getAll: () => api.get<AdminDefectCode[]>('/defect-codes'),
  create: (req: CreateDefectCodeRequest) => api.post<AdminDefectCode>('/defect-codes', req),
  update: (id: string, req: UpdateDefectCodeRequest) => api.put<AdminDefectCode>(`/defect-codes/${id}`, req),
  remove: (id: string) => api.delete<AdminDefectCode>(`/defect-codes/${id}`),
};

export const adminDefectLocationApi = {
  getAll: () => api.get<AdminDefectLocation[]>('/defect-locations'),
  create: (req: CreateDefectLocationRequest) => api.post<AdminDefectLocation>('/defect-locations', req),
  update: (id: string, req: UpdateDefectLocationRequest) => api.put<AdminDefectLocation>(`/defect-locations/${id}`, req),
  remove: (id: string) => api.delete<AdminDefectLocation>(`/defect-locations/${id}`),
};

export const adminWorkCenterApi = {
  getAll: () => api.get<AdminWorkCenter[]>('/workcenters/admin'),
  getGrouped: () => api.get<AdminWorkCenterGroup[]>('/workcenters/admin/grouped'),
  updateConfig: (id: string, req: UpdateWorkCenterConfigRequest) => api.put<AdminWorkCenter>(`/workcenters/${id}/config`, req),
  updateGroup: (groupId: string, req: UpdateWorkCenterGroupRequest) => api.put<AdminWorkCenterGroup>(`/workcenters/admin/group/${groupId}`, req),
  getProductionLineConfigs: (wcId: string) =>
    api.get<WorkCenterProductionLine[]>(`/workcenters/${wcId}/production-lines`),
  getProductionLineConfig: (wcId: string, plId: string) =>
    api.get<WorkCenterProductionLine>(`/workcenters/${wcId}/production-lines/${plId}`),
  createProductionLineConfig: (wcId: string, req: CreateWorkCenterProductionLineRequest) =>
    api.post<WorkCenterProductionLine>(`/workcenters/${wcId}/production-lines`, req),
  updateProductionLineConfig: (wcId: string, plId: string, req: UpdateWorkCenterProductionLineRequest) =>
    api.put<WorkCenterProductionLine>(`/workcenters/${wcId}/production-lines/${plId}`, req),
  deleteProductionLineConfig: (wcId: string, plId: string) =>
    api.delete<void>(`/workcenters/${wcId}/production-lines/${plId}`),
};

export const adminCharacteristicApi = {
  getAll: () => api.get<AdminCharacteristic[]>('/characteristics/admin'),
  create: (req: CreateCharacteristicRequest) => api.post<AdminCharacteristic>('/characteristics', req),
  update: (id: string, req: UpdateCharacteristicRequest) => api.put<AdminCharacteristic>(`/characteristics/${id}`, req),
  remove: (id: string) => api.delete<AdminCharacteristic>(`/characteristics/${id}`),
};

export const adminControlPlanApi = {
  getAll: (siteId?: string) => api.get<AdminControlPlan[]>(`/control-plans${siteId ? `?siteId=${siteId}` : ''}`),
  create: (req: CreateControlPlanRequest) => api.post<AdminControlPlan>('/control-plans', req),
  update: (id: string, req: UpdateControlPlanRequest) => api.put<AdminControlPlan>(`/control-plans/${id}`, req),
  remove: (id: string) => api.delete<AdminControlPlan>(`/control-plans/${id}`),
};

export const adminAssetApi = {
  getAll: () => api.get<AdminAsset[]>('/assets/admin'),
  create: (req: CreateAssetRequest) => api.post<AdminAsset>('/assets', req),
  update: (id: string, req: UpdateAssetRequest) => api.put<AdminAsset>(`/assets/${id}`, req),
};

export const adminKanbanCardApi = {
  getAll: () => api.get<AdminBarcodeCard[]>('/kanban-cards'),
  create: (req: CreateBarcodeCardRequest) => api.post<AdminBarcodeCard>('/kanban-cards', req),
  remove: (id: string) => api.delete<void>(`/kanban-cards/${id}`),
};

export const adminPlantGearApi = {
  getAll: () => api.get<PlantWithGear[]>('/plant-gear'),
  setGear: (plantId: string, req: SetPlantGearRequest) => api.put<void>(`/plant-gear/${plantId}`, req),
  setLimbleLocationId: (plantId: string, req: UpdatePlantLimbleRequest) => api.put<void>(`/plant-gear/${plantId}/limble`, req),
};

export const adminAnnotationTypeApi = {
  getAll: () => api.get<AdminAnnotationType[]>('/annotation-types'),
  create: (req: CreateAnnotationTypeRequest) => api.post<AdminAnnotationType>('/annotation-types', req),
  update: (id: string, req: UpdateAnnotationTypeRequest) => api.put<AdminAnnotationType>(`/annotation-types/${id}`, req),
  remove: (id: string) => api.delete<void>(`/annotation-types/${id}`),
};

export const adminProductionLineApi = {
  getAll: () => api.get<ProductionLineAdmin[]>('/productionlines/all'),
  create: (req: CreateProductionLineRequest) => api.post<ProductionLineAdmin>('/productionlines', req),
  update: (id: string, req: UpdateProductionLineRequest) => api.put<ProductionLineAdmin>(`/productionlines/${id}`, req),
  remove: (id: string) => api.delete<void>(`/productionlines/${id}`),
};

export const adminPlantPrinterApi = {
  getAll: () => api.get<AdminPlantPrinter[]>('/plant-printers'),
  create: (req: CreatePlantPrinterRequest) => api.post<AdminPlantPrinter>('/plant-printers', req),
  update: (id: string, req: UpdatePlantPrinterRequest) => api.put<AdminPlantPrinter>(`/plant-printers/${id}`, req),
  remove: (id: string) => api.delete<void>(`/plant-printers/${id}`),
};

export const adminAnnotationApi = {
  getAll: (siteId?: string) => api.get<AdminAnnotation[]>(`/annotations${siteId ? `?siteId=${siteId}` : ''}`),
  create: (req: CreateAdminAnnotationRequest) => api.post<AdminAnnotation>('/annotations', req),
  update: (id: string, req: UpdateAnnotationRequest) => api.put<AdminAnnotation>(`/annotations/${id}`, req),
};

export const sellableTankStatusApi = {
  getStatus: (siteId: string, date: string) =>
    api.get<SellableTankStatus[]>(`/sellable-tank-status?siteId=${encodeURIComponent(siteId)}&date=${encodeURIComponent(date)}`),
};

export const activeSessionApi = {
  getBySite: (plantId: string) => api.get<ActiveSession[]>(`/active-sessions?plantId=${encodeURIComponent(plantId)}`),
  upsert: (req: CreateActiveSessionRequest) => api.post<void>('/active-sessions', req),
  heartbeat: () => api.put<void>('/active-sessions/heartbeat'),
  endSession: () => api.delete<void>('/active-sessions'),
};

export const issueRequestApi = {
  submit: (req: CreateIssueRequestDto) =>
    api.post<IssueRequestDto>('/issue-requests', req),
  getMine: (userId: string) =>
    api.get<IssueRequestDto[]>(`/issue-requests/mine?userId=${encodeURIComponent(userId)}`),
  getPending: () =>
    api.get<IssueRequestDto[]>('/issue-requests/pending'),
  approve: (id: string, req: ApproveIssueRequestDto) =>
    api.put<IssueRequestDto>(`/issue-requests/${id}/approve`, req),
  reject: (id: string, req: RejectIssueRequestDto) =>
    api.put<IssueRequestDto>(`/issue-requests/${id}/reject`, req),
};

export const aiReviewApi = {
  getRecords: (wcId: string, plantId: string, date: string) =>
    api.get<AIReviewRecord[]>(`/ai-review/${wcId}/records?plantId=${encodeURIComponent(plantId)}&date=${encodeURIComponent(date)}`),
  submitReview: (req: CreateAIReviewRequest) =>
    api.post<AIReviewResultDto>('/ai-review', req),
};

export const limbleApi = {
  getStatuses: () => api.get<LimbleStatus[]>('/limble/statuses'),
  getMyRequests: (empNo: string) =>
    api.get<LimbleTask[]>(`/limble/my-requests?empNo=${encodeURIComponent(empNo)}`),
  createWorkRequest: (req: CreateLimbleWorkRequest) =>
    api.post<LimbleTask>('/limble/work-requests', req),
};

export const logViewerApi = {
  getRollsLog: (siteId: string, startDate: string, endDate: string) =>
    api.get<RollsLogEntry[]>(`/logs/rolls?siteId=${encodeURIComponent(siteId)}&startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`),
  getFitupLog: (siteId: string, startDate: string, endDate: string) =>
    api.get<FitupLogEntry[]>(`/logs/fitup?siteId=${encodeURIComponent(siteId)}&startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`),
  getHydroLog: (siteId: string, startDate: string, endDate: string) =>
    api.get<HydroLogEntry[]>(`/logs/hydro?siteId=${encodeURIComponent(siteId)}&startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`),
  getRtXrayLog: (siteId: string, startDate: string, endDate: string) =>
    api.get<RtXrayLogEntry[]>(`/logs/rt-xray?siteId=${encodeURIComponent(siteId)}&startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`),
  getSpotXrayLog: (siteId: string, startDate: string, endDate: string) =>
    api.get<SpotXrayLogResponse>(`/logs/spot-xray?siteId=${encodeURIComponent(siteId)}&startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`),
  createAnnotation: (req: CreateLogAnnotationRequest) =>
    api.post<AdminAnnotation>('/logs/annotations', req),
};

export const supervisorDashboardApi = {
  getMetrics: (wcId: string, plantId: string, date: string, operatorId?: string) => {
    const params = new URLSearchParams({ plantId, date });
    if (operatorId) params.set('operatorId', operatorId);
    return api.get<SupervisorDashboardMetrics>(`/supervisor-dashboard/${wcId}/metrics?${params}`);
  },
  getRecords: (wcId: string, plantId: string, date: string) =>
    api.get<SupervisorRecord[]>(`/supervisor-dashboard/${wcId}/records?plantId=${encodeURIComponent(plantId)}&date=${encodeURIComponent(date)}`),
  submitAnnotation: (req: CreateSupervisorAnnotationRequest) =>
    api.post<SupervisorAnnotationResultDto>('/supervisor-dashboard/annotate', req),
  getPerformanceTable: (wcId: string, plantId: string, view: string, date: string, operatorId?: string) => {
    const params = new URLSearchParams({ plantId, view, date });
    if (operatorId) params.set('operatorId', operatorId);
    return api.get<PerformanceTableResponse>(`/supervisor-dashboard/${wcId}/performance-table?${params}`);
  },
};

export const downtimeReasonCategoryApi = {
  getAll: (plantId: string) =>
    api.get<DowntimeReasonCategory[]>(`/downtime-reason-categories?plantId=${encodeURIComponent(plantId)}`),
  create: (req: CreateDowntimeReasonCategoryRequest) =>
    api.post<DowntimeReasonCategory>('/downtime-reason-categories', req),
  update: (id: string, req: UpdateDowntimeReasonCategoryRequest) =>
    api.put<DowntimeReasonCategory>(`/downtime-reason-categories/${id}`, req),
  delete: (id: string) =>
    api.delete<void>(`/downtime-reason-categories/${id}`),
};

export const downtimeReasonApi = {
  getAll: (plantId: string) =>
    api.get<DowntimeReason[]>(`/downtime-reasons?plantId=${encodeURIComponent(plantId)}`),
  create: (req: CreateDowntimeReasonRequest) =>
    api.post<DowntimeReason>('/downtime-reasons', req),
  update: (id: string, req: UpdateDowntimeReasonRequest) =>
    api.put<DowntimeReason>(`/downtime-reasons/${id}`, req),
  delete: (id: string) =>
    api.delete<void>(`/downtime-reasons/${id}`),
};

export const downtimeConfigApi = {
  get: (wcId: string, plId: string) =>
    api.get<DowntimeConfig>(`/workcenters/${wcId}/production-lines/${plId}/downtime-config`),
  update: (wcId: string, plId: string, req: UpdateDowntimeConfigRequest) =>
    api.put<DowntimeConfig>(`/workcenters/${wcId}/production-lines/${plId}/downtime-config`, req),
  setReasons: (wcId: string, plId: string, req: SetDowntimeReasonsRequest) =>
    api.put<void>(`/workcenters/${wcId}/production-lines/${plId}/downtime-reasons`, req),
};

export const downtimeEventApi = {
  create: (req: CreateDowntimeEventRequest) =>
    api.post<DowntimeEvent>('/downtime-events', req),
};

export const digitalTwinApi = {
  getSnapshot: (productionLineId: string, plantId: string) =>
    api.get<DigitalTwinSnapshot>(
      `/digital-twin/${productionLineId}/snapshot?plantId=${encodeURIComponent(plantId)}`,
    ),
};

export const shiftScheduleApi = {
  getAll: (plantId: string) =>
    api.get<ShiftSchedule[]>(`/shift-schedules?plantId=${encodeURIComponent(plantId)}`),
  create: (req: CreateShiftScheduleRequest) =>
    api.post<ShiftSchedule>('/shift-schedules', req),
  update: (id: string, req: Partial<CreateShiftScheduleRequest>) =>
    api.put<ShiftSchedule>(`/shift-schedules/${id}`, req),
  delete: (id: string) =>
    api.delete<void>(`/shift-schedules/${id}`),
};

export const capacityTargetApi = {
  getAll: (plantId: string) =>
    api.get<CapacityTarget[]>(`/capacity-targets?plantId=${encodeURIComponent(plantId)}`),
  create: (req: CreateCapacityTargetRequest) =>
    api.post<CapacityTarget>('/capacity-targets', req),
  update: (id: string, req: { targetUnitsPerHour: number }) =>
    api.put<CapacityTarget>(`/capacity-targets/${id}`, req),
  delete: (id: string) =>
    api.delete<void>(`/capacity-targets/${id}`),
  bulkUpsert: (req: BulkUpsertCapacityTargetsRequest) =>
    api.put<CapacityTarget[]>('/capacity-targets/bulk', req),
  getTankSizes: (plantId: string) =>
    api.get<number[]>(`/capacity-targets/tank-sizes?plantId=${encodeURIComponent(plantId)}`),
};
