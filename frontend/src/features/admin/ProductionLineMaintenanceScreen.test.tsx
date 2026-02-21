import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ProductionLineMaintenanceScreen } from './ProductionLineMaintenanceScreen.tsx';
import { adminProductionLineApi, siteApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1, defaultSiteId: 's1' };
const tier3User = { plantCode: 'PLT1', plantName: 'Plant 1', displayName: 'QM User', roleTier: 3, defaultSiteId: 's1' };

vi.mock('../../api/endpoints.ts', () => ({
  adminProductionLineApi: {
    getAll: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <ProductionLineMaintenanceScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockLines = [
  {
    id: '1',
    name: 'Line A',
    plantId: 's1',
    plantName: 'Plant 1',
  },
];

const mockSites = [
  { id: 's1', code: 'PLT1', name: 'Plant 1', timeZoneId: 'America/Chicago' },
  { id: 's2', code: 'PLT2', name: 'Plant 2', timeZoneId: 'America/New_York' },
];

describe('ProductionLineMaintenanceScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminProductionLineApi.getAll).mockResolvedValue(mockLines);
    vi.mocked(siteApi.getSites).mockResolvedValue(mockSites);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockLines) => void;
    let resolveGetSites!: (v: typeof mockSites) => void;
    vi.mocked(adminProductionLineApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    vi.mocked(siteApi.getSites).mockImplementation(
      () => new Promise((r) => { resolveGetSites = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockLines);
    resolveGetSites(mockSites);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Line A')).toBeInTheDocument();
    });
    expect(screen.getByText('Plant 1')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminProductionLineApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No production lines found.')).toBeInTheDocument();
    });
  });

  it('shows Add Production Line button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Production Line/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Production Line Maintenance')).toBeInTheDocument();
  });

  it('renders search box for filtering', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Line A')).toBeInTheDocument();
    });
    expect(screen.getByPlaceholderText(/search/i)).toBeInTheDocument();
  });

  describe('Tier 3 site-scoped behavior', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
    });

    it('only shows production lines from the same site', async () => {
      const multiSiteLines = [
        ...mockLines,
        {
          id: '2',
          name: 'Line B',
          plantId: 's2',
          plantName: 'Plant 2',
        },
      ];
      vi.mocked(adminProductionLineApi.getAll).mockResolvedValue(multiSiteLines);
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText('Line A')).toBeInTheDocument();
      });
      expect(screen.queryByText('Line B')).not.toBeInTheDocument();
    });
  });
});
