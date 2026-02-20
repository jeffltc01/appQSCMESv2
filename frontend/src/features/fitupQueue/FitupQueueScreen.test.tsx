import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { FitupQueueScreen } from './FitupQueueScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  workCenterApi: { getMaterialQueue: vi.fn().mockResolvedValue([]) },
  materialQueueApi: { addFitupItem: vi.fn(), updateFitupItem: vi.fn(), deleteFitupItem: vi.fn() },
  productApi: { getProducts: vi.fn().mockResolvedValue([]) },
  vendorApi: { getVendors: vi.fn().mockResolvedValue([]) },
  barcodeCardApi: { getCards: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-fitup-q', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], requiresWelder: false, externalInput: false,
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(), setRequiresWelder: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><FitupQueueScreen {...props} /></FluentProvider>) };
}

describe('FitupQueueScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders queue header', () => {
    renderScreen();
    expect(screen.getByText(/material queue for: fitup/i)).toBeInTheDocument();
  });

  it('sets requiresWelder to false', () => {
    const { props } = renderScreen();
    expect(props.setRequiresWelder).toHaveBeenCalledWith(false);
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('shows add button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /add material to queue/i })).toBeInTheDocument();
  });

  it('shows empty queue message', () => {
    renderScreen();
    expect(screen.getByText(/no material in queue/i)).toBeInTheDocument();
  });
});
