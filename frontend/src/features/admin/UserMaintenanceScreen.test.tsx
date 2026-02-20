import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { UserMaintenanceScreen } from './UserMaintenanceScreen.tsx';
import { adminUserApi, siteApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminUserApi: {
    getAll: vi.fn(),
    getRoles: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <UserMaintenanceScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockUsers = [
  {
    id: '1',
    employeeNumber: 'EMP001',
    firstName: 'John',
    lastName: 'Doe',
    displayName: 'John Doe',
    roleTier: 1,
    roleName: 'Admin',
    defaultSiteId: 's1',
    defaultSiteName: 'Plant 1',
    isCertifiedWelder: false,
    requirePinForLogin: false,
    userType: 0,
    isActive: true,
  },
];

const mockRoles = [
  { name: 'Administrator', tier: 1 },
  { name: 'Operator', tier: 6 },
];

const mockSites = [{ id: 's1', code: 'PLT1', name: 'Plant 1' }];

describe('UserMaintenanceScreen', () => {
  beforeEach(() => {
    vi.mocked(adminUserApi.getAll).mockResolvedValue(mockUsers);
    vi.mocked(adminUserApi.getRoles).mockResolvedValue(mockRoles);
    vi.mocked(siteApi.getSites).mockResolvedValue(mockSites);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockUsers) => void;
    let resolveGetRoles!: (v: typeof mockRoles) => void;
    let resolveGetSites!: (v: typeof mockSites) => void;
    vi.mocked(adminUserApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    vi.mocked(adminUserApi.getRoles).mockImplementation(
      () => new Promise((r) => { resolveGetRoles = r; }),
    );
    vi.mocked(siteApi.getSites).mockImplementation(
      () => new Promise((r) => { resolveGetSites = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockUsers);
    resolveGetRoles(mockRoles);
    resolveGetSites(mockSites);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });
    expect(screen.getByText('Plant 1')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminUserApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No users found.')).toBeInTheDocument();
    });
  });

  it('shows Add User button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add User/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('User Maintenance')).toBeInTheDocument();
  });

  it('renders search box for filtering', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });
    expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument();
  });
});
