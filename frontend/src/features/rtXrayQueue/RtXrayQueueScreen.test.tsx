import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { RtXrayQueueScreen } from './RtXrayQueueScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';
import { xrayQueueApi } from '../../api/endpoints';

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

  it('disables add button when queue is full', async () => {
    vi.mocked(xrayQueueApi.getQueue).mockResolvedValueOnce(
      Array.from({ length: 5 }, (_, i) => ({
        id: `x-${i}`,
        serialNumber: `SN-${i}`,
        createdAt: new Date().toISOString(),
      })),
    );

    renderScreen({ externalInput: false });

    const addButton = await screen.findByRole('button', { name: /^add$/i });
    expect(addButton).toBeDisabled();
    expect(screen.getByText(/queue is full \(max 5 items\)/i)).toBeInTheDocument();
  });

  it('shows delete confirmation modal and cancels delete', async () => {
    vi.mocked(xrayQueueApi.getQueue).mockResolvedValueOnce([
      { id: 'x-1', serialNumber: 'SN-1', createdAt: new Date().toISOString() },
    ]);

    renderScreen();

    const deleteButton = await screen.findByRole('button', { name: '🗑' });
    fireEvent.click(deleteButton);

    expect(screen.getByText(/remove from queue\?/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }));
    expect(xrayQueueApi.removeItem).not.toHaveBeenCalled();
  });

  it('deletes when confirmation modal is accepted', async () => {
    vi.mocked(xrayQueueApi.getQueue).mockResolvedValueOnce([
      { id: 'x-1', serialNumber: 'SN-1', createdAt: new Date().toISOString() },
    ]);

    renderScreen();

    const deleteButton = await screen.findByRole('button', { name: '🗑' });
    fireEvent.click(deleteButton);
    fireEvent.click(screen.getByRole('button', { name: /yes, remove/i }));

    await waitFor(() => {
      expect(xrayQueueApi.removeItem).toHaveBeenCalledWith('wc-xray', 'x-1');
    });
  });

  it('preserves manual serial when add fails', async () => {
    vi.mocked(xrayQueueApi.addItem).mockRejectedValueOnce(new Error('Save failed'));
    const { props } = renderScreen({ externalInput: false });

    const input = screen.getByPlaceholderText(/enter serial number/i);
    fireEvent.change(input, { target: { value: 'SN-FAIL' } });
    fireEvent.click(screen.getByRole('button', { name: /^add$/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(expect.objectContaining({ type: 'error' }));
    });
    expect((input as HTMLInputElement).value).toBe('SN-FAIL');
  });
});
