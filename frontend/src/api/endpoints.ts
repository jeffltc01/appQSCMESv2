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
};
