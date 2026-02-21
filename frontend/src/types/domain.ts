export interface Plant {
  id: string;
  code: string;
  name: string;
  timeZoneId: string;
}

export interface ProductionLine {
  id: string;
  name: string;
  plantId: string;
}

export interface ProductionLineAdmin {
  id: string;
  name: string;
  plantId: string;
  plantName: string;
}

export interface WorkCenter {
  id: string;
  name: string;
  workCenterTypeId: string;
  workCenterTypeName: string;
  numberOfWelders: number;
  dataEntryType?: string;
  materialQueueForWCId?: string;
}

export interface Asset {
  id: string;
  name: string;
  workCenterId: string;
}

export enum UserType {
  Standard = 0,
  AuthorizedInspector = 1,
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
  userType: number;
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

export interface HeadLotInfo {
  heatNumber: string;
  coilNumber: string;
  lotNumber?: string;
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

export interface QueueTransaction {
  id: string;
  action: string;
  itemSummary: string;
  operatorName: string;
  timestamp: string;
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
  shells: string[];
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

// ---- Admin types ----

export interface AdminProduct {
  id: string;
  productNumber: string;
  tankSize: number;
  tankType: string;
  sageItemNumber?: string;
  nameplateNumber?: string;
  siteNumbers?: string;
  productTypeId: string;
  productTypeName: string;
  isActive: boolean;
}

export interface ProductType {
  id: string;
  name: string;
}

export interface AdminUser {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  displayName: string;
  roleTier: number;
  roleName: string;
  defaultSiteId: string;
  defaultSiteName: string;
  isCertifiedWelder: boolean;
  requirePinForLogin: boolean;
  hasPin: boolean;
  userType: number;
  isActive: boolean;
}

export interface RoleOption {
  tier: number;
  name: string;
}

export interface AdminVendor {
  id: string;
  name: string;
  vendorType: string;
  plantIds?: string;
  isActive: boolean;
}

export interface AdminDefectCode {
  id: string;
  code: string;
  name: string;
  severity?: string;
  systemType?: string;
  workCenterIds: string[];
  isActive: boolean;
}

export interface AdminDefectLocation {
  id: string;
  code: string;
  name: string;
  defaultLocationDetail?: string;
  characteristicId?: string;
  characteristicName?: string;
  isActive: boolean;
}

export interface AdminWorkCenter {
  id: string;
  name: string;
  workCenterTypeName: string;
  numberOfWelders: number;
  dataEntryType?: string;
  materialQueueForWCId?: string;
  materialQueueForWCName?: string;
}

export interface AdminWorkCenterGroup {
  groupId: string;
  baseName: string;
  workCenterTypeName: string;
  dataEntryType?: string;
  siteConfigs: WorkCenterSiteConfig[];
}

export interface WorkCenterSiteConfig {
  workCenterId: string;
  siteName: string;
  numberOfWelders: number;
  materialQueueForWCId?: string;
  materialQueueForWCName?: string;
}

export interface WorkCenterProductionLine {
  id: string;
  workCenterId: string;
  productionLineId: string;
  productionLineName: string;
  plantName: string;
  displayName: string;
  numberOfWelders: number;
}

export interface AdminCharacteristic {
  id: string;
  name: string;
  specHigh?: number;
  specLow?: number;
  specTarget?: number;
  productTypeId?: string;
  productTypeName?: string;
  workCenterIds: string[];
}

export interface AdminControlPlan {
  id: string;
  characteristicId: string;
  characteristicName: string;
  workCenterId: string;
  workCenterName: string;
  isEnabled: boolean;
  resultType: string;
  isGateCheck: boolean;
}

export interface AdminAsset {
  id: string;
  name: string;
  workCenterId: string;
  workCenterName: string;
  productionLineId: string;
  productionLineName: string;
  limbleIdentifier?: string;
  isActive: boolean;
}

export interface AdminBarcodeCard {
  id: string;
  cardValue: string;
  color?: string;
  description?: string;
}

export interface PlantWithGear {
  plantId: string;
  plantName: string;
  plantCode: string;
  currentPlantGearId?: string;
  currentGearLevel?: number;
  gears: PlantGearItem[];
}

export interface PlantGearItem {
  id: string;
  name: string;
  level: number;
  plantId: string;
}

export interface ActiveSession {
  id: string;
  userId: string;
  userDisplayName: string;
  employeeNumber: string;
  plantId: string;
  productionLineId: string;
  productionLineName: string;
  workCenterId: string;
  workCenterName: string;
  loginDateTime: string;
  lastHeartbeatDateTime: string;
  isStale: boolean;
}

export interface AdminAnnotationType {
  id: string;
  name: string;
  abbreviation?: string;
  requiresResolution: boolean;
  operatorCanCreate: boolean;
  displayColor?: string;
}

export interface TraceabilityNode {
  id: string;
  label: string;
  nodeType: string;
  children?: TraceabilityNode[];
}

export interface ManufacturingEvent {
  timestamp: string;
  workCenterName: string;
  type: string;
  completedBy: string;
  assetName?: string;
  inspectionResult?: string;
}

export interface SerialNumberLookup {
  serialNumber: string;
  treeNodes: TraceabilityNode[];
  events: ManufacturingEvent[];
}

export interface SellableTankStatus {
  serialNumber: string;
  productNumber: string;
  tankSize: number;
  rtXrayResult: string | null;
  spotXrayResult: string | null;
  hydroResult: string | null;
}

export interface AdminAnnotation {
  id: string;
  serialNumber: string;
  annotationTypeName: string;
  annotationTypeId: string;
  flag: boolean;
  notes?: string;
  initiatedByName: string;
  resolvedByName?: string;
  resolvedNotes?: string;
  createdAt: string;
}
