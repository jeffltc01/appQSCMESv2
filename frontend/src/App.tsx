import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './auth/AuthContext.tsx';
import { LoginScreen } from './components/login/LoginScreen.tsx';
import { TabletSetupScreen } from './components/tabletSetup/TabletSetupScreen.tsx';
import { OperatorLayout } from './components/layout/OperatorLayout.tsx';

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

function DefaultRoute({ roleTier: _roleTier }: { roleTier: number }) {
  const cachedWcId = localStorage.getItem('cachedWorkCenterId');

  if (cachedWcId) {
    return <Navigate to="/operator" replace />;
  }
  return <Navigate to="/tablet-setup" replace />;
}
