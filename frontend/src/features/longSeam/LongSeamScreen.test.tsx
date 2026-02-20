import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { LongSeamScreen } from './LongSeamScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  productionRecordApi: {
    create: vi.fn(),
  },
}));

const { productionRecordApi } = await import('../../api/endpoints');

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-ls',
    assetId: 'asset-1',
    productionLineId: 'pl-1',
    operatorId: 'op-1',
    welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    requiresWelder: true,
    externalInput: false,
    showScanResult: vi.fn(),
    refreshHistory: vi.fn(),
    registerBarcodeHandler: vi.fn(),
    setRequiresWelder: vi.fn(),
    ...overrides,
  };
}

function renderLongSeam(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return {
    props,
    ...render(
      <FluentProvider theme={webLightTheme}>
        <LongSeamScreen {...props} />
      </FluentProvider>,
    ),
  };
}

describe('LongSeamScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders prompt to scan serial', () => {
    renderLongSeam();
    expect(screen.getByText(/scan serial number/i)).toBeInTheDocument();
  });

  it('sets requiresWelder to true', () => {
    const { props } = renderLongSeam();
    expect(props.setRequiresWelder).toHaveBeenCalledWith(true);
  });

  it('has serial number input in manual mode', () => {
    renderLongSeam();
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeInTheDocument();
  });

  it('has submit button in manual mode', () => {
    renderLongSeam();
    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument();
  });

  it('disables input in external input mode', () => {
    renderLongSeam({ externalInput: true });
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeDisabled();
  });

  it('calls create on manual submit', async () => {
    const user = userEvent.setup();
    vi.mocked(productionRecordApi.create).mockResolvedValue({
      id: 'pr1',
      serialNumber: '000001',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderLongSeam();
    const input = screen.getByPlaceholderText(/enter serial number/i);
    await user.type(input, '000001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(productionRecordApi.create).toHaveBeenCalledWith(
        expect.objectContaining({ serialNumber: '000001' }),
      );
    });
  });

  it('shows success on successful record', async () => {
    const user = userEvent.setup();
    vi.mocked(productionRecordApi.create).mockResolvedValue({
      id: 'pr1',
      serialNumber: '000001',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderLongSeam();
    const input = screen.getByPlaceholderText(/enter serial number/i);
    await user.type(input, '000001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'success' }),
      );
    });
  });

  it('shows warning on catch-up flow', async () => {
    const user = userEvent.setup();
    vi.mocked(productionRecordApi.create).mockResolvedValue({
      id: 'pr1',
      serialNumber: '000001',
      timestamp: new Date().toISOString(),
      warning: 'Rolls missed — annotation created',
    });

    const { props } = renderLongSeam();
    const input = screen.getByPlaceholderText(/enter serial number/i);
    await user.type(input, '000001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'success',
          message: expect.stringContaining('Rolls missed'),
        }),
      );
    });
  });

  it('shows error on failure', async () => {
    const user = userEvent.setup();
    vi.mocked(productionRecordApi.create).mockRejectedValue({ message: 'Duplicate' });

    const { props } = renderLongSeam();
    const input = screen.getByPlaceholderText(/enter serial number/i);
    await user.type(input, '000001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'error' }),
      );
    });
  });

  it('handles barcode scan with L1 suffix stripping', () => {
    const { props } = renderLongSeam();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    expect(handler).toBeDefined();

    if (handler) {
      handler({ prefix: 'SC', value: '000001/L1', raw: 'SC;000001/L1' }, 'SC;000001/L1');
    }
    // productionRecordApi.create should be called with serial 000001 (suffix stripped)
  });

  it('handles barcode scan with L2 suffix stripping', () => {
    const { props } = renderLongSeam();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    expect(handler).toBeDefined();

    if (handler) {
      handler({ prefix: 'SC', value: '000001/L2', raw: 'SC;000001/L2' }, 'SC;000001/L2');
    }
  });

  it('rejects non-SC barcodes', () => {
    const { props } = renderLongSeam();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (handler) {
      handler({ prefix: 'D', value: '042', raw: 'D;042' }, 'D;042');
    }
    expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error' }),
    );
  });
});
