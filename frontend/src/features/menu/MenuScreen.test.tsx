import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { MenuScreen } from './MenuScreen';
import { adminAnnotationApi, frontendTelemetryApi, issueRequestApi } from '../../api/endpoints';

vi.mock('../../api/endpoints');
vi.mock('../../help/helpRegistry', () => ({ getArticleBySlug: vi.fn() }));
vi.mock('../../help/components/HelpButton', () => ({ HelpButton: () => null }));

const baseUser = {
  id: 'u1',
  displayName: 'Test User',
  roleTier: 1,
  roleName: 'Admin',
  defaultSiteId: 'site-1',
  employeeNumber: 'EMP001',
  plantCode: 'TST',
  plantName: 'Test Plant',
  plantTimeZoneId: 'America/Denver',
  isCertifiedWelder: false,
  userType: 0,
};

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext', () => ({ useAuth: () => mockUseAuth() }));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <MenuScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

function authValue(overrides: Partial<typeof baseUser> = {}) {
  return {
    user: { ...baseUser, ...overrides },
    logout: vi.fn(),
    login: vi.fn(),
    isAuthenticated: true,
    token: 'test-token',
    isWelder: false,
  };
}

describe('MenuScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(frontendTelemetryApi.getCount).mockResolvedValue({
      rowCount: 0,
      warningThreshold: 250000,
      isWarning: false,
    });
    vi.mocked(issueRequestApi.getPending).mockResolvedValue([]);
    vi.mocked(adminAnnotationApi.getAll).mockResolvedValue([]);
  });

  it('admin (roleTier 1) sees all tiles including Work Centers screens and User Maintenance', () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 1 }));
    renderScreen();

    expect(screen.getByText('Work Centers')).toBeInTheDocument();
    expect(screen.getByText('Production Line Work Centers')).toBeInTheDocument();
    expect(screen.getByText('User Maintenance')).toBeInTheDocument();
    expect(screen.getByText('Product Maintenance')).toBeInTheDocument();
    expect(screen.getByText('Vendor Maintenance')).toBeInTheDocument();
    expect(screen.getByText('Defect Codes')).toBeInTheDocument();
    expect(screen.getByText('Kanban Card Mgmt')).toBeInTheDocument();
    expect(screen.getByText('Log Viewer')).toBeInTheDocument();
    expect(screen.getByText('AI Review')).toBeInTheDocument();
    expect(screen.getByText('Operator View')).toBeInTheDocument();
    expect(screen.getByText('Checklist Score Types')).toBeInTheDocument();
  });

  it('operator (roleTier 7) sees only Log Viewer from Dashboards & Insights', () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 7 }));
    renderScreen();

    expect(screen.getByText('Log Viewer')).toBeInTheDocument();

    expect(screen.queryByText('Product Maintenance')).not.toBeInTheDocument();
    expect(screen.queryByText('Work Centers')).not.toBeInTheDocument();
    expect(screen.queryByText('Production Line Work Centers')).not.toBeInTheDocument();
    expect(screen.queryByText('User Maintenance')).not.toBeInTheDocument();
    expect(screen.queryByText('Defect Codes')).not.toBeInTheDocument();
    expect(screen.queryByText('Kanban Card Mgmt')).not.toBeInTheDocument();
    expect(screen.queryByText('AI Review')).not.toBeInTheDocument();
  });

  it('roleTier 5 user sees appropriate tiles', () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 5 }));
    renderScreen();

    expect(screen.getByText('Kanban Card Mgmt')).toBeInTheDocument();
    expect(screen.getByText('Serial Number Lookup')).toBeInTheDocument();
    expect(screen.getByText('Where Used')).toBeInTheDocument();
    expect(screen.getByText('Downtime Log')).toBeInTheDocument();
    expect(screen.getByText('Supervisor / Team Lead Dashboard')).toBeInTheDocument();
    expect(screen.getByText("Who's On the Floor")).toBeInTheDocument();
    expect(screen.getByText('Issues')).toBeInTheDocument();
    expect(screen.getByText('Operator View')).toBeInTheDocument();
    expect(screen.getByText('Log Viewer')).toBeInTheDocument();
  });

  it('shows pending approvals count on Issues tile for approvers', async () => {
    vi.mocked(issueRequestApi.getPending).mockResolvedValue([
      { id: 'a' } as never,
      { id: 'b' } as never,
    ]);
    mockUseAuth.mockReturnValue(authValue({ roleTier: 3 }));
    renderScreen();
    expect(await screen.findByLabelText('Issues pending approval count: 2')).toBeInTheDocument();
  });

  it('shows unresolved annotation count on Annotations tile for approvers', async () => {
    vi.mocked(adminAnnotationApi.getAll).mockResolvedValue([
      { id: 'a1', resolvedByName: undefined } as never,
      { id: 'a2', resolvedByName: 'Admin User' } as never,
      { id: 'a3', resolvedByName: undefined } as never,
    ]);
    mockUseAuth.mockReturnValue(authValue({ roleTier: 3 }));
    renderScreen();
    expect(await screen.findByLabelText('Annotations needing response count: 2')).toBeInTheDocument();
    expect(adminAnnotationApi.getAll).toHaveBeenCalledWith('site-1');
  });

  it('roleTier 5.5 user sees AI Review tile', () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 5.5 }));
    renderScreen();

    expect(screen.getByText('AI Review')).toBeInTheDocument();
  });

  it('roleTier 3 user does NOT see AI Review tile', () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 3 }));
    renderScreen();

    expect(screen.queryByText('AI Review')).not.toBeInTheDocument();
  });

  it('renders Logout button', () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 1 }));
    renderScreen();

    expect(screen.getByRole('button', { name: /logout/i })).toBeInTheDocument();
  });

  it('shows archive warning badge for frontend telemetry tile', async () => {
    vi.mocked(frontendTelemetryApi.getCount).mockResolvedValue({
      rowCount: 300000,
      warningThreshold: 250000,
      isWarning: true,
    });
    mockUseAuth.mockReturnValue(authValue({ roleTier: 1 }));

    renderScreen();

    expect(await screen.findByText('Archive Needed')).toBeInTheDocument();
  });
});
