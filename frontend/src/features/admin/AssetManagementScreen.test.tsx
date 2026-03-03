import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AssetManagementScreen } from './AssetManagementScreen.tsx';
import { adminAssetApi, adminWorkCenterApi, productionLineApi, siteApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1, defaultSiteId: 'p1' };

vi.mock('../../api/endpoints.ts', () => ({
  adminAssetApi: {
    getAll: vi.fn(),
    remove: vi.fn(),
  },
  adminWorkCenterApi: {
    getAll: vi.fn(),
  },
  productionLineApi: {
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
        <AssetManagementScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockAssets = [
  {
    id: '1',
    name: 'Asset 1',
    workCenterId: 'wc1',
    workCenterName: 'Rolls 1',
    productionLineId: 'pl1',
    productionLineName: 'Line 1 (Cleveland)',
    limbleIdentifier: 'LMB-001',
    laneName: 'Lane A',
    isActive: true,
  },
];

const mockWorkCenters = [
  {
    id: 'wc1',
    name: 'Rolls 1',
    workCenterTypeName: 'Rolls',
    productionLineName: 'Line 1',
    numberOfWelders: 2,
    dataEntryType: 'standard',
  },
];

const mockProductionLines = [
  {
    id: 'pl1',
    name: 'Line 1',
    plantId: 'p1',
    plantName: 'Cleveland',
  },
];

const mockSites = [
  { id: 'p1', code: 'PLT1', name: 'Cleveland', timeZoneId: 'America/Chicago' },
];

describe('AssetManagementScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminAssetApi.getAll).mockResolvedValue(mockAssets);
    vi.mocked(adminAssetApi.remove).mockResolvedValue(undefined);
    vi.mocked(adminWorkCenterApi.getAll).mockResolvedValue(mockWorkCenters);
    vi.mocked(productionLineApi.getAll).mockResolvedValue(mockProductionLines);
    vi.mocked(siteApi.getSites).mockResolvedValue(mockSites);
  });

  it('renders loading state initially', async () => {
    let resolveAssets!: (v: typeof mockAssets) => void;
    let resolveWcs!: (v: typeof mockWorkCenters) => void;
    let resolvePls!: (v: typeof mockProductionLines) => void;
    vi.mocked(adminAssetApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveAssets = r; }),
    );
    vi.mocked(adminWorkCenterApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveWcs = r; }),
    );
    vi.mocked(productionLineApi.getAll).mockImplementation(
      () => new Promise((r) => { resolvePls = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveAssets(mockAssets);
    resolveWcs(mockWorkCenters);
    resolvePls(mockProductionLines);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Asset 1')).toBeInTheDocument();
    });
    expect(adminAssetApi.getAll).toHaveBeenCalledWith(undefined);
    expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    expect(screen.getByText('LMB-001')).toBeInTheDocument();
    expect(screen.getByText('Lane A')).toBeInTheDocument();
  });

  it('shows production line name on card', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Asset 1')).toBeInTheDocument();
    });
    expect(screen.getByText('Line 1 (Cleveland)')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminAssetApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No assets found.')).toBeInTheDocument();
    });
  });

  it('shows Add Asset button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Asset/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Asset Management')).toBeInTheDocument();
  });

  it('requests filtered assets when site changes', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Asset 1')).toBeInTheDocument();
    });

    const siteSelect = screen.getByRole('combobox');
    fireEvent.change(siteSelect, { target: { value: 'p1' } });

    await waitFor(() => {
      expect(adminAssetApi.getAll).toHaveBeenLastCalledWith('p1');
    });
  });

  it('deactivates an active asset from confirmation dialog', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Asset 1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Deactivate Asset 1' }));
    const dialog = await screen.findByRole('dialog', { hidden: true });
    expect(within(dialog).getByText('Confirm Deactivation')).toBeInTheDocument();

    fireEvent.click(within(dialog).getByRole('button', { name: 'Deactivate', hidden: true }));

    await waitFor(() => {
      expect(adminAssetApi.remove).toHaveBeenCalledWith('1');
    });
    await waitFor(() => {
      expect(screen.getByText('Inactive')).toBeInTheDocument();
    });
  });
});
