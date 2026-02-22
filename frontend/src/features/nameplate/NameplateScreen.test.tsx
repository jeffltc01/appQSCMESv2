import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { NameplateScreen } from './NameplateScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

const mockGetProducts = vi.fn().mockResolvedValue([]);
const mockCreate = vi.fn();

vi.mock('../../api/endpoints', () => ({
  productApi: { getProducts: (...args: unknown[]) => mockGetProducts(...args) },
  nameplateApi: { create: (...args: unknown[]) => mockCreate(...args) },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-np', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1',
    welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
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
    renderScreen();
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await waitFor(() => expect(screen.getByRole('option', { name: 'PLT-120AG' })).toBeInTheDocument());
  });

  it('shows success message when print succeeds', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    mockCreate.mockResolvedValue({
      id: 'sn-1', serialNumber: 'W00100001', productId: 'p1',
      timestamp: new Date().toISOString(), printSucceeded: true, printMessage: null,
    });

    const { props } = renderScreen();
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'W00100001');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'success', message: expect.stringContaining('W00100001') }),
      );
    });
  });

  it('shows warning message when print fails', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    mockCreate.mockResolvedValue({
      id: 'sn-2', serialNumber: 'W00100002', productId: 'p1',
      timestamp: new Date().toISOString(), printSucceeded: false, printMessage: 'Printer offline',
    });

    const { props } = renderScreen();
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'W00100002');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'warning', message: expect.stringContaining('Printer offline') }),
      );
    });
  });
});
