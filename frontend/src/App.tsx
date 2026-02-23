import { useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './auth/AuthContext.tsx';
import { LoginScreen } from './components/login/LoginScreen.tsx';
import { TabletSetupScreen } from './components/tabletSetup/TabletSetupScreen.tsx';
import { OperatorLayout } from './components/layout/OperatorLayout.tsx';
import { MenuScreen } from './features/menu/MenuScreen.tsx';
import { AdminRoutes } from './features/admin/AdminRoutes.tsx';
import { reportTelemetry } from './telemetry/telemetryClient.ts';

export function App() {
  const { isAuthenticated, user } = useAuth();

  if (!isAuthenticated) {
    return (
      <Routes>
        <Route path="*" element={<LoginScreen />} />
      </Routes>
    );
  }

  return (
    <Routes>
      <Route path="/login" element={<LoginScreen />} />
      <Route path="/menu" element={<MenuScreen />} />
      <Route path="/menu/*" element={<AdminRoutes />} />
      <Route path="/tablet-setup" element={<TabletSetupScreen />} />
      <Route path="/operator/*" element={<OperatorLayout />} />
      <Route
        path="*"
        element={
          <DefaultRoute roleTier={user?.roleTier ?? 6} />
        }
      />
    </Routes>
  );
}

function DefaultRoute({ roleTier }: { roleTier: number }) {
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

  if (roleTier < 6) {
    return <Navigate to="/menu" replace />;
  }

  const cachedWcId = localStorage.getItem('cachedWorkCenterId');
  if (cachedWcId) {
    return <Navigate to="/operator" replace />;
  }
  return <Navigate to="/tablet-setup" replace />;
}
