import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { NameplateScreen } from './NameplateScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

const mockGetProducts = vi.fn().mockResolvedValue([]);

vi.mock('../../api/endpoints', () => ({
  productApi: { getProducts: (...args: unknown[]) => mockGetProducts(...args) },
  nameplateApi: { create: vi.fn() },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-np', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1',
    welders: [], numberOfWelders: 0, externalInput: false,
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderScreen(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return { props, ...render(<FluentProvider theme={webLightTheme}><NameplateScreen {...props} /></FluentProvider>) };
}

describe('NameplateScreen', () => {
  beforeEach(() => { vi.clearAllMocks(); });

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

  it('passes plantId when fetching products', async () => {
    renderScreen({ plantId: 'site-abc' });
    await waitFor(() => expect(mockGetProducts).toHaveBeenCalledWith('sellable', 'site-abc'));
  });

  it('displays product number in dropdown options', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    const { container } = renderScreen();
    // Fluent Dropdown renders options in a listbox portal when expanded;
    // the button text attribute reflects the selected value format.
    // Open the dropdown to render the option.
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await waitFor(() => expect(screen.getByRole('option', { name: 'PLT-120AG' })).toBeInTheDocument());
  });
});
