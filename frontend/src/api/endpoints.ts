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
  UpdateCharacteristicRequest,
  UpdateControlPlanRequest,
  CreateAssetRequest,
  UpdateAssetRequest,
  CreateBarcodeCardRequest,
  SetPlantGearRequest,
  CreateActiveSessionRequest,
} from '../types/api.ts';
import type {
  Plant,
  WorkCenter,
  ProductionLine,
  Asset,
  Welder,
  WCHistoryData,
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
  AdminCharacteristic,
  AdminControlPlan,
  AdminAsset,
  AdminBarcodeCard,
  PlantWithGear,
  ActiveSession,
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
  getWorkCenters: (siteCode: string) =>
    api.get<WorkCenter[]>(`/workcenters?siteCode=${encodeURIComponent(siteCode)}`),
  getWelders: (wcId: string) =>
    api.get<Welder[]>(`/workcenters/${wcId}/welders`),
  addWelder: (wcId: string, employeeNumber: string) =>
    api.post<Welder>(`/workcenters/${wcId}/welders`, { employeeNumber }),
  removeWelder: (wcId: string, userId: string) =>
    api.delete<void>(`/workcenters/${wcId}/welders/${userId}`),
  getHistory: (wcId: string, date: string) =>
    api.get<WCHistoryData>(`/workcenters/${wcId}/history?date=${date}&limit=5`),
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
  getCharacteristics: (wcId: string) =>
    api.get<Characteristic[]>(`/workcenters/${wcId}/characteristics`),
};

export const productionLineApi = {
  getProductionLines: (siteCode: string) =>
    api.get<ProductionLine[]>(`/productionlines?siteCode=${encodeURIComponent(siteCode)}`),
};

export const assetApi = {
  getAssets: (workCenterId: string) =>
    api.get<Asset[]>(`/assets?workCenterId=${encodeURIComponent(workCenterId)}`),
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
  getProducts: (type?: string, siteCode?: string) => {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    if (siteCode) params.set('siteCode', siteCode);
    return api.get<ProductListItem[]>(`/products?${params}`);
  },
};

export const vendorApi = {
  getVendors: (type?: string, siteCode?: string) => {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    if (siteCode) params.set('siteCode', siteCode);
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
    api.post<void>(`/nameplate-records/${id}/reprint`),
};

export const hydroApi = {
  create: (req: CreateHydroRecordRequest) =>
    api.post<HydroRecordResponse>('/hydro-records', req),
  getLocationsByCharacteristic: (charId: string) =>
    api.get<DefectLocation[]>(`/characteristics/${charId}/locations`),
};

export const barcodeCardApi = {
  getCards: (wcId: string, siteCode?: string) => {
    const params = siteCode ? `?siteCode=${encodeURIComponent(siteCode)}` : '';
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
  remove: (id: string) => api.delete<void>(`/defect-codes/${id}`),
};

export const adminDefectLocationApi = {
  getAll: () => api.get<AdminDefectLocation[]>('/defect-locations'),
  create: (req: CreateDefectLocationRequest) => api.post<AdminDefectLocation>('/defect-locations', req),
  update: (id: string, req: UpdateDefectLocationRequest) => api.put<AdminDefectLocation>(`/defect-locations/${id}`, req),
  remove: (id: string) => api.delete<void>(`/defect-locations/${id}`),
};

export const adminWorkCenterApi = {
  getAll: () => api.get<AdminWorkCenter[]>('/workcenters/admin'),
  updateConfig: (id: string, req: UpdateWorkCenterConfigRequest) => api.put<AdminWorkCenter>(`/workcenters/${id}/config`, req),
};

export const adminCharacteristicApi = {
  getAll: () => api.get<AdminCharacteristic[]>('/characteristics/admin'),
  update: (id: string, req: UpdateCharacteristicRequest) => api.put<AdminCharacteristic>(`/characteristics/${id}`, req),
};

export const adminControlPlanApi = {
  getAll: () => api.get<AdminControlPlan[]>('/control-plans'),
  update: (id: string, req: UpdateControlPlanRequest) => api.put<AdminControlPlan>(`/control-plans/${id}`, req),
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
};

export const activeSessionApi = {
  getBySite: (siteCode: string) => api.get<ActiveSession[]>(`/active-sessions?siteCode=${encodeURIComponent(siteCode)}`),
  upsert: (req: CreateActiveSessionRequest) => api.post<void>('/active-sessions', req),
  heartbeat: () => api.put<void>('/active-sessions/heartbeat'),
  endSession: () => api.delete<void>('/active-sessions'),
};
