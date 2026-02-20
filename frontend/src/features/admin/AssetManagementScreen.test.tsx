import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AssetManagementScreen } from './AssetManagementScreen.tsx';
import { adminAssetApi, adminWorkCenterApi, productionLineApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminAssetApi: {
    getAll: vi.fn(),
  },
  adminWorkCenterApi: {
    getAll: vi.fn(),
  },
  productionLineApi: {
    getAll: vi.fn(),
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

describe('AssetManagementScreen', () => {
  beforeEach(() => {
    vi.mocked(adminAssetApi.getAll).mockResolvedValue(mockAssets);
    vi.mocked(adminWorkCenterApi.getAll).mockResolvedValue(mockWorkCenters);
    vi.mocked(productionLineApi.getAll).mockResolvedValue(mockProductionLines);
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
    expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    expect(screen.getByText('LMB-001')).toBeInTheDocument();
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
});
