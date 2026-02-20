import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { NameplateScreen } from './NameplateScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  productApi: { getProducts: vi.fn().mockResolvedValue([]) },
  nameplateApi: { create: vi.fn() },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-np', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    welders: [], requiresWelder: false, externalInput: false,
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(), setRequiresWelder: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><NameplateScreen {...props} /></FluentProvider>) };
}

describe('NameplateScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('sets requiresWelder to false', () => {
    const { props } = renderScreen();
    expect(props.setRequiresWelder).toHaveBeenCalledWith(false);
  });

  it('has serial number input', () => {
    renderScreen();
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeInTheDocument();
  });

  it('has save button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /save/i })).toBeInTheDocument();
  });

  it('save button disabled when no input', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /save/i })).toBeDisabled();
  });
});
