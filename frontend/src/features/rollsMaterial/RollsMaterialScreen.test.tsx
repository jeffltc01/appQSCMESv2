import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RollsMaterialScreen } from './RollsMaterialScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { productApi, vendorApi, workCenterApi, materialQueueApi } from '../../api/endpoints';

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
    plantId: 'plant-1', welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
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

  it('disables add button when queue is full', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce(
      Array.from({ length: 5 }, (_, i) => ({
        id: `q-${i}`,
        position: i + 1,
        status: 'queued',
        productDescription: 'PLATE',
        shellSize: '250',
        heatNumber: `H${i}`,
        coilNumber: `C${i}`,
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: undefined,
        cardColor: undefined,
        createdAt: new Date().toISOString(),
      })),
    );

    renderScreen();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add material to queue/i })).toBeDisabled();
    });
    expect(screen.getByText(/queue is full \(max 5 items\)/i)).toBeInTheDocument();
  });

  it('shows delete confirmation modal and cancels delete', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-1',
        position: 1,
        status: 'queued',
        productDescription: 'PLATE',
        shellSize: '250',
        heatNumber: 'H1',
        coilNumber: 'C1',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: undefined,
        cardColor: undefined,
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    const deleteButton = await screen.findByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    expect(screen.getByText(/remove from queue\?/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }));
    expect(materialQueueApi.deleteItem).not.toHaveBeenCalled();
  });

  it('deletes when confirmation modal is accepted', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-1',
        position: 1,
        status: 'queued',
        productDescription: 'PLATE',
        shellSize: '250',
        heatNumber: 'H1',
        coilNumber: 'C1',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: undefined,
        cardColor: undefined,
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    const deleteButton = await screen.findByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);
    fireEvent.click(screen.getByRole('button', { name: /yes, remove/i }));

    await waitFor(() => {
      expect(materialQueueApi.deleteItem).toHaveBeenCalledWith('wc-rolls-mat', 'q-1');
    });
  });
});
