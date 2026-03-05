import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { NameplateScreen } from './NameplateScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import type { ParsedBarcode } from '../../types/barcode';

const mockGetProducts = vi.fn().mockResolvedValue([]);
const mockCreate = vi.fn();
const mockUpdate = vi.fn();

vi.mock('../../api/endpoints', () => ({
  productApi: { getProducts: (...args: unknown[]) => mockGetProducts(...args) },
  nameplateApi: {
    create: (...args: unknown[]) => mockCreate(...args),
    update: (...args: unknown[]) => mockUpdate(...args),
  },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-np', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1', plantCode: '000',
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

  it('hides serial input and save button when external input is enabled', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    renderScreen({ externalInput: true });

    await waitFor(() => expect(mockGetProducts).toHaveBeenCalled());
    expect(screen.queryByPlaceholderText(/enter serial number/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument();
    expect(screen.getByText(/scan finished serial barcode/i)).toBeInTheDocument();
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

  it('auto-saves when external input scans a finished serial', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    mockCreate.mockResolvedValue({
      id: 'sn-7', serialNumber: 'W000302100', productId: 'p1',
      timestamp: new Date().toISOString(), printSucceeded: true, printMessage: null,
    });

    let barcodeHandler: ((bc: ParsedBarcode | null, raw: string) => void) | null = null;
    const registerBarcodeHandler = vi.fn((handler: (bc: ParsedBarcode | null, raw: string) => void) => {
      barcodeHandler = handler;
    });

    const { props } = renderScreen({ externalInput: true, registerBarcodeHandler });
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    expect(barcodeHandler).not.toBeNull();
    await act(async () => {
      barcodeHandler?.(null, 'W000302100');
    });

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith(expect.objectContaining({
        serialNumber: 'W000302100',
        productId: 'p1',
      }));
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'success', message: expect.stringContaining('W000302100') }),
      );
    });
  });

  it('retains product selection after successful save', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    mockCreate.mockResolvedValue({
      id: 'sn-3', serialNumber: 'W00100003', productId: 'p1',
      timestamp: new Date().toISOString(), printSucceeded: true, printMessage: null,
    });

    renderScreen();
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'W00100003');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => expect(mockCreate).toHaveBeenCalled());

    expect(combobox).toHaveValue('PLT-120AG');
    expect(screen.getByPlaceholderText(/enter serial number/i)).toHaveValue('');
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

  it('shows dry-run message when print is suppressed in non-production', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    mockCreate.mockResolvedValue({
      id: 'sn-8', serialNumber: 'W00100008', productId: 'p1',
      timestamp: new Date().toISOString(), printSucceeded: false,
      printMessage: 'Print suppressed in non-production environment by configuration.',
    });

    const { props } = renderScreen();
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'W00100008');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'warning',
          message: expect.stringContaining('dry-run mode'),
        }),
      );
    });
  });

  it('blocks save for Fremont when serial does not start with F', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);

    const { props } = renderScreen({ plantCode: '600' });
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'W00100004');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    expect(mockCreate).not.toHaveBeenCalled();
    expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: expect.stringContaining('Fremont (600)') }),
    );
  });

  it('blocks save for West Jordan when serial does not start with W', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);

    const { props } = renderScreen({ plantCode: '700' });
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'F00100005');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    expect(mockCreate).not.toHaveBeenCalled();
    expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'error', message: expect.stringContaining('West Jordan (700)') }),
    );
  });

  it('allows save for Cleveland regardless of serial prefix', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
    ]);
    mockCreate.mockResolvedValue({
      id: 'sn-6', serialNumber: 'F00100006', productId: 'p1',
      timestamp: new Date().toISOString(), printSucceeded: true, printMessage: null,
    });

    renderScreen({ plantCode: '000' });
    const combobox = await waitFor(() => screen.getByRole('combobox'));
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-120AG' }).click(); });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    await userEvent.type(input, 'F00100006');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => expect(mockCreate).toHaveBeenCalled());
  });

  it('updates selected history record product and auto-reprints', async () => {
    mockGetProducts.mockResolvedValue([
      { id: 'p1', productNumber: 'PLT-120AG', tankSize: 120, tankType: 'AG', nameplateNumber: null },
      { id: 'p2', productNumber: 'PLT-250AG', tankSize: 250, tankType: 'AG', nameplateNumber: null },
    ]);
    mockUpdate.mockResolvedValue({
      id: 'sn-9', serialNumber: 'W00100009', productId: 'p2',
      timestamp: new Date().toISOString(), printSucceeded: true, printMessage: null,
    });

    const { props } = renderScreen({
      selectedHistoryRecord: {
        id: 'pr-1',
        productionRecordId: 'pr-1',
        serialNumberId: 'sn-9',
        serialOrIdentifier: 'W00100009',
        productId: 'p1',
        timestamp: new Date().toISOString(),
        hasAnnotation: false,
      },
    });

    const serialInput = await waitFor(() => screen.getByPlaceholderText(/enter serial number/i));
    expect(serialInput).toBeDisabled();
    expect(serialInput).toHaveValue('W00100009');

    const combobox = screen.getByRole('combobox');
    await act(async () => { combobox.click(); });
    await act(async () => { screen.getByRole('option', { name: 'PLT-250AG' }).click(); });

    await userEvent.click(screen.getByRole('button', { name: /update & reprint/i }));

    await waitFor(() => {
      expect(mockUpdate).toHaveBeenCalledWith('sn-9', { productId: 'p2', operatorId: 'op-1' });
    });
    expect(mockCreate).not.toHaveBeenCalled();
    expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'success', message: expect.stringContaining('updated') }),
    );
  });
});
