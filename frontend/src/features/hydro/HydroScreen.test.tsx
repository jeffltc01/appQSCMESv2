import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act, fireEvent, waitFor, within } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { HydroScreen } from './HydroScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { roundSeamApi, nameplateApi, workCenterApi, controlPlanApi } from '../../api/endpoints';

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getAssemblyByShell: vi.fn() },
  nameplateApi: { getBySerial: vi.fn() },
  workCenterApi: {
    getDefectCodes: vi.fn().mockResolvedValue([]),
    getDefectLocations: vi.fn().mockResolvedValue([]),
    getCharacteristics: vi.fn().mockResolvedValue([]),
  },
  hydroApi: { create: vi.fn() },
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
    const shellCardHeader = screen.getByText(/shell \/ tank/i).closest('div');
    expect(shellCardHeader).not.toBeNull();
    expect(within(shellCardHeader as HTMLElement).getByText(/^shell$/i)).toBeInTheDocument();
    expect(within(shellCardHeader as HTMLElement).getByText(/not scanned/i)).toBeInTheDocument();
    const nameplateCardHeader = screen.getByText(/tank nameplate/i).closest('div');
    expect(nameplateCardHeader).not.toBeNull();
    expect(within(nameplateCardHeader as HTMLElement).getByText(/^nameplate$/i)).toBeInTheDocument();
    expect(within(nameplateCardHeader as HTMLElement).getByText(/not scanned/i)).toBeInTheDocument();
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
    expect(screen.getAllByLabelText(/tank size mismatch/i)).toHaveLength(2);
    expect(screen.getByText(/tank size:\s*120 gal/i)).toBeInTheDocument();
    expect(screen.getByText(/tank size:\s*250 gal/i)).toBeInTheDocument();
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

  it('requests characteristics with resolved tank size when shell scan succeeds', async () => {
    let barcodeHandler: (bc: any, raw: string) => void = () => {};
    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AA',
      tankSize: 120,
      roundSeamCount: 2,
      shells: ['021604'],
    });

    renderScreen({
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler({ prefix: 'SC', value: '021604' }, 'SC;021604'); });

    await waitFor(() => {
      expect(
        vi.mocked(workCenterApi.getCharacteristics).mock.calls.some(
          ([wcId, tankSize]) => wcId === 'wc-hydro' && tankSize === 120,
        ),
      ).toBe(true);
    });
  });

  it('shows one characteristic at a time with arrow navigation', async () => {
    let barcodeHandler: (bc: any, raw: string) => void = () => {};

    vi.mocked(controlPlanApi.getForWorkCenter).mockResolvedValue([
      { id: 'cp-1', characteristicName: 'Pressure Hold', resultType: 'PassFail', isGateCheck: true },
      { id: 'cp-2', characteristicName: 'Leak Check', resultType: 'AcceptReject', isGateCheck: true },
    ]);
    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AA',
      tankSize: 120,
      roundSeamCount: 2,
      shells: ['021604'],
    });
    vi.mocked(nameplateApi.getBySerial).mockResolvedValue({
      id: 'np-1',
      serialNumber: 'W00100001',
      productId: 'prod-1',
      tankSize: 120,
      timestamp: new Date().toISOString(),
      printSucceeded: true,
    });

    renderScreen({
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler({ prefix: 'SC', value: '021604' }, 'SC;021604'); });
    await act(async () => { barcodeHandler(null, 'W00100001'); });

    expect(await screen.findByRole('button', { name: /^pass$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^fail$/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/previous characteristic/i)).toBeDisabled();
    expect(screen.getByLabelText(/next characteristic/i)).not.toBeDisabled();

    fireEvent.click(screen.getByLabelText(/next characteristic/i));
    expect(await screen.findByRole('button', { name: /^accept$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^reject$/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/next characteristic/i)).toBeDisabled();

    fireEvent.click(screen.getByLabelText(/previous characteristic/i));
    expect(await screen.findByRole('button', { name: /^pass$/i })).toBeInTheDocument();
  });

  it('renders control-plan response buttons and no numeric entry input', async () => {
    let barcodeHandler: (bc: any, raw: string) => void = () => {};

    vi.mocked(controlPlanApi.getForWorkCenter).mockResolvedValue([
      { id: 'cp-1', characteristicName: 'Pressure Hold', resultType: 'PassFail', isGateCheck: true },
    ]);
    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AA',
      tankSize: 120,
      roundSeamCount: 2,
      shells: ['021604'],
    });
    vi.mocked(nameplateApi.getBySerial).mockResolvedValue({
      id: 'np-1',
      serialNumber: 'W00100001',
      productId: 'prod-1',
      tankSize: 120,
      timestamp: new Date().toISOString(),
      printSucceeded: true,
    });

    renderScreen({
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler({ prefix: 'SC', value: '021604' }, 'SC;021604'); });
    await act(async () => { barcodeHandler(null, 'W00100001'); });

    const passBtn = await screen.findByRole('button', { name: /^pass$/i });
    const failBtn = screen.getByRole('button', { name: /^fail$/i });
    const saveBtn = screen.getByRole('button', { name: /no defects accept/i });
    expect(saveBtn).toBeDisabled();
    expect(passBtn).toHaveAttribute('data-state', 'neutral');
    expect(failBtn).toHaveAttribute('data-state', 'neutral');

    fireEvent.click(passBtn);
    expect(passBtn).toHaveAttribute('data-state', 'positive');
    expect(failBtn).toHaveAttribute('data-state', 'neutral');
    expect(saveBtn).not.toBeDisabled();

    fireEvent.click(failBtn);
    expect(passBtn).toHaveAttribute('data-state', 'neutral');
    expect(failBtn).toHaveAttribute('data-state', 'negative');
    expect(saveBtn).not.toBeDisabled();

    expect(screen.queryByPlaceholderText(/enter numeric value/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/previous characteristic/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/next characteristic/i)).not.toBeInTheDocument();
  });

  it('filters wizard locations by selected characteristic', async () => {
    let barcodeHandler: (bc: any, raw: string) => void = () => {};

    vi.mocked(workCenterApi.getDefectCodes).mockResolvedValue([
      { id: 'd1', code: '001', name: 'Crack' },
    ]);
    vi.mocked(workCenterApi.getCharacteristics).mockResolvedValue([
      { id: 'c-flange', code: 'C1', name: 'Flange' },
      { id: 'c-other', code: 'C2', name: 'Other' },
    ]);
    vi.mocked(workCenterApi.getDefectLocations).mockResolvedValue([
      { id: 'l-flange', code: 'L1', name: 'Fill Valve', characteristicIds: ['c-flange'] },
      { id: 'l-universal', code: 'L2', name: 'Universal', characteristicIds: [] },
      { id: 'l-other', code: 'L3', name: 'Other Loc', characteristicIds: ['c-other'] },
    ]);
    vi.mocked(roundSeamApi.getAssemblyByShell).mockResolvedValue({
      alphaCode: 'AA',
      tankSize: 120,
      roundSeamCount: 2,
      shells: ['021604'],
    });
    vi.mocked(nameplateApi.getBySerial).mockResolvedValue({
      id: 'np-1',
      serialNumber: 'W00100001',
      productId: 'prod-1',
      tankSize: 120,
      timestamp: new Date().toISOString(),
      printSucceeded: true,
    });

    renderScreen({
      registerBarcodeHandler: (handler: any) => { barcodeHandler = handler; },
    });

    await act(async () => { barcodeHandler({ prefix: 'SC', value: '021604' }, 'SC;021604'); });
    await act(async () => { barcodeHandler(null, 'W00100001'); });

    fireEvent.click(await screen.findByRole('button', { name: /add defect/i }));
    fireEvent.click(await screen.findByRole('button', { name: /crack/i }));
    fireEvent.click(await screen.findByRole('button', { name: /flange/i }));

    expect(await screen.findByRole('button', { name: /fill valve/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /universal/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /other loc/i })).not.toBeInTheDocument();
  });
});
