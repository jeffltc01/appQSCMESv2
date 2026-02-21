import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RollsMaterialScreen } from './RollsMaterialScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { productApi, vendorApi } from '../../api/endpoints';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: (...args: unknown[]) => mockUseAuth(...args),
}));

vi.mock('../../api/endpoints', () => ({
  workCenterApi: { getMaterialQueue: vi.fn().mockResolvedValue([]) },
  materialQueueApi: { addItem: vi.fn(), updateItem: vi.fn(), deleteItem: vi.fn() },
  productApi: { getProducts: vi.fn().mockResolvedValue([]) },
  vendorApi: { getVendors: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rolls-mat', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], numberOfWelders: 0, externalInput: false,
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><RollsMaterialScreen {...props} /></FluentProvider>) };
}

describe('RollsMaterialScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { defaultSiteId: '11111111-1111-1111-1111-111111111111', plantName: 'Cleveland', displayName: 'Test User' },
      logout: vi.fn(),
    });
  });

  it('renders queue header', () => {
    renderScreen();
    expect(screen.getByText(/material queue for: rolls/i)).toBeInTheDocument();
  });

  it('shows add button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /add material to queue/i })).toBeInTheDocument();
  });

  it('shows refresh button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /refresh/i })).toBeInTheDocument();
  });

  it('shows empty queue message', () => {
    renderScreen();
    expect(screen.getByText(/no material in queue/i)).toBeInTheDocument();
  });

  it('fetches vendors with correct type and plantId', async () => {
    renderScreen();
    await waitFor(() => {
      expect(vendorApi.getVendors).toHaveBeenCalledWith('mill', '11111111-1111-1111-1111-111111111111');
      expect(vendorApi.getVendors).toHaveBeenCalledWith('processor', '11111111-1111-1111-1111-111111111111');
    });
  });

  it('fetches products with correct type and plantId', async () => {
    renderScreen();
    await waitFor(() => {
      expect(productApi.getProducts).toHaveBeenCalledWith('plate', '11111111-1111-1111-1111-111111111111');
    });
  });

  it('passes undefined plantId when defaultSiteId is missing', async () => {
    mockUseAuth.mockReturnValue({
      user: { plantName: 'Unknown', displayName: 'Test User' },
      logout: vi.fn(),
    });
    renderScreen();
    await waitFor(() => {
      expect(vendorApi.getVendors).toHaveBeenCalledWith('mill', undefined);
      expect(vendorApi.getVendors).toHaveBeenCalledWith('processor', undefined);
      expect(productApi.getProducts).toHaveBeenCalledWith('plate', undefined);
    });
  });
});
