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
  isHoldTagEnabled?: boolean;
}

export interface ProductionLineAdmin {
  id: string;
  name: string;
  plantId: string;
  plantName: string;
  isHoldTagEnabled?: boolean;
}

export interface WorkCenter {
  id: string;
  name: string;
  workCenterTypeId: string;
  workCenterTypeName: string;
  numberOfWelders: number;
  dataEntryType?: string;
  materialQueueForWCId?: string;
  isHoldTagEnabled?: boolean;
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
  demoMode?: boolean;
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
  characteristicIds: string[];
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
  quantityCompleted: number;
  productId?: string;
  vendorMillId?: string;
  vendorProcessorId?: string;
  vendorHeadId?: string;
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
  serialNumberId?: string;
  timestamp: string;
  serialOrIdentifier: string;
  productId?: string;
  tankSize?: number;
  hasAnnotation: boolean;
  annotationColor?: string;
}

export interface TankSizeCount {
  tankSize?: number | null;
  count: number;
}

export interface WCHistoryData {
  dayCount: number;
  hourlyCounts?: HourlyCount[];
  tankSizeCounts?: TankSizeCount[];
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
  demoMode?: boolean;
  requirePinForLogin: boolean;
  hasPin: boolean;
  userType: number;
  isActive: boolean;
}

export interface DemoShellCurrent {
  stage: string;
  hasCurrent: boolean;
  barcodeRaw?: string;
  serialNumber?: string;
  shellNumber?: number;
  stageQueueCount: number;
}

export interface DemoDataTableCount {
  table: string;
  count: number;
}

export interface DemoDataResetSeedResult {
  executedAtUtc: string;
  deleted: DemoDataTableCount[];
  inserted: DemoDataTableCount[];
}

export interface DemoDataRefreshDatesResult {
  executedAtUtc: string;
  appliedDeltaHours: number;
  updated: DemoDataTableCount[];
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
  characteristicIds: string[];
  characteristicNames: string[];
  isActive: boolean;
}

export interface AdminWorkCenter {
  id: string;
  name: string;
  workCenterTypeName: string;
  numberOfWelders: number;
  productionSequence?: number;
  dataEntryType?: string;
  materialQueueForWCId?: string;
  materialQueueForWCName?: string;
  isHoldTagEnabled?: boolean;
}

export interface WorkCenterType {
  id: string;
  name: string;
}

export interface AdminWorkCenterGroup {
  groupId: string;
  baseName: string;
  workCenterTypeName: string;
  productionSequence?: number;
  dataEntryType?: string;
  isHoldTagEnabled?: boolean;
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
  enableWorkCenterChecklist?: boolean;
  enableSafetyChecklist?: boolean;
}

export interface ChecklistTemplateItem {
  id?: string;
  sortOrder: number;
  prompt: string;
  isRequired: boolean;
  section?: string;
  responseMode?: string;
  responseType?: 'Checkbox' | 'Datetime' | 'Number' | 'Image' | 'Dimension' | 'Score';
  responseOptions?: string[];
  scoreTypeId?: string;
  scoreOptions?: ScoreTypeValue[];
  dimensionTarget?: number;
  dimensionUpperLimit?: number;
  dimensionLowerLimit?: number;
  dimensionUnitOfMeasure?: string;
  helpText?: string;
  requireFailNote: boolean;
}

export interface ChecklistTemplate {
  id: string;
  templateCode: string;
  title: string;
  checklistType: string;
  scopeLevel: string;
  siteId?: string;
  workCenterId?: string;
  productionLineId?: string;
  versionNo: number;
  effectiveFromUtc: string;
  effectiveToUtc?: string;
  isActive: boolean;
  responseMode: string;
  requireFailNote: boolean;
  isSafetyProfile: boolean;
  ownerUserId: string;
  items: ChecklistTemplateItem[];
}

export interface ScoreTypeValue {
  id?: string;
  score: number;
  description: string;
  sortOrder: number;
}

export interface ScoreType {
  id: string;
  name: string;
  isActive: boolean;
  values: ScoreTypeValue[];
}

export interface ChecklistResponse {
  id?: string;
  checklistTemplateItemId: string;
  responseValue: string;
  note?: string;
}

export interface ChecklistEntry {
  id: string;
  checklistTemplateId: string;
  checklistType: string;
  siteId: string;
  workCenterId: string;
  productionLineId?: string;
  operatorUserId: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  resolvedFromScope: string;
  resolvedTemplateCode: string;
  resolvedTemplateVersionNo: number;
  responses: ChecklistResponse[];
}

export interface ChecklistResponseBucket {
  value: string;
  label: string;
  count: number;
}

export interface ChecklistQuestionSummary {
  checklistTemplateItemId: string;
  prompt: string;
  section?: string;
  responseType: string;
  responseCount: number;
  responseBuckets: ChecklistResponseBucket[];
}

export interface ChecklistFilterOption {
  checklistType: string;
  checklistName: string;
}

export interface ChecklistReviewSummary {
  siteId: string;
  fromUtc: string;
  toUtc: string;
  checklistType?: string;
  totalEntries: number;
  totalResponses: number;
  checklistTypesFound: string[];
  checklistFiltersFound?: ChecklistFilterOption[];
  questions: ChecklistQuestionSummary[];
}

export interface ChecklistQuestionResponseRow {
  checklistEntryId: string;
  checklistType: string;
  operatorUserId: string;
  operatorDisplayName: string;
  respondedAtUtc: string;
  responseValue: string;
  responseLabel: string;
  note?: string;
}

export interface ChecklistQuestionResponses {
  checklistTemplateItemId: string;
  prompt: string;
  section?: string;
  responseType: string;
  totalResponses: number;
  responseBuckets: ChecklistResponseBucket[];
  rows: ChecklistQuestionResponseRow[];
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

export interface OperatorControlPlan {
  id: string;
  characteristicName: string;
  resultType: string;
  isGateCheck: boolean;
}

export interface AdminControlPlan {
  id: string;
  characteristicId: string;
  characteristicName: string;
  workCenterProductionLineId: string;
  workCenterName: string;
  productionLineName: string;
  plantId: string;
  plantName: string;
  plantCode: string;
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
  nextTankAlphaCode: string;
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
  annotations: LogAnnotationBadge[];
}

export interface SerialNumberLookup {
  serialNumber: string;
  treeNodes: TraceabilityNode[];
}

export interface SellableTankStatus {
  serialNumber: string;
  alphaCode: string | null;
  shellSerials: string[];
  productNumber: string;
  tankSize: number;
  rtXrayResult: string | null;
  spotXrayResult: string | null;
  hydroResult: string | null;
}

export interface WhereUsedResult {
  plant: string;
  serialNumber: string;
  productionNumber: string;
  tankSize: number;
  hydroCompletedAt: string | null;
}

export interface AdminPlantPrinter {
  id: string;
  plantId: string;
  plantName: string;
  plantCode: string;
  printerName: string;
  documentPath: string;
  enabled: boolean;
  printLocation: string;
}

export interface NiceLabelPrinter {
  printerName: string;
}

export interface NiceLabelDocument {
  name: string;
  itemPath: string;
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
  id: string;
  abbreviation: string;
  color: string;
  typeName: string;
  status: string;
  notes?: string;
  initiatedByName: string;
  resolvedByName?: string;
  resolvedNotes?: string;
  createdAt: string;
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

// ---- Spot X-ray Operator types ----

export interface SpotXrayQueueTank {
  position: number;
  assemblySerialNumberId: string;
  alphaCode: string;
  shellSerials: string[];
  tankSize: number;
  weldType: string;
  roundSeamWeldedAtUtc?: string;
  seamWelders?: string;
  welderNames: string[];
  welderIds: string[];
  sizeChanged: boolean;
  welderChanged: boolean;
}

export interface SpotXrayLane {
  laneName: string;
  draftCount: number;
  tanks: SpotXrayQueueTank[];
}

export interface SpotXrayLaneQueues {
  lanes: SpotXrayLane[];
}

export interface SpotXrayIncrementSummary {
  id: string;
  incrementNo: string;
  laneNo: string;
  tankSize?: number;
  overallStatus: string;
  isDraft: boolean;
}

export interface SpotXrayIncrementTank {
  serialNumberId: string;
  alphaCode: string;
  shellSerials: string[];
  position: number;
}

export interface SpotXraySeam {
  seamNumber: number;
  welderName?: string;
  welderId?: string;
  shotNo?: string;
  shotDateTime?: string;
  result?: string;
  trace1ShotNo?: string;
  trace1DateTime?: string;
  trace1TankId?: string;
  trace1TankAlpha?: string;
  trace1Result?: string;
  trace2ShotNo?: string;
  trace2DateTime?: string;
  trace2TankId?: string;
  trace2TankAlpha?: string;
  trace2Result?: string;
  finalShotNo?: string;
  finalDateTime?: string;
  finalResult?: string;
}

export interface SpotXrayIncrementDetail {
  id: string;
  incrementNo: string;
  overallStatus: string;
  laneNo: string;
  isDraft: boolean;
  tankSize?: number;
  seamCount: number;
  inspectTankId?: string;
  inspectTankAlpha?: string;
  tanks: SpotXrayIncrementTank[];
  seams: SpotXraySeam[];
  createdDateTime?: string;
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
  monthCount: number;
  supportsFirstPassYield: boolean;
  dayFPY: number | null;
  weekFPY: number | null;
  monthFPY: number | null;
  dayDefects: number;
  weekDefects: number;
  monthDefects: number;
  dayDowntimeMinutes: number;
  weekDowntimeMinutes: number;
  monthDowntimeMinutes: number;
  dayQtyPerHour: number;
  weekQtyPerHour: number;
  monthQtyPerHour: number;
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

export interface KpiTrendPoint {
  date: string;
  value: number | null;
}

export interface SupervisorDashboardTrends {
  count: KpiTrendPoint[];
  fpy: KpiTrendPoint[];
  defects: KpiTrendPoint[];
  qtyPerHour: KpiTrendPoint[];
  downtimeMinutes: KpiTrendPoint[];
  oee: KpiTrendPoint[];
  availability: KpiTrendPoint[];
  performance: KpiTrendPoint[];
  quality: KpiTrendPoint[];
}

export interface DefectParetoItem {
  defectCode: string;
  defectName: string;
  count: number;
  cumulativePercent: number;
}

export interface DefectParetoResponse {
  totalDefects: number;
  items: DefectParetoItem[];
}

export interface DowntimeParetoItem {
  reasonName: string;
  minutes: number;
  cumulativePercent: number;
}

export interface DowntimeParetoResponse {
  totalDowntimeMinutes: number;
  items: DowntimeParetoItem[];
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

export interface EdgeWipCount {
  fromWorkCenterId: string;
  toWorkCenterId: string;
  count: number;
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
  edgeWipCounts: EdgeWipCount[];
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
  countsAsDowntime: boolean;
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
  workCenterName: string;
  productionLineName: string;
  operatorUserId: string;
  operatorName: string;
  downtimeReasonId?: string;
  downtimeReasonName?: string;
  downtimeReasonCategoryName?: string;
  countsAsDowntime?: boolean;
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

// ── Audit Log ──

export interface AuditLogEntry {
  id: number;
  action: string;
  entityName: string;
  entityId: string;
  changes: string | null;
  changedByUserName: string;
  changedByUserId: string;
  changedAtUtc: string;
}

export interface AuditLogPage {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ---- Frontend Telemetry ----
export interface FrontendTelemetryEntry {
  id: number;
  occurredAtUtc: string;
  receivedAtUtc: string;
  category: string;
  source: string;
  severity: string;
  isReactRuntimeOverlayCandidate: boolean;
  message: string;
  stack?: string;
  route?: string;
  screen?: string;
  metadataJson?: string;
  sessionId?: string;
  correlationId?: string;
  apiPath?: string;
  httpMethod?: string;
  httpStatus?: number;
  userId?: string;
  userDisplayName?: string;
  workCenterId?: string;
  workCenterName?: string;
  productionLineId?: string;
  productionLineName?: string;
  plantId?: string;
  plantName?: string;
}

export interface FrontendTelemetryPage {
  items: FrontendTelemetryEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface FrontendTelemetryFilterOptions {
  categories: string[];
  sources: string[];
  severities: string[];
}

export interface FrontendTelemetryCount {
  rowCount: number;
  warningThreshold: number;
  isWarning: boolean;
}

export interface FrontendTelemetryArchiveResult {
  deletedRows: number;
  remainingRows: number;
}

// ---- Natural Language Query ----
export interface NaturalLanguageQueryDataPoint {
  label: string;
  value: string;
  unit?: string;
}

export interface NaturalLanguageQueryTrace {
  intent: string;
  usedModel: boolean;
  usedCache: boolean;
  durationMs: number;
}

export interface NaturalLanguageQueryResponse {
  answerText: string;
  scopeUsed: string;
  confidence: number;
  dataPoints: NaturalLanguageQueryDataPoint[];
  followUps: string[];
  trace: NaturalLanguageQueryTrace;
}

export interface WorkflowStepDefinition {
  id: string;
  stepCode: string;
  stepName: string;
  sequence: number;
  requiredFields: string[];
  requiredChecklistTemplateIds: string[];
  approvalMode: 'None' | 'AnyOne' | 'All';
  approvalAssignments: string[];
  allowReject: boolean;
  onApproveNextStepCode?: string;
  onRejectTargetStepCode?: string;
}

export interface WorkflowDefinition {
  id: string;
  workflowType: string;
  version: number;
  isActive: boolean;
  startStepCode: string;
  steps: WorkflowStepDefinition[];
}

export interface WorkflowInstance {
  id: string;
  workflowDefinitionId: string;
  workflowType: string;
  workflowDefinitionVersion: number;
  entityType: string;
  entityId: string;
  status: string;
  currentStepCode: string;
  startedAtUtc: string;
  completedAtUtc?: string;
}

export interface WorkflowEvent {
  id: string;
  eventType: string;
  eventAtUtc: string;
  actorUserId?: string;
  payloadJson: string;
}

export interface WorkflowWorkItem {
  id: string;
  workflowInstanceId: string;
  entityType: string;
  entityId: string;
  workItemType: string;
  title: string;
  instructions?: string;
  assignedUserId?: string;
  assignedRoleTier?: number;
  status: string;
  priority: string;
  dueAtUtc?: string;
  createdAtUtc: string;
}

export interface NotificationRule {
  id: string;
  workflowType: string;
  triggerEvent: string;
  targetStepCodes: string[];
  recipientMode: string;
  recipientConfigJson: string;
  templateKey: string;
  templateTitle: string;
  templateBody: string;
  clearPolicy: string;
  isActive: boolean;
}

export interface HoldTag {
  id: string;
  holdTagNumber: number;
  siteCode: string;
  productionLineId?: string;
  workCenterId?: string;
  serialNumberMasterId: string;
  problemDescription: string;
  defectCodeId?: string;
  disposition?: 'ReleaseAsIs' | 'Repair' | 'Scrap';
  businessStatus: string;
  workflowInstanceId: string;
  linkedNcrId?: string;
  createdAtUtc: string;
}

export interface NcrType {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  isVendorRelated: boolean;
  description?: string;
  workflowDefinitionId: string;
}

export interface Ncr {
  id: string;
  ncrNumber: number;
  sourceType: string;
  sourceEntityId?: string;
  siteCode: string;
  submitterUserId: string;
  coordinatorUserId: string;
  ncrTypeId: string;
  currentStepCode: string;
  workflowInstanceId: string;
  problemDescription: string;
  dateUtc: string;
}

// ---- Heijunka Scheduling ----
export interface HeijunkaScheduleLine {
  id: string;
  plannedDateLocal: string;
  sequenceIndex?: number;
  productId?: string;
  planningClass: string;
  plannedQty: number;
  loadGroupId?: string;
  dispatchDateLocal?: string;
  mesPlanningGroupId?: string;
  planningResourceId?: string;
  executionResourceId?: string;
}

export interface HeijunkaSchedule {
  id: string;
  siteCode: string;
  productionLineId: string;
  weekStartDateLocal: string;
  status: 'Draft' | 'Published' | 'InExecution' | 'Closed';
  publishedAtUtc?: string;
  publishedByUserId?: string;
  freezeHours: number;
  revisionNumber: number;
  lines: HeijunkaScheduleLine[];
}

export interface HeijunkaScheduleChangeLog {
  id: string;
  scheduleLineId?: string;
  changedAtUtc: string;
  changedByUserId: string;
  changeReasonCode: string;
  fieldName: string;
  fromValue?: string;
  toValue?: string;
}

export interface ErpSkuPlanningGroupMapping {
  id: string;
  erpSkuCode: string;
  mesPlanningGroupId: string;
  siteCode?: string;
  effectiveFromUtc: string;
  effectiveToUtc?: string;
  isActive: boolean;
  mappingOwnerUserId: string;
  lastReviewedAtUtc?: string;
  requiresReview: boolean;
}

export interface UnmappedDemandException {
  id: string;
  erpSkuCode: string;
  siteCode: string;
  loadGroupId: string;
  dispatchDateLocal: string;
  requiredQty: number;
  detectedAtUtc: string;
  exceptionStatus: 'Open' | 'Resolved' | 'Deferred';
  resolutionNotes?: string;
}

export interface DispatchRiskSummary {
  siteCode: string;
  productionLineId: string;
  weekStartDateLocal: string;
  openUnmappedExceptions: number;
  loadGroupsDue: number;
  loadGroupsPlanned: number;
  hasDispatchRisk: boolean;
}

export interface HeijunkaExecutionEvent {
  id: string;
  scheduleLineId?: string;
  executionState: 'Completed' | 'Short' | 'Missed' | 'Moved';
  actualQty: number;
  executionDateLocal: string;
  idempotencyKey: string;
}

export interface KpiMetric {
  value?: number;
  nullReasonCode?: string;
}

export interface HeijunkaPhase1Kpis {
  siteCode: string;
  productionLineId: string;
  fromDateLocal: string;
  toDateLocal: string;
  isEligible: boolean;
  eligibilityReason?: string;
  scheduleAdherencePercent: KpiMetric;
  planAttainmentPercent: KpiMetric;
  loadReadinessPercent: KpiMetric;
  supermarketStockoutDurationMinutes: KpiMetric;
}

export interface DispatchWeekOrderCoverage {
  siteCode: string;
  productionLineId: string;
  weekStartDateLocal: string;
  loadGroupId: string;
  dispatchDateLocal: string;
  erpSalesOrderId: string;
  erpSalesOrderLineId: string;
  erpSkuCode: string;
  mesPlanningGroupId?: string;
  requiredQty: number;
  loadGroupRequiredQty: number;
  loadGroupPlannedQty: number;
  isMapped: boolean;
  loadGroupCovered: boolean;
}

export interface SupermarketQuantityStatus {
  siteCode: string;
  productionLineId: string;
  weekStartDateLocal: string;
  productId?: string;
  onHandQty: number;
  inTransitQty: number;
  demandQty: number;
  netAvailableQty: number;
  stockoutDurationMinutes: number;
  hasOpenStockout: boolean;
  lastCapturedAtUtc: string;
}

export type WorkCenterBreakdownDimension = 'TankSize' | 'TankType' | 'Color' | 'FinishedPartNumber';

export interface WorkCenterBreakdownConfig {
  id: string;
  siteCode: string;
  productionLineId: string;
  workCenterId: string;
  workCenterName: string;
  groupingDimensions: WorkCenterBreakdownDimension[];
}

export interface WorkCenterScheduleBreakdownRow {
  plannedDateLocal: string;
  plannedQty: number;
  dimensionValues: Record<string, string>;
}

export interface WorkCenterScheduleBreakdown {
  scheduleId: string;
  siteCode: string;
  productionLineId: string;
  workCenterId: string;
  workCenterName: string;
  weekStartDateLocal: string;
  groupingDimensions: WorkCenterBreakdownDimension[];
  rows: WorkCenterScheduleBreakdownRow[];
}
