import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { FitupQueueScreen } from './FitupQueueScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { productApi, vendorApi, workCenterApi, materialQueueApi, barcodeCardApi } from '../../api/endpoints';

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

  it('shows lot number in queue card when lot exists', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-lot-1',
        position: 1,
        status: 'queued',
        productDescription: 'HEAD 500',
        shellSize: '500',
        heatNumber: '',
        coilNumber: '',
        lotNumber: 'LOT-777',
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '01',
        cardColor: 'Blue',
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    expect(await screen.findByText('Lot LOT-777')).toBeInTheDocument();
  });

  it('shows heat and coil/slab in queue card when lot is absent', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-heat-1',
        position: 1,
        status: 'queued',
        productDescription: 'HEAD 500',
        shellSize: '500',
        heatNumber: 'H-123',
        coilNumber: 'C-456',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '02',
        cardColor: 'Red',
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    expect(await screen.findByText(/Heat H-123\s+Coil\/Slab C-456/i)).toBeInTheDocument();
  });

  it('shows only queued rows in Fitup queue list', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-queued',
        position: 1,
        status: 'queued',
        productDescription: 'Queued Head 500',
        shellSize: '500',
        heatNumber: 'H-100',
        coilNumber: 'C-100',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '03',
        cardColor: 'Red',
        createdAt: new Date().toISOString(),
      },
      {
        id: 'q-active',
        position: 2,
        status: 'active',
        productDescription: 'Active Head 500',
        shellSize: '500',
        heatNumber: 'H-200',
        coilNumber: 'C-200',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '04',
        cardColor: 'Blue',
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    expect(await screen.findByText(/queued head 500/i)).toBeInTheDocument();
    expect(screen.queryByText(/active head 500/i)).not.toBeInTheDocument();
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
      expect(barcodeCardApi.getCards).toHaveBeenCalledWith('wc-fitup-q', undefined);
    });
  });

  it('fetches barcode cards using work center and plantId', async () => {
    renderScreen();
    await waitFor(() => {
      expect(barcodeCardApi.getCards).toHaveBeenCalledWith('wc-fitup-q', '11111111-1111-1111-1111-111111111111');
    });
  });

  it('uses materialQueueForWCId for queue read when present', async () => {
    renderScreen({ workCenterId: 'wc-current', materialQueueForWCId: 'wc-feed', productionLineId: 'pl-inside' });

    await waitFor(() => {
      expect(workCenterApi.getMaterialQueue).toHaveBeenCalledWith('wc-feed', 'fitup', 'pl-inside');
    });
    expect(barcodeCardApi.getCards).toHaveBeenCalledWith('wc-feed', '11111111-1111-1111-1111-111111111111');
  });

  it('uses materialQueueForWCId for queue save when present', async () => {
    vi.mocked(productApi.getProducts).mockResolvedValueOnce([
      {
        id: 'prod-1',
        productNumber: 'HEAD 500',
        tankSize: 500,
        tankType: 'Head',
      },
    ]);
    vi.mocked(vendorApi.getVendors).mockResolvedValueOnce([
      {
        id: 'vendor-1',
        name: 'Compco Industries',
        vendorType: 'head',
      },
    ]);
    vi.mocked(barcodeCardApi.getCards).mockResolvedValueOnce([
      { id: 'card-1', cardValue: '01', color: '#0000ff', colorName: 'Blue', isAssigned: false },
    ]);
    vi.mocked(materialQueueApi.addFitupItem).mockResolvedValueOnce({
      id: 'q-1',
      position: 1,
      status: 'queued',
      productDescription: 'HEAD 500',
      shellSize: '500',
      heatNumber: 'H1',
      coilNumber: 'C1',
      lotNumber: undefined,
      quantity: 1,
      quantityCompleted: 0,
      productId: 'prod-1',
      vendorHeadId: 'vendor-1',
      cardId: '01',
      cardColor: 'Blue',
      createdAt: new Date().toISOString(),
    });

    const registerBarcodeHandler = vi.fn();
    renderScreen({
      workCenterId: 'wc-current',
      materialQueueForWCId: 'wc-feed',
      productionLineId: 'pl-inside',
      registerBarcodeHandler,
    });

    fireEvent.click(await screen.findByRole('button', { name: /add material to queue/i }));
    fireEvent.click(screen.getByRole('button', { name: /select product/i }));
    fireEvent.click(await screen.findByRole('button', { name: /\(500\)\s*HEAD 500/i }));
    fireEvent.click(screen.getByRole('button', { name: /select vendor/i }));
    fireEvent.click(await screen.findByRole('button', { name: /compco industries/i }));

    await waitFor(() => {
      expect(registerBarcodeHandler).toHaveBeenCalled();
    });
    const handler = registerBarcodeHandler.mock.calls[0][0] as (bc: { prefix: 'KC'; value: string; raw: string }, raw: string) => void;
    act(() => {
      handler({ prefix: 'KC', value: '01', raw: 'KC;01' }, 'KC;01');
    });

    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(materialQueueApi.addFitupItem).toHaveBeenCalledWith(
        'wc-feed',
        expect.objectContaining({
          productionLineId: 'pl-inside',
          cardCode: '01',
        }),
      );
    });
  });

  it('disables add button when queue is full', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce(
      Array.from({ length: 5 }, (_, i) => ({
        id: `q-${i}`,
        position: i + 1,
        status: 'queued',
        productDescription: 'HEAD',
        shellSize: '500',
        heatNumber: `H${i}`,
        coilNumber: `C${i}`,
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: `${i + 1}`,
        cardColor: 'Blue',
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
        productDescription: 'HEAD',
        shellSize: '500',
        heatNumber: 'H1',
        coilNumber: 'C1',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '01',
        cardColor: 'Blue',
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    const deleteButton = await screen.findByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    expect(screen.getByText(/remove from queue\?/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }));
    expect(materialQueueApi.deleteFitupItem).not.toHaveBeenCalled();
  });

  it('deletes when confirmation modal is accepted', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-1',
        position: 1,
        status: 'queued',
        productDescription: 'HEAD',
        shellSize: '500',
        heatNumber: 'H1',
        coilNumber: 'C1',
        lotNumber: undefined,
        quantity: 1,
        quantityCompleted: 0,
        productId: undefined,
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '01',
        cardColor: 'Blue',
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    const deleteButton = await screen.findByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);
    fireEvent.click(screen.getByRole('button', { name: /yes, remove/i }));

    await waitFor(() => {
      expect(materialQueueApi.deleteFitupItem).toHaveBeenCalledWith('wc-fitup-q', 'q-1');
    });
  });

  it('pre-populates edit dialog with selected queue item values', async () => {
    vi.mocked(barcodeCardApi.getCards).mockResolvedValueOnce([
      { id: 'card-1', cardValue: '01', color: '#ff0000', colorName: undefined, isAssigned: false },
    ]);
    vi.mocked(productApi.getProducts).mockResolvedValueOnce([
      {
        id: 'prod-1',
        productNumber: 'HEAD 500',
        tankSize: 500,
        tankType: 'Head',
      },
    ]);
    vi.mocked(vendorApi.getVendors).mockResolvedValueOnce([
      {
        id: 'vendor-1',
        name: 'Commercial Metal Forming (CMF)',
        vendorType: 'head',
      },
    ]);
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'q-1',
        position: 1,
        status: 'queued',
        productDescription: 'HEAD 500',
        shellSize: '500',
        heatNumber: 'H1',
        coilNumber: 'C1',
        lotNumber: 'LOT-123',
        quantity: 1,
        quantityCompleted: 0,
        productId: 'prod-1',
        vendorHeadId: 'vendor-1',
        vendorMillId: undefined,
        vendorProcessorId: undefined,
        cardId: '01',
        cardColor: 'Red',
        createdAt: new Date().toISOString(),
      },
    ]);

    renderScreen();

    const editButton = await screen.findByRole('button', { name: /edit/i });
    fireEvent.click(editButton);

    expect(await screen.findByRole('button', { name: /\(500\)\s+HEAD 500/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /commercial metal forming \(cmf\)/i })).toBeInTheDocument();
    expect(screen.getByDisplayValue('LOT-123')).toBeInTheDocument();
    expect(screen.getByText('01 - Red')).toBeInTheDocument();
  });

  it('auto-selects queue card dropdown when KC barcode is scanned', async () => {
    const registerBarcodeHandler = vi.fn();
    const showScanResult = vi.fn();
    vi.mocked(barcodeCardApi.getCards).mockResolvedValueOnce([
      { id: 'card-1', cardValue: '01', color: '#ff0000', colorName: 'Red', isAssigned: false },
      { id: 'card-2', cardValue: '02', color: '#ffff00', colorName: 'Yellow', isAssigned: false },
    ]);

    renderScreen({ registerBarcodeHandler, showScanResult });
    fireEvent.click(screen.getByRole('button', { name: /add material to queue/i }));

    await waitFor(() => {
      expect(registerBarcodeHandler).toHaveBeenCalled();
    });
    const handler = registerBarcodeHandler.mock.calls[0][0] as (bc: { prefix: 'KC'; value: string; raw: string }, raw: string) => void;
    act(() => {
      handler({ prefix: 'KC', value: '02', raw: 'KC;02' }, 'KC;02');
    });

    const queueCardField = screen.getByRole('combobox');
    await waitFor(() => {
      expect(queueCardField).toHaveTextContent(/02|yellow/i);
    });
    expect(showScanResult).toHaveBeenCalledWith({ type: 'success', message: 'Card 02 scanned' });
  });

});
