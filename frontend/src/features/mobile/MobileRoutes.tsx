import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext.tsx';
import {
  isOpsDirector,
  isQualityDirector,
} from '../../auth/mobilePolicy.ts';
import {
  MobileAlertsScreen,
  MobileApprovalsScreen,
  MobileBottlenecksScreen,
  MobileLookupScreen,
  MobileMoreScreen,
  MobileOperatorQuickActionsScreen,
  MobileOpsDirectorScreen,
  MobilePlantsScreen,
  MobileQualityPlantsScreen,
  MobileQuickActionsScreen,
  MobileQualityDirectorScreen,
  MobileWhosOnFloorScreen,
} from './MobileScreens.tsx';

function MobileDefaultRoute() {
  const { user } = useAuth();

  if (isQualityDirector(user)) {
    return <Navigate to="/mobile/quality-portfolio" replace />;
  }
  if (isOpsDirector(user)) {
    return <Navigate to="/mobile/operations-portfolio" replace />;
  }
  return <Navigate to="/mobile/operator-quick-actions" replace />;
}

export function MobileRoutes() {
  return (
    <Routes>
      <Route path="alerts" element={<MobileAlertsScreen />} />
      <Route path="approvals" element={<MobileApprovalsScreen />} />
      <Route path="lookup" element={<MobileLookupScreen />} />
      <Route path="whos-on-floor" element={<MobileWhosOnFloorScreen />} />
      <Route path="more" element={<MobileMoreScreen />} />
      <Route path="quality-portfolio" element={<MobileQualityDirectorScreen />} />
      <Route path="quality-plants" element={<MobileQualityPlantsScreen />} />
      <Route path="operations-portfolio" element={<MobileOpsDirectorScreen />} />
      <Route path="bottlenecks" element={<MobileBottlenecksScreen />} />
      <Route path="plants" element={<MobilePlantsScreen />} />
      <Route path="actions" element={<MobileQuickActionsScreen />} />
      <Route path="operator-quick-actions" element={<MobileOperatorQuickActionsScreen />} />
      <Route path="*" element={<MobileDefaultRoute />} />
    </Routes>
  );
}
