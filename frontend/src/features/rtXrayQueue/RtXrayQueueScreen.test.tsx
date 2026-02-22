import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RtXrayQueueScreen } from './RtXrayQueueScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  xrayQueueApi: { getQueue: vi.fn().mockResolvedValue([]), addItem: vi.fn(), removeItem: vi.fn() },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-xray', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1', welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><RtXrayQueueScreen {...props} /></FluentProvider>) };
}

describe('RtXrayQueueScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders queue header', () => {
    renderScreen();
    expect(screen.getByText(/queue for: real time x-ray/i)).toBeInTheDocument();
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('shows add button in manual mode', () => {
    renderScreen({ externalInput: false });
    expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument();
  });

  it('hides manual input in external mode', () => {
    renderScreen({ externalInput: true });
    expect(screen.queryByPlaceholderText(/enter serial/i)).not.toBeInTheDocument();
  });

  it('shows empty queue message', () => {
    renderScreen();
    expect(screen.getByText(/queue is empty/i)).toBeInTheDocument();
  });
});
