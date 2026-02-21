import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
  workCenterApi: { getWelders: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rs', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    numberOfWelders: 1, externalInput: false,
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

  it('shows warning when setup not complete', async () => {
    renderScreen();
    expect(await screen.findByText(/roundseam setup hasn't been completed/i)).toBeInTheDocument();
  });

  it('shows setup button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /roundseam setup/i })).toBeInTheDocument();
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
      rs1WelderId: 'w1', rs2WelderId: 'w2',
    });
    renderScreen();
    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /save setup/i })).not.toBeInTheDocument();
    });
  });
});
