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
  code: string;
  name: string;
  minTankSize?: number;
}

export interface MaterialQueueItem {
  id: string;
  position: number;
  status: 'queued' | 'active' | 'completed';
  productDescription: string;
  shellSize?: string;
  heatNumber: string;
  coilNumber: string;
  lotNumber?: string;
  quantity: number;
  productId?: string;
  vendorMillId?: string;
  vendorProcessorId?: string;
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
  tankSize?: number;
}

export interface WCHistoryEntry {
  id: string;
  productionRecordId?: string;
  timestamp: string;
  serialOrIdentifier: string;
  tankSize?: number;
  hasAnnotation: boolean;
  annotationColor?: string;
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
  status: string;
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
  tankSize?: number;
  timestamp: string;
  printSucceeded: boolean;
  printMessage?: string;
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
  downtimeTrackingEnabled: boolean;
  downtimeThresholdMinutes: number;
}

export interface AdminCharacteristic {
  id: string;
  code: string;
  name: string;
  specHigh?: number;
  specLow?: number;
  specTarget?: number;
  minTankSize?: number;
  productTypeId?: string;
  productTypeName?: string;
  workCenterIds: string[];
  isActive: boolean;
}

export interface AdminControlPlan {
  id: string;
  characteristicId: string;
  characteristicName: string;
  workCenterProductionLineId: string;
  workCenterName: string;
  productionLineName: string;
  isEnabled: boolean;
  resultType: string;
  isGateCheck: boolean;
  codeRequired: boolean;
  isActive: boolean;
}

export interface AdminAsset {
  id: string;
  name: string;
  workCenterId: string;
  workCenterName: string;
  productionLineId: string;
  productionLineName: string;
  limbleIdentifier?: string;
  laneName?: string;
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
  limbleLocationId?: string;
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
  serial: string;
  productName?: string;
  tankSize?: number;
  tankType?: string;
  vendorName?: string;
  coilNumber?: string;
  heatNumber?: string;
  lotNumber?: string;
  createdAt?: string;
  defectCount?: number;
  annotationCount?: number;
  childSerials?: string[];
  children?: TraceabilityNode[];
  events?: ManufacturingEvent[];
}

export interface ManufacturingEvent {
  serialNumberId: string;
  serialNumberSerial: string;
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
}

export interface SellableTankStatus {
  serialNumber: string;
  productNumber: string;
  tankSize: number;
  rtXrayResult: string | null;
  spotXrayResult: string | null;
  hydroResult: string | null;
}

export interface AdminPlantPrinter {
  id: string;
  plantId: string;
  plantName: string;
  plantCode: string;
  printerName: string;
  enabled: boolean;
  printLocation: string;
}

export interface AdminAnnotation {
  id: string;
  serialNumber: string;
  annotationTypeName: string;
  annotationTypeId: string;
  status: string;
  notes?: string;
  initiatedByName: string;
  resolvedByName?: string;
  resolvedNotes?: string;
  createdAt: string;
  linkedEntityType?: string;
  linkedEntityId?: string;
  linkedEntityName?: string;
}

export interface AIReviewRecord {
  id: string;
  timestamp: string;
  serialOrIdentifier: string;
  tankSize?: string;
  operatorName: string;
  alreadyReviewed: boolean;
}

// ---- Log Viewer types ----

export interface LogAnnotationBadge {
  abbreviation: string;
  color: string;
}

export interface RollsLogEntry {
  id: string;
  timestamp: string;
  coilHeatLot: string;
  thickness?: string;
  shellCode: string;
  tankSize?: number;
  welders: string[];
  annotations: LogAnnotationBadge[];
}

export interface FitupLogEntry {
  id: string;
  timestamp: string;
  headNo1?: string;
  headNo2?: string;
  shellNo1?: string;
  shellNo2?: string;
  shellNo3?: string;
  alphaCode?: string;
  tankSize?: number;
  welders: string[];
  annotations: LogAnnotationBadge[];
}

export interface HydroLogEntry {
  id: string;
  timestamp: string;
  nameplate?: string;
  alphaCode?: string;
  tankSize?: number;
  operator: string;
  welders: string[];
  result?: string;
  defectCount: number;
  annotations: LogAnnotationBadge[];
}

export interface RtXrayLogEntry {
  id: string;
  timestamp: string;
  shellCode: string;
  tankSize?: number;
  operator: string;
  result?: string;
  defects?: string;
  annotations: LogAnnotationBadge[];
}

export interface SpotXrayShotCount {
  date: string;
  count: number;
}

export interface SpotXrayLogEntry {
  id: string;
  timestamp: string;
  tanks: string;
  inspected?: string;
  tankSize?: number;
  operator: string;
  result?: string;
  shots?: string;
  annotations: LogAnnotationBadge[];
}

export interface SpotXrayLogResponse {
  shotCounts: SpotXrayShotCount[];
  entries: SpotXrayLogEntry[];
}

// ---- Supervisor Dashboard types ----

export interface HourlyCount {
  hour: number;
  count: number;
}

export interface DailyCount {
  date: string;
  count: number;
}

export interface OperatorSummary {
  id: string;
  displayName: string;
  recordCount: number;
}

export interface SupervisorDashboardMetrics {
  dayCount: number;
  weekCount: number;
  supportsFirstPassYield: boolean;
  dayFPY: number | null;
  weekFPY: number | null;
  dayDefects: number;
  weekDefects: number;
  dayAvgTimeBetweenScans: number;
  weekAvgTimeBetweenScans: number;
  dayQtyPerHour: number;
  weekQtyPerHour: number;
  hourlyCounts: HourlyCount[];
  weekDailyCounts: DailyCount[];
  operators: OperatorSummary[];
  oeeAvailability: number | null;
  oeePerformance: number | null;
  oeeQuality: number | null;
  oeeOverall: number | null;
  oeePlannedMinutes: number | null;
  oeeDowntimeMinutes: number | null;
  oeeRunTimeMinutes: number | null;
}

export interface ExistingAnnotation {
  annotationTypeId: string;
  typeName: string;
  abbreviation?: string;
  displayColor?: string;
}

export interface SupervisorRecord {
  id: string;
  timestamp: string;
  serialOrIdentifier: string;
  tankSize?: string;
  operatorName: string;
  annotations: ExistingAnnotation[];
}

export interface PerformanceTableRow {
  label: string;
  planned: number | null;
  actual: number;
  delta: number | null;
  fpy: number | null;
  downtimeMinutes: number;
}

export interface PerformanceTableResponse {
  rows: PerformanceTableRow[];
  totalRow: PerformanceTableRow | null;
}

// ---- Digital Twin ----

export type StationStatusValue = 'Active' | 'Slow' | 'Idle' | 'Down';

export interface StationStatus {
  workCenterId: string;
  name: string;
  sequence: number;
  wipCount: number;
  status: StationStatusValue;
  isBottleneck: boolean;
  isGateCheck: boolean;
  currentOperator?: string;
  unitsToday: number;
  avgCycleTimeMinutes?: number;
  firstPassYieldPercent?: number;
}

export interface MaterialFeed {
  workCenterName: string;
  queueLabel: string;
  itemCount: number;
  feedsIntoStation: string;
}

export interface LineThroughput {
  unitsToday: number;
  unitsDelta: number;
  unitsPerHour: number;
}

export interface UnitPosition {
  serialNumber: string;
  productName?: string;
  currentStationName: string;
  currentStationSequence: number;
  enteredCurrentStationAt: string;
  isAssembly: boolean;
}

export interface DigitalTwinSnapshot {
  stations: StationStatus[];
  materialFeeds: MaterialFeed[];
  throughput: LineThroughput;
  avgCycleTimeMinutes: number;
  lineEfficiencyPercent: number;
  unitTracker: UnitPosition[];
}

// ---- Downtime ----
export interface DowntimeReasonCategory {
  id: string;
  plantId: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
  reasons: DowntimeReason[];
}

export interface DowntimeReason {
  id: string;
  downtimeReasonCategoryId: string;
  categoryName: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface DowntimeConfig {
  downtimeTrackingEnabled: boolean;
  downtimeThresholdMinutes: number;
  applicableReasons: DowntimeReason[];
}

export interface DowntimeEvent {
  id: string;
  workCenterProductionLineId: string;
  operatorUserId: string;
  operatorName: string;
  downtimeReasonId?: string;
  downtimeReasonName?: string;
  downtimeReasonCategoryName?: string;
  startedAt: string;
  endedAt: string;
  durationMinutes: number;
  isAutoGenerated: boolean;
  createdAt: string;
}

// ---- OEE / Shift Schedule / Capacity Targets ----

export interface ShiftSchedule {
  id: string;
  plantId: string;
  effectiveDate: string;
  mondayHours: number;
  mondayBreakMinutes: number;
  tuesdayHours: number;
  tuesdayBreakMinutes: number;
  wednesdayHours: number;
  wednesdayBreakMinutes: number;
  thursdayHours: number;
  thursdayBreakMinutes: number;
  fridayHours: number;
  fridayBreakMinutes: number;
  saturdayHours: number;
  saturdayBreakMinutes: number;
  sundayHours: number;
  sundayBreakMinutes: number;
  createdAt: string;
  createdByName?: string;
}

export interface CreateShiftScheduleRequest {
  plantId: string;
  effectiveDate: string;
  mondayHours: number;
  mondayBreakMinutes: number;
  tuesdayHours: number;
  tuesdayBreakMinutes: number;
  wednesdayHours: number;
  wednesdayBreakMinutes: number;
  thursdayHours: number;
  thursdayBreakMinutes: number;
  fridayHours: number;
  fridayBreakMinutes: number;
  saturdayHours: number;
  saturdayBreakMinutes: number;
  sundayHours: number;
  sundayBreakMinutes: number;
}

export interface CapacityTarget {
  id: string;
  workCenterProductionLineId: string;
  workCenterName: string;
  productionLineName: string;
  tankSize: number | null;
  plantGearId: string;
  gearLevel: number;
  targetUnitsPerHour: number;
}

export interface CreateCapacityTargetRequest {
  workCenterProductionLineId: string;
  tankSize: number | null;
  plantGearId: string;
  targetUnitsPerHour: number;
}
