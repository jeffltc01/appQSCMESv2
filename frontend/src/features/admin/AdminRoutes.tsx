import { Routes, Route, Navigate } from 'react-router-dom';
import { ProductMaintenanceScreen } from './ProductMaintenanceScreen.tsx';
import { UserMaintenanceScreen } from './UserMaintenanceScreen.tsx';
import { VendorMaintenanceScreen } from './VendorMaintenanceScreen.tsx';
import { WorkCenterConfigScreen } from './WorkCenterConfigScreen.tsx';
import { DefectCodesScreen } from './DefectCodesScreen.tsx';
import { DefectLocationsScreen } from './DefectLocationsScreen.tsx';
import { AssetManagementScreen } from './AssetManagementScreen.tsx';
import { KanbanCardScreen } from './KanbanCardScreen.tsx';
import { CharacteristicsScreen } from './CharacteristicsScreen.tsx';
import { ControlPlansScreen } from './ControlPlansScreen.tsx';
import { PlantGearScreen } from './PlantGearScreen.tsx';
import { WhosOnFloorScreen } from './WhosOnFloorScreen.tsx';
import { ProductionLineMaintenanceScreen } from './ProductionLineMaintenanceScreen.tsx';
import { AnnotationTypesScreen } from './AnnotationTypesScreen.tsx';
import { SerialNumberLookupScreen } from './SerialNumberLookupScreen.tsx';
import { SellableTankStatusScreen } from './SellableTankStatusScreen.tsx';
import { AnnotationMaintenanceScreen } from './AnnotationMaintenanceScreen.tsx';
import { PlantPrinterScreen } from './PlantPrinterScreen.tsx';
import { ReportIssueScreen } from './ReportIssueScreen.tsx';
import { IssueApprovalsScreen } from './IssueApprovalsScreen.tsx';
import { AIReviewScreen } from './AIReviewScreen.tsx';
import { ProductionLogsScreen } from './ProductionLogsScreen.tsx';
import { SupervisorDashboardScreen } from './SupervisorDashboardScreen.tsx';
import { DowntimeReasonsScreen } from './DowntimeReasonsScreen.tsx';

export function AdminRoutes() {
  return (
    <Routes>
      <Route path="products" element={<ProductMaintenanceScreen />} />
      <Route path="users" element={<UserMaintenanceScreen />} />
      <Route path="vendors" element={<VendorMaintenanceScreen />} />
      <Route path="workcenters" element={<WorkCenterConfigScreen />} />
      <Route path="defect-codes" element={<DefectCodesScreen />} />
      <Route path="defect-locations" element={<DefectLocationsScreen />} />
      <Route path="assets" element={<AssetManagementScreen />} />
      <Route path="kanban-cards" element={<KanbanCardScreen />} />
      <Route path="characteristics" element={<CharacteristicsScreen />} />
      <Route path="control-plans" element={<ControlPlansScreen />} />
      <Route path="plant-gear" element={<PlantGearScreen />} />
      <Route path="whos-on-floor" element={<WhosOnFloorScreen />} />
      <Route path="production-lines" element={<ProductionLineMaintenanceScreen />} />
      <Route path="annotation-types" element={<AnnotationTypesScreen />} />
      <Route path="annotations" element={<AnnotationMaintenanceScreen />} />
      <Route path="serial-lookup" element={<SerialNumberLookupScreen />} />
      <Route path="sellable-tank-status" element={<SellableTankStatusScreen />} />
      <Route path="plant-printers" element={<PlantPrinterScreen />} />
      <Route path="report-issue" element={<ReportIssueScreen />} />
      <Route path="issue-approvals" element={<IssueApprovalsScreen />} />
      <Route path="ai-review" element={<AIReviewScreen />} />
      <Route path="production-logs" element={<ProductionLogsScreen />} />
      <Route path="supervisor-dashboard" element={<SupervisorDashboardScreen />} />
      <Route path="downtime-reasons" element={<DowntimeReasonsScreen />} />
      <Route path="*" element={<Navigate to="/menu" replace />} />
    </Routes>
  );
}
