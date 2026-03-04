import { api } from './apiClient.ts';
import type {
  CoverageSummary,
  LoginConfigResponse,
  LoginRequest,
  LoginResponse,
  TabletSetupRequest,
  CreateProductionRecordRequest,
  CreateProductionRecordResponse,
  CreateInspectionRecordRequest,
  CreateAssemblyRequest,
  CreateAssemblyResponse,
  ReassemblyRequest,
  ReassembleResponse,
  SerialNumberContextResponse,
  QueueAdvanceResponse,
  KanbanCardLookupResponse,
  CreateMaterialQueueItemRequest,
  CreateFitupQueueItemRequest,
  AddXrayQueueItemRequest,
  CreateRoundSeamSetupRequest,
  CreateRoundSeamRecordRequest,
  CreateNameplateRecordRequest,
  UpdateNameplateRecordRequest,
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
  CreateWorkCenterRequest,
  CreateCharacteristicRequest,
  UpdateCharacteristicRequest,
  CreateControlPlanRequest,
  UpdateControlPlanRequest,
  CreateAssetRequest,
  UpdateAssetRequest,
  CreateBarcodeCardRequest,
  SetPlantGearRequest,
  UpdatePlantLimbleRequest,
  UpdatePlantNextAlphaCodeRequest,
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
  UpdateDowntimeEventRequest,
  BulkUpsertCapacityTargetsRequest,
  NaturalLanguageQueryRequest,
  FrontendTelemetryIngestRequest,
  FrontendTelemetryArchiveRequest,
  DemoShellCurrentResponse,
  DemoShellAdvanceRequest,
  DemoDataResetSeedResponse,
  DemoDataRefreshDatesResponse,
  UpsertChecklistTemplateRequest,
  UpsertScoreTypeRequest,
  ResolveChecklistTemplateRequest,
  CreateChecklistEntryRequest,
  SubmitChecklistResponsesRequest,
  GetChecklistReviewSummaryRequest,
  GetChecklistQuestionResponsesRequest,
  UpsertWorkflowDefinitionRequest,
  StartWorkflowRequest,
  AdvanceStepRequest,
  ApproveRejectRequest,
  CompleteWorkItemRequest,
  CreateHoldTagRequest,
  SetHoldTagDispositionRequest,
  LinkHoldTagNcrRequest,
  ResolveHoldTagRequest,
  VoidHoldTagRequest,
  UpsertNcrTypeRequest,
  CreateNcrRequest,
  UpdateNcrDataRequest,
  SubmitNcrStepRequest,
  NcrDecisionRequest,
  VoidNcrRequest,
  AddNcrAttachmentRequest,
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
  WorkCenterType,
  AdminCharacteristic,
  AdminControlPlan,
  OperatorControlPlan,
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
  WhereUsedResult,
  AIReviewRecord,
  RollsLogEntry,
  FitupLogEntry,
  HydroLogEntry,
  RtXrayLogEntry,
  SpotXrayLogResponse,
  SpotXrayLaneQueues,
  SpotXrayIncrementSummary,
  SpotXrayIncrementDetail,
  SupervisorDashboardMetrics,
  SupervisorDashboardTrends,
  DefectParetoResponse,
  DowntimeParetoResponse,
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
  AuditLogPage,
  NaturalLanguageQueryResponse,
  FrontendTelemetryPage,
  FrontendTelemetryFilterOptions,
  FrontendTelemetryCount,
  FrontendTelemetryArchiveResult,
  DemoShellCurrent,
  DemoDataResetSeedResult,
  DemoDataRefreshDatesResult,
  ChecklistTemplate,
  ScoreType,
  ChecklistEntry,
  ChecklistReviewSummary,
  ChecklistQuestionResponses,
  WorkflowDefinition,
  WorkflowInstance,
  WorkflowWorkItem,
  WorkflowEvent,
  NotificationRule,
  HoldTag,
  NcrType,
  Ncr,
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
  lookupWelder: (wcId: string, empNo: string) =>
    api.get<Welder>(`/workcenters/${wcId}/welders/lookup?empNo=${encodeURIComponent(empNo)}`),
  getHistory: (wcId: string, date: string, plantId: string, productionLineId: string, assetId?: string) =>
    api.get<WCHistoryData>(`/workcenters/${wcId}/history?plantId=${encodeURIComponent(plantId)}&productionLineId=${encodeURIComponent(productionLineId)}&date=${date}&limit=5${assetId ? `&assetId=${assetId}` : ''}`),
  getQueueTransactions: (wcId: string, productionLineId: string, plantId?: string, limit = 5, action?: string) =>
    api.get<QueueTransaction[]>(
      `/workcenters/${wcId}/queue-transactions?productionLineId=${encodeURIComponent(productionLineId)}&limit=${limit}${plantId ? `&plantId=${plantId}` : ''}${action ? `&action=${action}` : ''}`,
    ),
  getMaterialQueue: (wcId: string, type?: string, productionLineId?: string) => {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    if (productionLineId) params.set('productionLineId', productionLineId);
    const qs = params.toString();
    return api.get<MaterialQueueItem[]>(`/workcenters/${wcId}/material-queue${qs ? `?${qs}` : ''}`);
  },
  advanceQueue: (wcId: string, productionLineId?: string) =>
    api.post<QueueAdvanceResponse>(
      `/workcenters/${wcId}/queue/advance${productionLineId ? `?productionLineId=${encodeURIComponent(productionLineId)}` : ''}`,
    ),
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

export const controlPlanApi = {
  getForWorkCenter: (workCenterId: string, productionLineId: string) =>
    api.get<OperatorControlPlan[]>(
      `/control-plans/by-work-center?workCenterId=${encodeURIComponent(workCenterId)}&productionLineId=${encodeURIComponent(productionLineId)}`
    ),
};

export const checklistApi = {
  getScoreTypes: (includeArchived = false) =>
    api.get<ScoreType[]>(`/checklists/score-types?includeArchived=${includeArchived ? 'true' : 'false'}`),
  getScoreType: (scoreTypeId: string) =>
    api.get<ScoreType>(`/checklists/score-types/${scoreTypeId}`),
  upsertScoreType: (request: UpsertScoreTypeRequest) =>
    api.post<ScoreType>('/checklists/score-types', request),
  getTemplates: (siteId?: string, checklistType?: string) => {
    const params = new URLSearchParams();
    if (siteId) params.set('siteId', siteId);
    if (checklistType) params.set('checklistType', checklistType);
    const qs = params.toString();
    return api.get<ChecklistTemplate[]>(`/checklists/templates${qs ? `?${qs}` : ''}`);
  },
  getTemplate: (templateId: string) =>
    api.get<ChecklistTemplate>(`/checklists/templates/${templateId}`),
  upsertTemplate: (request: UpsertChecklistTemplateRequest) =>
    api.post<ChecklistTemplate>('/checklists/templates', request),
  resolveTemplate: (request: ResolveChecklistTemplateRequest) =>
    api.post<ChecklistTemplate>('/checklists/templates/resolve', request),
  createEntry: (request: CreateChecklistEntryRequest) =>
    api.post<ChecklistEntry>('/checklists/entries', request),
  submitResponses: (entryId: string, request: SubmitChecklistResponsesRequest) =>
    api.put<ChecklistEntry>(`/checklists/entries/${entryId}/responses`, request),
  completeEntry: (entryId: string) =>
    api.put<ChecklistEntry>(`/checklists/entries/${entryId}/complete`),
  getEntryHistory: (siteId: string, workCenterId?: string, checklistType?: string) => {
    const params = new URLSearchParams({ siteId });
    if (workCenterId) params.set('workCenterId', workCenterId);
    if (checklistType) params.set('checklistType', checklistType);
    return api.get<ChecklistEntry[]>(`/checklists/entries?${params.toString()}`);
  },
  getEntry: (entryId: string) =>
    api.get<ChecklistEntry>(`/checklists/entries/${entryId}`),
  getReviewSummary: (request: GetChecklistReviewSummaryRequest) => {
    const params = new URLSearchParams({
      siteId: request.siteId,
      fromUtc: request.fromUtc,
      toUtc: request.toUtc,
    });
    if (request.checklistType) params.set('checklistType', request.checklistType);
    return api.get<ChecklistReviewSummary>(`/checklists/review/summary?${params.toString()}`);
  },
  getQuestionResponses: (request: GetChecklistQuestionResponsesRequest) => {
    const params = new URLSearchParams({
      siteId: request.siteId,
      fromUtc: request.fromUtc,
      toUtc: request.toUtc,
      checklistTemplateItemId: request.checklistTemplateItemId,
    });
    if (request.checklistType) params.set('checklistType', request.checklistType);
    return api.get<ChecklistQuestionResponses>(`/checklists/review/question-responses?${params.toString()}`);
  },
};

export const assemblyApi = {
  create: (req: CreateAssemblyRequest) =>
    api.post<CreateAssemblyResponse>('/assemblies', req),
  reassemble: (alphaCode: string, body: ReassemblyRequest) =>
    api.post<ReassembleResponse>(`/assemblies/${alphaCode}/reassemble`, body),
};

export const serialNumberApi = {
  getContext: (serial: string) =>
    api.get<SerialNumberContextResponse>(`/serial-numbers/${encodeURIComponent(serial)}/context`),
  getLookup: (serial: string) =>
    api.get<SerialNumberLookup>(`/serial-numbers/${encodeURIComponent(serial)}/lookup`),
};

export const materialQueueApi = {
  getCardLookup: (workCenterId: string, productionLineId: string, cardId: string) =>
    api.get<KanbanCardLookupResponse>(
      `/material-queue/card/${encodeURIComponent(cardId)}?workCenterId=${encodeURIComponent(workCenterId)}&productionLineId=${encodeURIComponent(productionLineId)}`,
    ),
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
  update: (id: string, req: UpdateNameplateRecordRequest) =>
    api.put<NameplateRecordInfo>(`/nameplate-records/${id}`, req),
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

export const demoShellApi = {
  getCurrent: (workCenterId: string) =>
    api.get<DemoShellCurrentResponse>(`/demo-shell-flow/current?workCenterId=${encodeURIComponent(workCenterId)}`) as Promise<DemoShellCurrent>,
  advance: (req: DemoShellAdvanceRequest) =>
    api.post<DemoShellCurrentResponse>('/demo-shell-flow/advance', req) as Promise<DemoShellCurrent>,
};

export const demoDataAdminApi = {
  resetSeed: () =>
    api.post<DemoDataResetSeedResponse>('/admin/demo-data/reset-seed', {}) as Promise<DemoDataResetSeedResult>,
  refreshDates: () =>
    api.post<DemoDataRefreshDatesResponse>('/admin/demo-data/refresh-dates', {}) as Promise<DemoDataRefreshDatesResult>,
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
  getAll: (siteId?: string, roleTiers?: number[]) => {
    const params = new URLSearchParams();
    if (siteId) params.set('siteId', siteId);
    for (const tier of roleTiers ?? []) {
      params.append('roleTiers', tier.toString());
    }
    const qs = params.toString();
    return api.get<AdminUser[]>(`/users/admin${qs ? `?${qs}` : ''}`);
  },
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
  getTypes: () => api.get<WorkCenterType[]>('/workcenters/admin/types'),
  create: (req: CreateWorkCenterRequest) => api.post<AdminWorkCenterGroup>('/workcenters/admin', req),
  updateConfig: (id: string, req: UpdateWorkCenterConfigRequest) => api.put<AdminWorkCenter>(`/workcenters/${id}/config`, req),
  updateGroup: (groupId: string, req: UpdateWorkCenterGroupRequest) => api.put<AdminWorkCenterGroup>(`/workcenters/admin/group/${groupId}`, req),
  getProductionLineConfigs: (wcId: string, plantId?: string) =>
    api.get<WorkCenterProductionLine[]>(`/workcenters/${wcId}/production-lines${plantId ? `?plantId=${encodeURIComponent(plantId)}` : ''}`),
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
  getAll: (siteId?: string) => api.get<AdminAsset[]>(`/assets/admin${siteId ? `?siteId=${siteId}` : ''}`),
  create: (req: CreateAssetRequest) => api.post<AdminAsset>('/assets', req),
  update: (id: string, req: UpdateAssetRequest) => api.put<AdminAsset>(`/assets/${id}`, req),
  remove: (id: string) => api.delete<void>(`/assets/${id}`),
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
  setNextAlphaCode: (plantId: string, req: UpdatePlantNextAlphaCodeRequest) => api.put<void>(`/plant-gear/${plantId}/next-alpha-code`, req),
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

export const whereUsedApi = {
  search: (params: {
    heatNumber?: string;
    coilNumber?: string;
    lotNumber?: string;
    siteId?: string;
  }) => {
    const query = new URLSearchParams();
    if (params.heatNumber?.trim()) query.set('heatNumber', params.heatNumber.trim());
    if (params.coilNumber?.trim()) query.set('coilNumber', params.coilNumber.trim());
    if (params.lotNumber?.trim()) query.set('lotNumber', params.lotNumber.trim());
    if (params.siteId?.trim()) query.set('siteId', params.siteId.trim());
    return api.get<WhereUsedResult[]>(`/where-used?${query.toString()}`);
  },
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

export const nlqApi = {
  ask: (req: NaturalLanguageQueryRequest) =>
    api.post<NaturalLanguageQueryResponse>('/nlq/ask', req),
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
  getTrends: (wcId: string, plantId: string, date: string, operatorId?: string, days = 30) => {
    const params = new URLSearchParams({ plantId, date, days: String(days) });
    if (operatorId) params.set('operatorId', operatorId);
    return api.get<SupervisorDashboardTrends>(`/supervisor-dashboard/${wcId}/trends?${params}`);
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
  getDefectPareto: (wcId: string, plantId: string, view: string, date: string, operatorId?: string) => {
    const params = new URLSearchParams({ plantId, view, date });
    if (operatorId) params.set('operatorId', operatorId);
    return api.get<DefectParetoResponse>(`/defect-analytics/${wcId}/pareto?${params}`);
  },
  getDowntimePareto: (wcId: string, plantId: string, view: string, date: string, operatorId?: string) => {
    const params = new URLSearchParams({ plantId, view, date });
    if (operatorId) params.set('operatorId', operatorId);
    return api.get<DowntimeParetoResponse>(`/defect-analytics/${wcId}/downtime-pareto?${params}`);
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
  getAll: (workCenterId: string, from?: string, to?: string) => {
    const params = new URLSearchParams({ workCenterId });
    if (from) params.append('from', from);
    if (to) params.append('to', to);
    return api.get<DowntimeEvent[]>(`/downtime-events?${params.toString()}`);
  },
  create: (req: CreateDowntimeEventRequest) =>
    api.post<DowntimeEvent>('/downtime-events', req),
  update: (id: string, req: UpdateDowntimeEventRequest) =>
    api.put<DowntimeEvent>(`/downtime-events/${id}`, req),
  delete: (id: string) =>
    api.delete<void>(`/downtime-events/${id}`),
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

export const spotXrayApi = {
  getLaneQueues: (siteId: string) =>
    api.get<SpotXrayLaneQueues>(`/spot-xray/lanes?siteId=${encodeURIComponent(siteId)}`),
  createIncrements: (req: {
    workCenterId: string;
    productionLineId: string;
    operatorId: string;
    siteId: string;
    laneSelections: { laneName: string; selectedPositions: number[] }[];
  }) =>
    api.post<{ increments: SpotXrayIncrementSummary[] }>('/spot-xray/increments', req),
  getIncrement: (id: string) =>
    api.get<SpotXrayIncrementDetail>(`/spot-xray/increments/${id}`),
  getRecentIncrements: (siteId: string) =>
    api.get<SpotXrayIncrementSummary[]>(`/spot-xray/increments/recent?siteId=${encodeURIComponent(siteId)}`),
  getDraftIncrements: (siteId: string) =>
    api.get<SpotXrayIncrementSummary[]>(`/spot-xray/increments/drafts?siteId=${encodeURIComponent(siteId)}`),
  saveResults: (id: string, req: {
    inspectTankId?: string;
    isDraft: boolean;
    operatorId: string;
    seams: {
      seamNumber: number;
      shotNo?: string;
      result?: string;
      trace1ShotNo?: string;
      trace1TankId?: string;
      trace1Result?: string;
      trace2ShotNo?: string;
      trace2TankId?: string;
      trace2Result?: string;
      finalShotNo?: string;
      finalResult?: string;
    }[];
  }) =>
    api.put<SpotXrayIncrementDetail>(`/spot-xray/increments/${id}`, req),
  getNextShotNumber: (plantId: string) =>
    api.post<{ shotNumber: number }>('/spot-xray/shot-number', { plantId }),
};

export const coverageApi = {
  getSummary: () => api.get<CoverageSummary>('/coverage/summary'),
  getReportHtml: (layer: 'frontend' | 'backend') =>
    api.getText(`/coverage/${layer}/index.html`),
};

export const auditLogApi = {
  getLogs: (params: {
    entityName?: string;
    entityId?: string;
    action?: string;
    userId?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const qs = new URLSearchParams();
    if (params.entityName) qs.set('entityName', params.entityName);
    if (params.entityId) qs.set('entityId', params.entityId);
    if (params.action) qs.set('action', params.action);
    if (params.userId) qs.set('userId', params.userId);
    if (params.from) qs.set('from', params.from);
    if (params.to) qs.set('to', params.to);
    if (params.page) qs.set('page', String(params.page));
    if (params.pageSize) qs.set('pageSize', String(params.pageSize));
    return api.get<AuditLogPage>(`/audit-logs?${qs.toString()}`);
  },
  getEntityNames: () =>
    api.get<string[]>('/audit-logs/entity-names'),
};

export const frontendTelemetryApi = {
  ingest: (req: FrontendTelemetryIngestRequest) =>
    api.post<void>('/frontend-telemetry', req),
  getEvents: (params: {
    category?: string;
    source?: string;
    severity?: string;
    userId?: string;
    workCenterId?: string;
    from?: string;
    to?: string;
    reactRuntimeOnly?: boolean;
    page?: number;
    pageSize?: number;
  }) => {
    const qs = new URLSearchParams();
    if (params.category) qs.set('category', params.category);
    if (params.source) qs.set('source', params.source);
    if (params.severity) qs.set('severity', params.severity);
    if (params.userId) qs.set('userId', params.userId);
    if (params.workCenterId) qs.set('workCenterId', params.workCenterId);
    if (params.from) qs.set('from', params.from);
    if (params.to) qs.set('to', params.to);
    if (params.reactRuntimeOnly) qs.set('reactRuntimeOnly', 'true');
    if (params.page) qs.set('page', String(params.page));
    if (params.pageSize) qs.set('pageSize', String(params.pageSize));
    return api.get<FrontendTelemetryPage>(`/frontend-telemetry?${qs.toString()}`);
  },
  getFilters: () =>
    api.get<FrontendTelemetryFilterOptions>('/frontend-telemetry/filters'),
  getCount: (warningThreshold = 250000) =>
    api.get<FrontendTelemetryCount>(`/frontend-telemetry/count?warningThreshold=${warningThreshold}`),
  archiveOldest: (req: FrontendTelemetryArchiveRequest) =>
    api.post<FrontendTelemetryArchiveResult>('/frontend-telemetry/archive', req),
};

export const workflowApi = {
  getDefinitions: (workflowType?: string) =>
    api.get<WorkflowDefinition[]>(`/workflows/definitions${workflowType ? `?workflowType=${encodeURIComponent(workflowType)}` : ''}`),
  upsertDefinition: (req: UpsertWorkflowDefinitionRequest) =>
    api.post<WorkflowDefinition>('/workflows/definitions', req),
  validateDefinition: (req: UpsertWorkflowDefinitionRequest) =>
    api.post<{ isExecutable: boolean; errors: string[] }>('/workflows/definitions/validate', req),
  getNotificationRules: (workflowType?: string) =>
    api.get<NotificationRule[]>(`/workflows/notification-rules${workflowType ? `?workflowType=${encodeURIComponent(workflowType)}` : ''}`),
  upsertNotificationRule: (req: NotificationRule) =>
    api.post<NotificationRule>('/workflows/notification-rules', req),
  start: (req: StartWorkflowRequest) =>
    api.post<WorkflowInstance>('/workflows/start', req),
  advance: (req: AdvanceStepRequest) =>
    api.post<WorkflowInstance>('/workflows/advance', req),
  approve: (req: ApproveRejectRequest) =>
    api.post<WorkflowInstance>('/workflows/approve', req),
  reject: (req: ApproveRejectRequest) =>
    api.post<WorkflowInstance>('/workflows/reject', req),
  getOpenWorkItems: (userId: string, roleTiers: number[]) => {
    const params = new URLSearchParams({ userId });
    for (const roleTier of roleTiers) {
      params.append('roleTiers', String(roleTier));
    }
    return api.get<WorkflowWorkItem[]>(`/workflows/work-items/open?${params.toString()}`);
  },
  completeWorkItem: (req: CompleteWorkItemRequest) =>
    api.post<WorkflowInstance>('/workflows/work-items/complete', req),
  getEvents: (workflowInstanceId: string) =>
    api.get<WorkflowEvent[]>(`/workflows/${workflowInstanceId}/events`),
};

export const holdTagApi = {
  getList: (siteCode?: string) =>
    api.get<HoldTag[]>(`/hold-tags${siteCode ? `?siteCode=${encodeURIComponent(siteCode)}` : ''}`),
  getById: (id: string) =>
    api.get<HoldTag>(`/hold-tags/${id}`),
  create: (req: CreateHoldTagRequest) =>
    api.post<HoldTag>('/hold-tags', req),
  setDisposition: (req: SetHoldTagDispositionRequest) =>
    api.post<HoldTag>('/hold-tags/disposition', req),
  linkNcr: (req: LinkHoldTagNcrRequest) =>
    api.post<HoldTag>('/hold-tags/link-ncr', req),
  resolve: (req: ResolveHoldTagRequest) =>
    api.post<HoldTag>('/hold-tags/resolve', req),
  void: (req: VoidHoldTagRequest) =>
    api.post<HoldTag>('/hold-tags/void', req),
  getEvents: (id: string) =>
    api.get<WorkflowEvent[]>(`/hold-tags/${id}/events`),
};

export const ncrApi = {
  getNcrTypes: (includeInactive = false) =>
    api.get<NcrType[]>(`/ncr-types?includeInactive=${includeInactive ? 'true' : 'false'}`),
  upsertNcrType: (req: UpsertNcrTypeRequest) =>
    api.post<NcrType>('/ncr-types', req),
  getList: (siteCode?: string) =>
    api.get<Ncr[]>(`/ncr${siteCode ? `?siteCode=${encodeURIComponent(siteCode)}` : ''}`),
  getById: (id: string) =>
    api.get<Ncr>(`/ncr/${id}`),
  create: (req: CreateNcrRequest) =>
    api.post<Ncr>('/ncr', req),
  updateData: (req: UpdateNcrDataRequest) =>
    api.put<Ncr>('/ncr/data', req),
  submitStep: (req: SubmitNcrStepRequest) =>
    api.post<Ncr>('/ncr/submit', req),
  approveStep: (req: NcrDecisionRequest) =>
    api.post<Ncr>('/ncr/approve', req),
  rejectStep: (req: NcrDecisionRequest) =>
    api.post<Ncr>('/ncr/reject', req),
  voidNcr: (req: VoidNcrRequest) =>
    api.post<Ncr>('/ncr/void', req),
  addAttachment: (req: AddNcrAttachmentRequest) =>
    api.post<void>('/ncr/attachments', req),
  getEvents: (id: string) =>
    api.get<WorkflowEvent[]>(`/ncr/${id}/events`),
};
