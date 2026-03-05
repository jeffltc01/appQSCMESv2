import { useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './auth/AuthContext.tsx';
import { isOperatorKioskRole, hasCachedWorkCenter } from './auth/kioskPolicy.ts';
import { isDirectorRole } from './auth/mobilePolicy.ts';
import { LoginScreen } from './components/login/LoginScreen.tsx';
import { TabletSetupScreen } from './components/tabletSetup/TabletSetupScreen.tsx';
import { OperatorLayout } from './components/layout/OperatorLayout.tsx';
import { EnvironmentWatermark } from './components/layout/EnvironmentWatermark.tsx';
import { MenuScreen } from './features/menu/MenuScreen.tsx';
import { AdminRoutes } from './features/admin/AdminRoutes.tsx';
import { ProductionLogsScreen } from './features/admin/ProductionLogsScreen.tsx';
import { MobileRoutes } from './features/mobile/MobileRoutes.tsx';
import { useIsPhoneViewport } from './hooks/useIsPhoneViewport.ts';
import { reportTelemetry } from './telemetry/telemetryClient.ts';

export function App() {
  const { isAuthenticated, user } = useAuth();
  const isPhone = useIsPhoneViewport();
  const roleTier = user?.roleTier ?? 6;
  const kioskMode = isOperatorKioskRole(roleTier);
  const shouldUseMobileAdmin = isPhone && !kioskMode && isDirectorRole(user);
  const cachedWcId = hasCachedWorkCenter();
  const operatorLandingPath = cachedWcId ? '/operator' : '/tablet-setup';
  const kioskLandingPath = isPhone ? '/mobile/operator-quick-actions' : operatorLandingPath;

  if (!isAuthenticated) {
    return (
      <>
        <EnvironmentWatermark />
        <Routes>
          <Route path="*" element={<LoginScreen />} />
        </Routes>
      </>
    );
  }

  return (
    <>
      <EnvironmentWatermark />
      <Routes>
        <Route
          path="/login"
          element={kioskMode ? <Navigate to={kioskLandingPath} replace /> : <LoginScreen />}
        />
        <Route
          path="/menu"
          element={
            kioskMode
              ? <Navigate to={kioskLandingPath} replace />
              : shouldUseMobileAdmin
                ? <Navigate to="/mobile" replace />
                : <MenuScreen />
          }
        />
        <Route
          path="/menu/*"
          element={
            kioskMode
              ? <Navigate to={kioskLandingPath} replace />
              : shouldUseMobileAdmin
                ? <Navigate to="/mobile" replace />
                : <AdminRoutes />
          }
        />
        <Route
          path="/tablet-setup"
          element={kioskMode && cachedWcId ? <Navigate to="/operator" replace /> : <TabletSetupScreen />}
        />
        <Route
          path="/operator/*"
          element={isPhone && kioskMode ? <Navigate to="/mobile/operator-quick-actions" replace /> : <OperatorLayout />}
        />
        <Route
          path="/operator/production-logs"
          element={isPhone && kioskMode ? <Navigate to="/mobile/operator-quick-actions" replace /> : <ProductionLogsScreen />}
        />
        <Route path="/mobile/*" element={<MobileRoutes />} />
        <Route
          path="*"
          element={
            <DefaultRoute roleTier={roleTier} />
          }
        />
      </Routes>
    </>
  );
}

function DefaultRoute({ roleTier }: { roleTier: number }) {
  const { user } = useAuth();
  const isPhone = useIsPhoneViewport();
  const kioskMode = isOperatorKioskRole(roleTier);
  const shouldUseMobileAdmin = isPhone && !kioskMode && isDirectorRole(user);

  useEffect(() => {
    reportTelemetry({
      category: 'navigation_issue',
      source: 'default_route_fallback',
      severity: 'warning',
      isReactRuntimeOverlayCandidate: false,
      message: 'Fallback route redirect executed',
      metadataJson: JSON.stringify({ roleTier }),
    });
  }, [roleTier]);

  if (kioskMode && isPhone) {
    return <Navigate to="/mobile/operator-quick-actions" replace />;
  }

  if (shouldUseMobileAdmin) {
    return <Navigate to="/mobile" replace />;
  }

  if (roleTier < 6) {
    return <Navigate to="/menu" replace />;
  }

  const cachedWcId = hasCachedWorkCenter();
  if (cachedWcId) {
    return <Navigate to="/operator" replace />;
  }
  return <Navigate to="/tablet-setup" replace />;
}
