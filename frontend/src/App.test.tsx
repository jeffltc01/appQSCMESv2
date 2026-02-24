import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { App } from './App';

const mockUseAuth = vi.fn();
const mockUseIsPhoneViewport = vi.fn();

vi.mock('./auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('./telemetry/telemetryClient.ts', () => ({
  reportTelemetry: vi.fn(),
}));

vi.mock('./hooks/useIsPhoneViewport.ts', () => ({
  useIsPhoneViewport: () => mockUseIsPhoneViewport(),
}));

vi.mock('./components/login/LoginScreen.tsx', () => ({
  LoginScreen: () => <div data-testid="login-screen">Login</div>,
}));

vi.mock('./components/tabletSetup/TabletSetupScreen.tsx', () => ({
  TabletSetupScreen: () => <div data-testid="tablet-setup-screen">Tablet Setup</div>,
}));

vi.mock('./components/layout/OperatorLayout.tsx', () => ({
  OperatorLayout: () => <div data-testid="operator-layout">Operator Layout</div>,
}));

vi.mock('./features/menu/MenuScreen.tsx', () => ({
  MenuScreen: () => <div data-testid="menu-screen">Menu</div>,
}));

vi.mock('./features/admin/AdminRoutes.tsx', () => ({
  AdminRoutes: () => <div data-testid="admin-routes">Admin Routes</div>,
}));

vi.mock('./features/mobile/MobileRoutes.tsx', () => ({
  MobileRoutes: () => <div data-testid="mobile-routes">Mobile Routes</div>,
}));

function setAuthState(isAuthenticated: boolean, roleTier = 6, roleName = 'Role') {
  mockUseAuth.mockReturnValue({
    isAuthenticated,
    user: isAuthenticated
      ? { roleTier, roleName }
      : null,
  });
}

function setViewport(phone: boolean) {
  mockUseIsPhoneViewport.mockReturnValue(phone);
}

function renderApp(path = '/') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  );
}

describe('App kiosk routing', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    setViewport(false);
  });

  it('renders login screen for unauthenticated users', () => {
    setAuthState(false);
    renderApp('/menu');
    expect(screen.getByTestId('login-screen')).toBeInTheDocument();
  });

  it('redirects operator away from menu when work center is cached', () => {
    localStorage.setItem('cachedWorkCenterId', 'wc-1');
    setAuthState(true, 6);
    renderApp('/menu');
    expect(screen.getByTestId('operator-layout')).toBeInTheDocument();
  });

  it('redirects operator away from menu to tablet setup when no cache exists', () => {
    setAuthState(true, 6);
    renderApp('/menu');
    expect(screen.getByTestId('tablet-setup-screen')).toBeInTheDocument();
  });

  it('redirects operator away from tablet setup when work center is cached', () => {
    localStorage.setItem('cachedWorkCenterId', 'wc-1');
    setAuthState(true, 6);
    renderApp('/tablet-setup');
    expect(screen.getByTestId('operator-layout')).toBeInTheDocument();
  });

  it('allows non-operator roles to access menu routes', () => {
    setAuthState(true, 4);
    renderApp('/menu');
    expect(screen.getByTestId('menu-screen')).toBeInTheDocument();
  });

  it('routes director phone users to mobile routes', () => {
    setViewport(true);
    setAuthState(true, 2, 'Quality Director');
    renderApp('/menu');
    expect(screen.getByTestId('mobile-routes')).toBeInTheDocument();
  });

  it('keeps non-director phone users on menu routes', () => {
    setViewport(true);
    setAuthState(true, 4, 'Team Lead');
    renderApp('/menu');
    expect(screen.getByTestId('menu-screen')).toBeInTheDocument();
  });

  it('routes kiosk phone users to operator quick actions route', () => {
    setViewport(true);
    setAuthState(true, 6);
    renderApp('/operator');
    expect(screen.getByTestId('mobile-routes')).toBeInTheDocument();
  });
});
