import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RoundSeamScreen } from './RoundSeamScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getSetup: vi.fn().mockResolvedValue({ isComplete: false }), saveSetup: vi.fn(), createRecord: vi.fn() },
  workCenterApi: { getWelders: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rs', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    requiresWelder: true, externalInput: false,
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(), setRequiresWelder: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><RoundSeamScreen {...props} /></FluentProvider>) };
}

describe('RoundSeamScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('sets requiresWelder to true', () => {
    const { props } = renderScreen();
    expect(props.setRequiresWelder).toHaveBeenCalledWith(true);
  });

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
});
