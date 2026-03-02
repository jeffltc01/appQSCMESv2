import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, act, fireEvent } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RoundSeamInspScreen } from './RoundSeamInspScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import type { ParsedBarcode } from '../../types/barcode';

const mockGetAssemblyByShell = vi.fn();
const mockGetDefectCodes = vi.fn().mockResolvedValue([{ id: 'dc1', code: 'DC1', name: 'Defect 1', severity: 'Low' }]);
const mockGetDefectLocations = vi.fn().mockResolvedValue([{ id: 'loc1', code: 'L1', name: 'Location 1', characteristicIds: [] }]);
const mockGetCharacteristics = vi.fn().mockResolvedValue([{ id: 'char1', code: 'RS1', name: 'Round Seam' }]);
const mockCreate = vi.fn().mockResolvedValue({ id: 'ir-1', serialNumber: 'AA', defects: [] });

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getAssemblyByShell: (...args: unknown[]) => mockGetAssemblyByShell(...args) },
  workCenterApi: {
    getDefectCodes: (...args: unknown[]) => mockGetDefectCodes(...args),
    getDefectLocations: (...args: unknown[]) => mockGetDefectLocations(...args),
    getCharacteristics: (...args: unknown[]) => mockGetCharacteristics(...args),
  },
  inspectionRecordApi: { create: (...args: unknown[]) => mockCreate(...args) },
  controlPlanApi: { getForWorkCenter: vi.fn().mockResolvedValue([]) },
}));

let capturedHandler: ((bc: ParsedBarcode | null, raw: string) => void) | null = null;

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rsi', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1', welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(),
    registerBarcodeHandler: vi.fn((handler) => { capturedHandler = handler; }),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><RoundSeamInspScreen {...props} /></FluentProvider>) };
}

describe('RoundSeamInspScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); capturedHandler = null; });

  it('renders waiting state', () => {
    renderScreen();
    expect(screen.getByText(/next: scan shell label/i)).toBeInTheDocument();
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('has manual serial input', () => {
    renderScreen();
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeInTheDocument();
  });

  it('hides serial input area in external mode', () => {
    renderScreen({ externalInput: true });
    expect(screen.queryByPlaceholderText(/enter serial number/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument();
  });

  it('rejects save when pending defect is incomplete (has code but no location)', async () => {
    mockGetAssemblyByShell.mockResolvedValue({ alphaCode: 'AA', tankSize: 500, roundSeamCount: 2 });
    const { props } = renderScreen();
    const showScanResult = props.showScanResult as ReturnType<typeof vi.fn>;

    // Scan a shell to enter AwaitingDefects state
    await act(async () => {
      capturedHandler!({ prefix: 'SC', value: '022101', raw: 'SC;022101' }, 'SC;022101');
    });
    await waitFor(() => expect(screen.getByText(/tank size/i)).toBeInTheDocument());

    // Scan a defect code (creates a pending entry with only defectCodeId set)
    act(() => {
      capturedHandler!({ prefix: 'D', value: 'DC1', raw: 'D;DC1' }, 'D;DC1');
    });

    // Scan Save — should be rejected because pending is incomplete
    act(() => {
      capturedHandler!({ prefix: 'S', value: '1', raw: 'S;1' }, 'S;1');
    });

    expect(mockCreate).not.toHaveBeenCalled();
    expect(showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: expect.stringContaining('ncomplete') }),
    );
  });

  it('loads characteristics with tankSize after assembly lookup', async () => {
    mockGetAssemblyByShell.mockResolvedValue({ alphaCode: 'BB', tankSize: 500, roundSeamCount: 2 });
    mockGetCharacteristics.mockResolvedValue([
      { id: 'rs1', code: 'RS1', name: 'RS1', minTankSize: 0 },
      { id: 'rs2', code: 'RS2', name: 'RS2', minTankSize: 0 },
    ]);
    renderScreen();

    await act(async () => {
      capturedHandler!({ prefix: 'SC', value: '022102', raw: 'SC;022102' }, 'SC;022102');
    });
    await waitFor(() => expect(screen.getByText(/tank size/i)).toBeInTheDocument());

    expect(mockGetCharacteristics).toHaveBeenCalledWith('wc-rsi', 500);
    expect(screen.getByText(/next: scan defect \+ characteristic \+ location, or scan save/i)).toBeInTheDocument();
  });

  it('shows external-input NEXT guidance while waiting', () => {
    renderScreen({ externalInput: true });
    expect(screen.getByText(/next: scan shell label/i)).toBeInTheDocument();
  });

  it('includes selected assetId when saving inspection', async () => {
    mockGetAssemblyByShell.mockResolvedValue({ alphaCode: 'AA', tankSize: 500, roundSeamCount: 2 });
    renderScreen({ assetId: 'asset-123' });

    await act(async () => {
      capturedHandler!({ prefix: 'SC', value: '022103', raw: 'SC;022103' }, 'SC;022103');
    });
    await waitFor(() => expect(screen.getByText(/tank size/i)).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith(expect.objectContaining({
        assetId: 'asset-123',
        workCenterId: 'wc-rsi',
      }));
    });
  });
});
