import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RoundSeamInspScreen } from './RoundSeamInspScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  roundSeamApi: { getAssemblyByShell: vi.fn() },
  workCenterApi: { getDefectCodes: vi.fn().mockResolvedValue([]), getDefectLocations: vi.fn().mockResolvedValue([]), getCharacteristics: vi.fn().mockResolvedValue([]) },
  inspectionRecordApi: { create: vi.fn() },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-rsi', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], numberOfWelders: 0, externalInput: false,
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><RoundSeamInspScreen {...props} /></FluentProvider>) };
}

describe('RoundSeamInspScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders waiting state', () => {
    renderScreen();
    expect(screen.getByText(/scan serial number to begin/i)).toBeInTheDocument();
  });

  it('registers barcode handler', () => {
    const { props } = renderScreen();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('has manual serial input', () => {
    renderScreen();
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeInTheDocument();
  });

  it('disables input in external mode', () => {
    renderScreen({ externalInput: true });
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeDisabled();
  });
});
