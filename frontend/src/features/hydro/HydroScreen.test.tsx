import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { HydroScreen } from './HydroScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getAssemblyByShell: vi.fn() },
  nameplateApi: { getBySerial: vi.fn() },
  workCenterApi: { getDefectCodes: vi.fn().mockResolvedValue([]), getCharacteristics: vi.fn().mockResolvedValue([]) },
  hydroApi: { create: vi.fn(), getLocationsByCharacteristic: vi.fn().mockResolvedValue([]) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-hydro', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], numberOfWelders: 0, externalInput: false,
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
});
