import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RoundSeamScreen } from './RoundSeamScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

const mockGetSetup = vi.fn().mockResolvedValue({ isComplete: false, tankSize: 0 });
const mockSaveSetup = vi.fn().mockResolvedValue({ isComplete: true, tankSize: 500 });

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: {
    getSetup: (...args: unknown[]) => mockGetSetup(...args),
    saveSetup: (...args: unknown[]) => mockSaveSetup(...args),
    createRecord: vi.fn(),
    getAssemblyByShell: vi.fn(),
  },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rs', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1', plantId: 'plant-1',
    welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    numberOfWelders: 1, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><RoundSeamScreen {...props} /></FluentProvider>) };
}

describe('RoundSeamScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('does not show warning before setup load completes', async () => {
    let resolveSetup!: (value: { isComplete: boolean; tankSize: number }) => void;
    mockGetSetup.mockImplementationOnce(
      () => new Promise<{ isComplete: boolean; tankSize: number }>((resolve) => { resolveSetup = resolve; }),
    );

    renderScreen();
    expect(screen.queryByText(/roundseam setup hasn't been completed/i)).not.toBeInTheDocument();

    resolveSetup({ isComplete: false, tankSize: 0 });
    expect(await screen.findByText(/roundseam setup hasn't been completed/i)).toBeInTheDocument();
  });

  it('shows warning when setup not complete', async () => {
    renderScreen();
    expect(await screen.findByText(/roundseam setup hasn't been completed/i)).toBeInTheDocument();
  });

  it('shows NEXT guidance to complete setup when setup is incomplete', async () => {
    renderScreen();
    expect(await screen.findByText(/next: complete roundseam setup/i)).toBeInTheDocument();
  });

  it('renders NEXT guidance above setup button', async () => {
    renderScreen();
    const nextGuidance = await screen.findByText(/next: complete roundseam setup/i);
    const setupButton = screen.getByRole('button', { name: /roundseam setup/i });
    const relation = nextGuidance.compareDocumentPosition(setupButton);
    expect((relation & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
  });

  it('shows setup button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /roundseam setup/i })).toBeInTheDocument();
  });

  it('hides setup button in external input mode', () => {
    renderScreen({ externalInput: true });
    expect(screen.queryByRole('button', { name: /roundseam setup/i })).not.toBeInTheDocument();
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('shows manual submit in manual mode', () => {
    renderScreen({ externalInput: false });
    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument();
  });

  it('auto-opens setup dialog when no setup exists', async () => {
    renderScreen();
    expect(await screen.findByText('Roundseam Setup')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save setup/i })).toBeInTheDocument();
  });

  it('does not show tank size dropdown (removed)', async () => {
    renderScreen();
    await waitFor(() => expect(screen.queryByText('Select tank size')).not.toBeInTheDocument());
  });

  it('shows seam info in setup dialog', async () => {
    renderScreen();
    expect(await screen.findByText(/seams required/i)).toBeInTheDocument();
  });

  it('does not auto-open dialog when setup is complete', async () => {
    mockGetSetup.mockResolvedValueOnce({
      isComplete: true, tankSize: 500,
      rs1WelderId: 'w1', rs2WelderId: 'w1',
    });
    renderScreen();
    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /save setup/i })).not.toBeInTheDocument();
    });
  });

  it('treats setup welder ids as case-insensitive', async () => {
    mockGetSetup.mockResolvedValueOnce({
      isComplete: true,
      tankSize: 500,
      rs1WelderId: 'W1',
      rs2WelderId: 'W1',
    });
    renderScreen({
      welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    });
    await waitFor(() => {
      expect(screen.queryByText(/setup welders are not all active/i)).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /save setup/i })).not.toBeInTheDocument();
    });
  });

  it('shows NEXT guidance to scan shell when setup is complete', async () => {
    mockGetSetup.mockResolvedValueOnce({
      isComplete: true,
      tankSize: 1000,
      rs1WelderId: 'w1',
      rs2WelderId: 'w1',
      rs3WelderId: 'w1',
    });
    renderScreen();
    expect(await screen.findByText(/next: scan shell barcode/i)).toBeInTheDocument();
  });

  it('shows tank size and seam assignments directly under NEXT area', async () => {
    mockGetSetup.mockResolvedValueOnce({
      isComplete: true,
      tankSize: 500,
      rs1WelderId: 'w1',
      rs2WelderId: 'w1',
    });
    renderScreen();

    const nextGuidance = await screen.findByText(/next: scan shell barcode/i);
    expect(screen.getByText(/tank size:/i)).toBeInTheDocument();
    expect(screen.getByText(/seams:/i)).toBeInTheDocument();
    expect(screen.getByText(/seam 1 =/i)).toBeInTheDocument();
    expect(screen.getByText(/seam 2 =/i)).toBeInTheDocument();

    const tankSize = screen.getByText(/tank size:/i);
    const relation = nextGuidance.compareDocumentPosition(tankSize);
    expect((relation & Node.DOCUMENT_POSITION_FOLLOWING) !== 0).toBe(true);
  });

  it('reopens setup when logged-in welders change', async () => {
    mockGetSetup.mockResolvedValueOnce({
      isComplete: true,
      tankSize: 500,
      rs1WelderId: 'w1',
      rs2WelderId: 'w1',
    });
    const { rerender, props } = renderScreen({
      welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    });

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /save setup/i })).not.toBeInTheDocument();
    });

    rerender(
      <FluentProvider theme={webLightTheme}>
        <RoundSeamScreen
          {...props}
          welders={[
            { userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' },
            { userId: 'w2', displayName: 'Welder 2', employeeNumber: '002' },
          ]}
        />
      </FluentProvider>,
    );

    expect(await screen.findByText(/logged-in welders changed/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save setup/i })).toBeInTheDocument();
  });

  it('forces re-setup when saved setup uses welders not currently active', async () => {
    mockGetSetup.mockResolvedValueOnce({
      isComplete: true,
      tankSize: 500,
      rs1WelderId: 'w1',
      rs2WelderId: 'w2',
    });

    renderScreen({
      welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    });

    expect(await screen.findByText(/setup welders are not all active/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save setup/i })).toBeInTheDocument();
    expect(screen.getByText(/roundseam setup hasn't been completed/i)).toBeInTheDocument();
  });

  it('does not auto-open setup dialog until welder count is loaded', async () => {
    mockGetSetup.mockResolvedValue({ isComplete: false, tankSize: 0 });
    const { rerender, props } = renderScreen({ welderCountLoaded: false });
    // Setup dialog should NOT appear yet
    await waitFor(() => {
      expect(mockGetSetup).toHaveBeenCalled();
    });
    expect(screen.queryByRole('button', { name: /save setup/i })).not.toBeInTheDocument();

    // Now simulate welder count becoming loaded
    rerender(
      <FluentProvider theme={webLightTheme}>
        <RoundSeamScreen {...props} welderCountLoaded={true} />
      </FluentProvider>
    );
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /save setup/i })).toBeInTheDocument();
    });
  });
});
