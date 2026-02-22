import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { HydroScreen } from './HydroScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { roundSeamApi, nameplateApi } from '../../api/endpoints';

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getAssemblyByShell: vi.fn() },
  nameplateApi: { getBySerial: vi.fn() },
  workCenterApi: { getDefectCodes: vi.fn().mockResolvedValue([]), getCharacteristics: vi.fn().mockResolvedValue([]) },
  hydroApi: { create: vi.fn(), getLocationsByCharacteristic: vi.fn().mockResolvedValue([]) },
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
    expect(screen.getByPlaceholderText(/enter serial/i)).toBeInTheDocument();
  });

  it('hides manual input in external mode', () => {
    renderScreen({ externalInput: true });
    expect(screen.queryByPlaceholderText(/enter serial/i)).not.toBeInTheDocument();
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
});
