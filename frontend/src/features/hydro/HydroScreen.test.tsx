import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act, fireEvent, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { HydroScreen } from './HydroScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { roundSeamApi, nameplateApi } from '../../api/endpoints';

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getAssemblyByShell: vi.fn() },
  nameplateApi: { getBySerial: vi.fn() },
  workCenterApi: { getDefectCodes: vi.fn().mockResolvedValue([]), getCharacteristics: vi.fn().mockResolvedValue([]) },
  hydroApi: { create: vi.fn(), getLocationsByCharacteristic: vi.fn().mockResolvedValue([]) },
  controlPlanApi: { getForWorkCenter: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-hydro', plantId: 'plant-1', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><HydroScreen {...props} /></FluentProvider>) };
}

describe('HydroScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders waiting state', () => {
    renderScreen();
    expect(screen.getByText(/scan shell or nameplate to begin/i)).toBeInTheDocument();
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('shows scan status', () => {
    renderScreen();
    expect(screen.getByText(/shell: not scanned/i)).toBeInTheDocument();
    expect(screen.getByText(/nameplate: not scanned/i)).toBeInTheDocument();
  });

  it('shows manual input in manual mode', () => {
    renderScreen({ externalInput: false });
    expect(screen.getByPlaceholderText(/enter shell serial/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/enter nameplate serial/i)).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /submit/i })).toHaveLength(2);
  });

  it('shows scan-filled cards in external mode', () => {
    renderScreen({ externalInput: true });
    expect(screen.getByPlaceholderText(/awaiting shell scan/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/awaiting nameplate scan/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument();
  });

  it('shows error when shell and nameplate tank sizes do not match', async () => {
    const showScanResult = vi.fn();
    let barcodeHandler: (bc: any, raw: string) => void = () => {};

    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AA',
      tankSize: 120,
      roundSeamCount: 2,
      shells: ['SH-001'],
    });
    vi.mocked(nameplateApi.getBySerial).mockResolvedValue({
      id: 'np-1',
      serialNumber: 'W00100001',
      productId: 'prod-1',
      tankSize: 250,
      timestamp: new Date().toISOString(),
      printSucceeded: true,
    });

    renderScreen({
      showScanResult,
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler({ prefix: 'SC', value: 'SH-001' }, 'SC;SH-001'); });
    await act(async () => { barcodeHandler(null, 'W00100001'); });

    const lastCall = showScanResult.mock.calls[showScanResult.mock.calls.length - 1][0];
    expect(lastCall.type).toBe('error');
    expect(lastCall.message).toContain('Tank size mismatch');
  });

  it('treats unprefixed shell labels with /L1 as shell scans', async () => {
    const showScanResult = vi.fn();
    let barcodeHandler: (bc: any, raw: string) => void = () => {};

    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AA',
      tankSize: 120,
      roundSeamCount: 2,
      shells: ['021604'],
    });

    renderScreen({
      showScanResult,
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler(null, '021604/L1'); });

    expect(roundSeamApi.getAssemblyByShell).toHaveBeenCalledWith('021604');
    expect(nameplateApi.getBySerial).not.toHaveBeenCalled();
    const lastCall = showScanResult.mock.calls[showScanResult.mock.calls.length - 1][0];
    expect(lastCall.type).toBe('success');
    expect(lastCall.message).toContain('Assembly');
  });

  it('shows shell error for unprefixed shell labels when assembly lookup fails', async () => {
    const showScanResult = vi.fn();
    let barcodeHandler: (bc: any, raw: string) => void = () => {};

    vi.mocked(roundSeamApi.getAssemblyByShell).mockRejectedValue(
      new Error('Shell not found in any assembly'),
    );

    renderScreen({
      showScanResult,
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler(null, '021604/L2'); });

    expect(roundSeamApi.getAssemblyByShell).toHaveBeenCalledWith('021604');
    expect(nameplateApi.getBySerial).not.toHaveBeenCalled();
    const lastCall = showScanResult.mock.calls[showScanResult.mock.calls.length - 1][0];
    expect(lastCall.type).toBe('error');
    expect(lastCall.message).toContain('Shell not found');
  });

  it('manual shell enter uses shell lookup only', async () => {
    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AB',
      tankSize: 250,
      roundSeamCount: 2,
      shells: ['021604'],
    });

    renderScreen({ externalInput: false });

    const shellInput = screen.getByPlaceholderText(/enter shell serial/i);
    fireEvent.change(shellInput, { target: { value: 'SC;021604/L1' } });
    fireEvent.keyDown(shellInput, { key: 'Enter', code: 'Enter' });

    await waitFor(() => {
      expect(roundSeamApi.getAssemblyByShell).toHaveBeenCalledWith('021604');
    });
    expect(nameplateApi.getBySerial).not.toHaveBeenCalled();
  });

  it('manual nameplate enter uses nameplate lookup only', async () => {
    vi.mocked(nameplateApi.getBySerial).mockResolvedValue({
      id: 'np-1',
      serialNumber: 'W00100001',
      productId: 'prod-1',
      tankSize: 250,
      timestamp: new Date().toISOString(),
      printSucceeded: true,
    });

    renderScreen({ externalInput: false });

    const nameplateInput = screen.getByPlaceholderText(/enter nameplate serial/i);
    fireEvent.change(nameplateInput, { target: { value: 'W00100001' } });
    fireEvent.keyDown(nameplateInput, { key: 'Enter', code: 'Enter' });

    await waitFor(() => {
      expect(nameplateApi.getBySerial).toHaveBeenCalledWith('W00100001');
    });
    expect(roundSeamApi.getAssemblyByShell).not.toHaveBeenCalled();
  });
});
