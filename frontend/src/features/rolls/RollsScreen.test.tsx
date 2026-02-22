import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RollsScreen } from './RollsScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  workCenterApi: {
    getMaterialQueue: vi.fn().mockResolvedValue([]),
    advanceQueue: vi.fn(),
    reportFault: vi.fn().mockResolvedValue(undefined),
  },
  productionRecordApi: {
    create: vi.fn(),
  },
  controlPlanApi: { getForWorkCenter: vi.fn().mockResolvedValue([]) },
  inspectionRecordApi: { create: vi.fn().mockResolvedValue({ id: 'ir-1', serialNumber: 'test', defects: [] }) },
}));

const { workCenterApi } = await import('../../api/endpoints');

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rolls',
    assetId: 'asset-1',
    productionLineId: 'pl-1',
    operatorId: 'op-1',
    plantId: 'plant-1',
    welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    numberOfWelders: 1,
    welderCountLoaded: true,
    externalInput: false,
    setExternalInput: vi.fn(),
    showScanResult: vi.fn(),
    refreshHistory: vi.fn(),
    registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderRolls(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return {
    props,
    ...render(
      <FluentProvider theme={webLightTheme}>
        <RollsScreen {...props} />
      </FluentProvider>,
    ),
  };
}

describe('RollsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders idle state prompting to advance queue', () => {
    renderRolls();
    expect(screen.getByText(/advance the material queue/i)).toBeInTheDocument();
  });

  it('displays material queue items', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValue([
      { id: 'q1', position: 1, status: 'queued', productDescription: 'PL .218NOM X 83.00', shellSize: '120', heatNumber: 'H1', coilNumber: 'C1', quantity: 15, createdAt: '' },
    ]);

    renderRolls();
    await waitFor(() => {
      expect(screen.getByText(/\(120\).*PL \.218NOM X 83\.00/)).toBeInTheDocument();
      expect(screen.getByText('15')).toBeInTheDocument();
    });
  });

  it('shows data grid after queue advance', async () => {
    vi.mocked(workCenterApi.advanceQueue).mockResolvedValue({
      shellSize: '120',
      heatNumber: 'HEAT001',
      coilNumber: 'COIL001',
      quantity: 10,
      quantityCompleted: 0,
      productDescription: 'PL .218',
    });

    const { props } = renderRolls();

    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    expect(handler).toBeDefined();

    // Simulate INP;2 scan to advance queue
    if (handler) {
      handler({ prefix: 'INP', value: '2', raw: 'INP;2' }, 'INP;2');
    }

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalled();
    });
  });

  it('registers barcode handler on mount', () => {
    const { props } = renderRolls();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('has manual submit button in manual mode', () => {
    renderRolls({ externalInput: false });
    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument();
  });

  it('hides manual entry in external input mode', () => {
    renderRolls({ externalInput: true });
    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument();
  });

  it('shows refresh button for queue', () => {
    renderRolls();
    expect(screen.getByRole('button', { name: /refresh/i })).toBeInTheDocument();
  });

  it('shows both serials when labels do not match', async () => {
    vi.mocked(workCenterApi.advanceQueue).mockResolvedValue({
      shellSize: '120',
      heatNumber: 'H1',
      coilNumber: 'C1',
      quantity: 10,
      quantityCompleted: 0,
      productDescription: 'PL .218',
    });

    const { props } = renderRolls();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];

    handler!({ prefix: 'INP', value: '2', raw: 'INP;2' }, 'INP;2');
    await waitFor(() => expect(screen.getByText(/Shell Count/)).toBeInTheDocument());

    handler!({ prefix: 'SC', value: '022001/L1', raw: 'SC;022001/L1' }, 'SC;022001/L1');
    await waitFor(() => expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ message: expect.stringContaining('Label 1 scanned') }),
    ));

    handler!({ prefix: 'SC', value: '022999/L2', raw: 'SC;022999/L2' }, 'SC;022999/L2');
    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'error',
          message: expect.stringContaining('022001'),
        }),
      );
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({
          message: expect.stringContaining('022999'),
        }),
      );
    });
  });

  it('rejects scanning the same unlabeled barcode twice', async () => {
    vi.mocked(workCenterApi.advanceQueue).mockResolvedValue({
      shellSize: '120',
      heatNumber: 'H1',
      coilNumber: 'C1',
      quantity: 10,
      quantityCompleted: 0,
      productDescription: 'PL .218',
    });

    const { props } = renderRolls();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];

    handler!({ prefix: 'INP', value: '2', raw: 'INP;2' }, 'INP;2');
    await waitFor(() => expect(screen.getByText(/Shell Count/)).toBeInTheDocument());

    handler!({ prefix: 'SC', value: '022001', raw: 'SC;022001' }, 'SC;022001');
    await waitFor(() => expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ message: expect.stringContaining('Label 1 scanned') }),
    ));

    handler!({ prefix: 'SC', value: '022001', raw: 'SC;022001' }, 'SC;022001');
    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'error',
          message: expect.stringContaining('Same label scanned twice'),
        }),
      );
    });
  });

});
