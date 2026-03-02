import { lazy, Suspense, useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Spinner } from '@fluentui/react-components';
import { reportTelemetry } from '../../telemetry/telemetryClient.ts';

const named = <T extends Record<string, React.ComponentType>>(
  loader: () => Promise<T>,
  name: keyof T,
) => lazy(() => loader().then((m) => ({ default: m[name] as React.ComponentType })));

const ProductMaintenanceScreen = named(() => import('./ProductMaintenanceScreen.tsx'), 'ProductMaintenanceScreen');
const UserMaintenanceScreen = named(() => import('./UserMaintenanceScreen.tsx'), 'UserMaintenanceScreen');
const VendorMaintenanceScreen = named(() => import('./VendorMaintenanceScreen.tsx'), 'VendorMaintenanceScreen');
const WorkCenterConfigScreen = named(() => import('./WorkCenterConfigScreen.tsx'), 'WorkCenterConfigScreen');
const ProductionLineWorkCentersScreen = named(() => import('./ProductionLineWorkCentersScreen.tsx'), 'ProductionLineWorkCentersScreen');
const DefectCodesScreen = named(() => import('./DefectCodesScreen.tsx'), 'DefectCodesScreen');
const DefectLocationsScreen = named(() => import('./DefectLocationsScreen.tsx'), 'DefectLocationsScreen');
const AssetManagementScreen = named(() => import('./AssetManagementScreen.tsx'), 'AssetManagementScreen');
const KanbanCardScreen = named(() => import('./KanbanCardScreen.tsx'), 'KanbanCardScreen');
const CharacteristicsScreen = named(() => import('./CharacteristicsScreen.tsx'), 'CharacteristicsScreen');
const ControlPlansScreen = named(() => import('./ControlPlansScreen.tsx'), 'ControlPlansScreen');
const ChecklistTemplatesScreen = named(() => import('./ChecklistTemplatesScreen.tsx'), 'ChecklistTemplatesScreen');
const ChecklistResponseReviewScreen = named(() => import('./ChecklistResponseReviewScreen.tsx'), 'ChecklistResponseReviewScreen');
const ChecklistTemplateEditorScreen = named(() => import('./ChecklistTemplateEditorScreen.tsx'), 'ChecklistTemplateEditorScreen');
const ScoreTypesScreen = named(() => import('./ScoreTypesScreen.tsx'), 'ScoreTypesScreen');
const PlantGearScreen = named(() => import('./PlantGearScreen.tsx'), 'PlantGearScreen');
const WhosOnFloorScreen = named(() => import('./WhosOnFloorScreen.tsx'), 'WhosOnFloorScreen');
const ProductionLineMaintenanceScreen = named(() => import('./ProductionLineMaintenanceScreen.tsx'), 'ProductionLineMaintenanceScreen');
const AnnotationTypesScreen = named(() => import('./AnnotationTypesScreen.tsx'), 'AnnotationTypesScreen');
const SerialNumberLookupScreen = named(() => import('./SerialNumberLookupScreen.tsx'), 'SerialNumberLookupScreen');
const SellableTankStatusScreen = named(() => import('./SellableTankStatusScreen.tsx'), 'SellableTankStatusScreen');
const WhereUsedScreen = named(() => import('./WhereUsedScreen.tsx'), 'WhereUsedScreen');
const AnnotationMaintenanceScreen = named(() => import('./AnnotationMaintenanceScreen.tsx'), 'AnnotationMaintenanceScreen');
const PlantPrinterScreen = named(() => import('./PlantPrinterScreen.tsx'), 'PlantPrinterScreen');
const IssuesScreen = named(() => import('./IssuesScreen.tsx'), 'IssuesScreen');
const AIReviewScreen = named(() => import('./AIReviewScreen.tsx'), 'AIReviewScreen');
const ProductionLogsScreen = named(() => import('./ProductionLogsScreen.tsx'), 'ProductionLogsScreen');
const SupervisorDashboardScreen = named(() => import('./SupervisorDashboardScreen.tsx'), 'SupervisorDashboardScreen');
const DowntimeReasonsScreen = named(() => import('./DowntimeReasonsScreen.tsx'), 'DowntimeReasonsScreen');
const DowntimeEventsScreen = named(() => import('./DowntimeEventsScreen.tsx'), 'DowntimeEventsScreen');
const DigitalTwinScreen = named(() => import('./DigitalTwinScreen.tsx'), 'DigitalTwinScreen');
const ShiftScheduleScreen = named(() => import('./ShiftScheduleScreen.tsx'), 'ShiftScheduleScreen');
const CapacityTargetsScreen = named(() => import('./CapacityTargetsScreen.tsx'), 'CapacityTargetsScreen');
const AuditLogScreen = named(() => import('./AuditLogScreen.tsx'), 'AuditLogScreen');
const FrontendTelemetryScreen = named(() => import('./FrontendTelemetryScreen.tsx'), 'FrontendTelemetryScreen');
const TestCoverageScreen = named(() => import('./TestCoverageScreen.tsx'), 'TestCoverageScreen');
const DemoDataResetScreen = named(() => import('./DemoDataResetScreen.tsx'), 'DemoDataResetScreen');

function AdminFallbackRoute() {
  useEffect(() => {
    reportTelemetry({
      category: 'navigation_issue',
      source: 'admin_route_fallback',
      severity: 'warning',
      isReactRuntimeOverlayCandidate: false,
      message: 'Unknown admin route redirected to /menu',
    });
  }, []);
  return <Navigate to="/menu" replace />;
}

export function AdminRoutes() {
  return (
    <Suspense fallback={<Spinner size="medium" style={{ padding: 40 }} />}>
      <Routes>
        <Route path="products" element={<ProductMaintenanceScreen />} />
        <Route path="users" element={<UserMaintenanceScreen />} />
        <Route path="vendors" element={<VendorMaintenanceScreen />} />
        <Route path="workcenters" element={<WorkCenterConfigScreen />} />
        <Route path="production-line-workcenters" element={<ProductionLineWorkCentersScreen />} />
        <Route path="defect-codes" element={<DefectCodesScreen />} />
        <Route path="defect-locations" element={<DefectLocationsScreen />} />
        <Route path="assets" element={<AssetManagementScreen />} />
        <Route path="kanban-cards" element={<KanbanCardScreen />} />
        <Route path="characteristics" element={<CharacteristicsScreen />} />
        <Route path="control-plans" element={<ControlPlansScreen />} />
        <Route path="checklists" element={<ChecklistTemplatesScreen />} />
        <Route path="checklist-response-review" element={<ChecklistResponseReviewScreen />} />
        <Route path="checklists/new" element={<ChecklistTemplateEditorScreen />} />
        <Route path="checklists/:templateId" element={<ChecklistTemplateEditorScreen />} />
        <Route path="checklist-score-types" element={<ScoreTypesScreen />} />
        <Route path="plant-gear" element={<PlantGearScreen />} />
        <Route path="whos-on-floor" element={<WhosOnFloorScreen />} />
        <Route path="production-lines" element={<ProductionLineMaintenanceScreen />} />
        <Route path="annotation-types" element={<AnnotationTypesScreen />} />
        <Route path="annotations" element={<AnnotationMaintenanceScreen />} />
        <Route path="serial-lookup" element={<SerialNumberLookupScreen />} />
        <Route path="sellable-tank-status" element={<SellableTankStatusScreen />} />
        <Route path="where-used" element={<WhereUsedScreen />} />
        <Route path="plant-printers" element={<PlantPrinterScreen />} />
        <Route path="issues" element={<IssuesScreen />} />
        <Route path="report-issue" element={<Navigate to="/menu/issues" replace />} />
        <Route path="issue-approvals" element={<Navigate to="/menu/issues" replace />} />
        <Route path="ai-review" element={<AIReviewScreen />} />
        <Route path="production-logs" element={<ProductionLogsScreen />} />
        <Route path="supervisor-dashboard" element={<SupervisorDashboardScreen />} />
        <Route path="downtime-reasons" element={<DowntimeReasonsScreen />} />
        <Route path="downtime-events" element={<DowntimeEventsScreen />} />
        <Route path="digital-twin" element={<DigitalTwinScreen />} />
        <Route path="shift-schedule" element={<ShiftScheduleScreen />} />
        <Route path="capacity-targets" element={<CapacityTargetsScreen />} />
        <Route path="audit-log" element={<AuditLogScreen />} />
        <Route path="frontend-telemetry" element={<FrontendTelemetryScreen />} />
        <Route path="test-coverage" element={<TestCoverageScreen />} />
        <Route path="demo-data" element={<DemoDataResetScreen />} />
        <Route path="*" element={<AdminFallbackRoute />} />
      </Routes>
    </Suspense>
  );
}
