import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { FitupQueueScreen } from './FitupQueueScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { productApi, vendorApi } from '../../api/endpoints';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: (...args: unknown[]) => mockUseAuth(...args),
}));

vi.mock('../../api/endpoints', () => ({
  workCenterApi: { getMaterialQueue: vi.fn().mockResolvedValue([]) },
  materialQueueApi: { addFitupItem: vi.fn(), updateFitupItem: vi.fn(), deleteFitupItem: vi.fn() },
  productApi: { getProducts: vi.fn().mockResolvedValue([]) },
  vendorApi: { getVendors: vi.fn().mockResolvedValue([]) },
  barcodeCardApi: { getCards: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-fitup-q', plantId: 'plant-1', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><FitupQueueScreen {...props} /></FluentProvider>) };
}

describe('FitupQueueScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { defaultSiteId: '11111111-1111-1111-1111-111111111111', plantName: 'Cleveland', displayName: 'Test User' },
      logout: vi.fn(),
    });
  });

  it('renders queue header', () => {
    renderScreen();
    expect(screen.getByText(/material queue for: fitup/i)).toBeInTheDocument();
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('shows add button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /add material to queue/i })).toBeInTheDocument();
  });

  it('shows empty queue message', () => {
    renderScreen();
    expect(screen.getByText(/no material in queue/i)).toBeInTheDocument();
  });

  it('fetches vendors with correct type and plantId', async () => {
    renderScreen();
    await waitFor(() => {
      expect(vendorApi.getVendors).toHaveBeenCalledWith('head', '11111111-1111-1111-1111-111111111111');
    });
  });

  it('fetches products with correct type and plantId', async () => {
    renderScreen();
    await waitFor(() => {
      expect(productApi.getProducts).toHaveBeenCalledWith('head', '11111111-1111-1111-1111-111111111111');
    });
  });

  it('passes undefined plantId when defaultSiteId is missing', async () => {
    mockUseAuth.mockReturnValue({
      user: { plantName: 'Unknown', displayName: 'Test User' },
      logout: vi.fn(),
    });
    renderScreen();
    await waitFor(() => {
      expect(vendorApi.getVendors).toHaveBeenCalledWith('head', undefined);
      expect(productApi.getProducts).toHaveBeenCalledWith('head', undefined);
    });
  });
});
