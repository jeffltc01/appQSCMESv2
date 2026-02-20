export interface Plant {
  id: string;
  code: string;
  name: string;
}

export interface ProductionLine {
  id: string;
  name: string;
  plantId: string;
}

export interface WorkCenter {
  id: string;
  name: string;
  plantId: string;
  workCenterTypeId: string;
  workCenterTypeName: string;
  requiresWelder: boolean;
  productionLineId?: string;
  dataEntryType?: string;
  materialQueueForWCId?: string;
}

export interface Asset {
  id: string;
  name: string;
  workCenterId: string;
}

export interface User {
  id: string;
  employeeNumber: string;
  displayName: string;
  firstName: string;
  lastName: string;
  roleTier: number;
  roleName: string;
  defaultSiteId: string;
  isCertifiedWelder: boolean;
  requirePinForLogin: boolean;
}

export interface Welder {
  userId: string;
  displayName: string;
  employeeNumber: string;
}

export interface Product {
  id: string;
  productNumber: string;
  tankSize: number;
  tankType: string;
  productTypeId: string;
}

export interface SerialNumber {
  id: string;
  serial: string;
  productId?: string;
  tankSize?: number;
  shellSize?: string;
}

export interface ProductionRecord {
  id: string;
  serialNumber: string;
  workCenterId: string;
  assetId: string;
  productionLineId: string;
  operatorId: string;
  timestamp: string;
  tankSize?: number;
  shellSize?: string;
}

export interface InspectionRecord {
  id: string;
  serialNumber: string;
  workCenterId: string;
  operatorId: string;
  timestamp: string;
  defects: DefectEntry[];
}

export interface DefectEntry {
  defectCodeId: string;
  defectCodeName?: string;
  characteristicId: string;
  characteristicName?: string;
  locationId: string;
  locationName?: string;
}

export interface DefectCode {
  id: string;
  code: string;
  name: string;
  severity?: string;
}

export interface DefectLocation {
  id: string;
  code: string;
  name: string;
}

export interface Characteristic {
  id: string;
  name: string;
}

export interface MaterialQueueItem {
  id: string;
  position: number;
  status: 'queued' | 'active' | 'completed';
  productDescription: string;
  shellSize?: string;
  heatNumber: string;
  coilNumber: string;
  quantity: number;
  cardId?: string;
  cardColor?: string;
  createdAt?: string;
}

export interface Assembly {
  id: string;
  alphaCode: string;
  tankSize: number;
  shells: string[];
  leftHeadLotId?: string;
  rightHeadLotId?: string;
  leftHeadInfo?: HeadLotInfo;
  rightHeadInfo?: HeadLotInfo;
  workCenterId: string;
  operatorId: string;
  timestamp: string;
}

export interface HeadLotInfo {
  heatNumber: string;
  coilNumber: string;
  productDescription: string;
  cardId?: string;
  cardColor?: string;
}

export interface WCHistoryEntry {
  id: string;
  timestamp: string;
  serialOrIdentifier: string;
  tankSize?: number;
  hasAnnotation: boolean;
}

export interface WCHistoryData {
  dayCount: number;
  recentRecords: WCHistoryEntry[];
}

export interface Annotation {
  id: string;
  productionRecordId: string;
  annotationTypeId: string;
  typeName: string;
  flag: boolean;
  notes?: string;
  initiatedBy: string;
  resolvedBy?: string;
  resolvedNotes?: string;
}

export interface PlantGear {
  id: string;
  name: string;
  level: number;
  plantId: string;
}

export interface FaultEvent {
  description: string;
  timestamp: string;
  workCenterId: string;
}

export interface Vendor {
  id: string;
  name: string;
  vendorType: string;
}

export interface ProductListItem {
  id: string;
  productNumber: string;
  tankSize: number;
  tankType: string;
  nameplateNumber?: string;
}

export interface XrayQueueItem {
  id: string;
  serialNumber: string;
  createdAt: string;
}

export interface RoundSeamSetup {
  id?: string;
  tankSize: number;
  rs1WelderId?: string;
  rs2WelderId?: string;
  rs3WelderId?: string;
  rs4WelderId?: string;
  isComplete: boolean;
}

export interface AssemblyLookup {
  alphaCode: string;
  tankSize: number;
  roundSeamCount: number;
}

export interface NameplateRecordInfo {
  id: string;
  serialNumber: string;
  productId: string;
  timestamp: string;
}

export interface BarcodeCardInfo {
  id: string;
  cardValue: string;
  color?: string;
  colorName?: string;
  isAssigned: boolean;
}
