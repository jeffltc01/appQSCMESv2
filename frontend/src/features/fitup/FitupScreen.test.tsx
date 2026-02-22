import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { FitupScreen } from './FitupScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  serialNumberApi: {
    getContext: vi.fn(),
  },
  materialQueueApi: {
    getCardLookup: vi.fn(),
  },
  workCenterApi: {
    getMaterialQueue: vi.fn().mockResolvedValue([]),
  },
  assemblyApi: {
    create: vi.fn(),
    reassemble: vi.fn(),
  },
}));

const { serialNumberApi, materialQueueApi, assemblyApi } = await import('../../api/endpoints');

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-fitup',
    plantId: 'plant-1',
    assetId: 'asset-1',
    productionLineId: 'pl-1',
    operatorId: 'op-1',
    welders: [{ userId: 'w1', displayName: 'Welder 1', employeeNumber: '001' }],
    numberOfWelders: 1,
    welderCountLoaded: true,
    externalInput: false,
    setExternalInput: vi.fn(),
    showScanResult: vi.fn(),
    refreshHistory: vi.fn(),
    registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderFitup(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return {
    props,
    ...render(
      <FluentProvider theme={webLightTheme}>
        <FitupScreen {...props} />
      </FluentProvider>,
    ),
  };
}

describe('FitupScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders initial state with shell placeholder', () => {
    renderFitup();
    expect(screen.getByText('Shell 1')).toBeInTheDocument();
    expect(screen.getByText('Scan Shell')).toBeInTheDocument();
  });

  it('renders head placeholders', () => {
    renderFitup();
    expect(screen.getByText('Left Head')).toBeInTheDocument();
    expect(screen.getByText('Right Head')).toBeInTheDocument();
    expect(screen.getAllByText('Scan KC')).toHaveLength(2);
  });

  it('adds shell via manual entry', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      shellSize: '120',
    });

    const { props } = renderFitup();
    const input = screen.getByPlaceholderText(/shell serial number/i);
    await user.type(input, 'SH001');
    await user.click(screen.getByRole('button', { name: /add shell/i }));

    await waitFor(() => {
      expect(props.showScanResult).toHaveBeenCalledWith(
        expect.objectContaining({ type: 'success' }),
      );
    });
  });

  it('shows tank size dropdown in manual mode', () => {
    renderFitup();
    expect(screen.getByText('Tank Size:')).toBeInTheDocument();
  });

  it('has reset button in manual mode', () => {
    renderFitup();
    expect(screen.getByRole('button', { name: /reset/i })).toBeInTheDocument();
  });

  it('has save button in manual mode', () => {
    renderFitup();
    expect(screen.getByRole('button', { name: /^save$/i })).toBeInTheDocument();
  });

  it('has swap heads button', () => {
    renderFitup();
    expect(screen.getByRole('button', { name: /swap heads/i })).toBeInTheDocument();
  });

  it('hides manual controls in external input mode', () => {
    renderFitup({ externalInput: true });
    expect(screen.queryByRole('button', { name: /add shell/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /reset/i })).not.toBeInTheDocument();
  });

  it('registers barcode handler on mount', () => {
    const { props } = renderFitup();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('applies head lot from kanban card', async () => {
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (handler) {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    }

    await waitFor(() => {
      expect(materialQueueApi.getCardLookup).toHaveBeenCalledWith('03');
    });
  });

  it('shows alpha code after save', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'asm1',
      alphaCode: 'AA',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];

    if (handler) {
      // Add shell
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
      await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());

      // Add head
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
      await waitFor(() => expect(materialQueueApi.getCardLookup).toHaveBeenCalled());
    }
  });

  it('detects existing assembly and shows reassembly prompt', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      existingAssembly: {
        alphaCode: 'AB',
        tankSize: 120,
        shells: ['SH001'],
      },
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (handler) {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    }

    await waitFor(() => {
      expect(screen.getByText(/reassembling/i)).toBeInTheDocument();
    });
  });

  it('alpha popup does not display the text Alpha Code', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'asm1',
      alphaCode: 'AA',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    // Add shell
    handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());
    // Add head
    handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    await waitFor(() => expect(materialQueueApi.getCardLookup).toHaveBeenCalled());
    // Save
    handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    await waitFor(() => expect(assemblyApi.create).toHaveBeenCalled());
    // Alpha popup should show the code but not the words "Alpha Code"
    await waitFor(() => expect(screen.getByText('AA')).toBeInTheDocument());
    expect(screen.queryByText('Alpha Code')).not.toBeInTheDocument();
  });

  it('scanning save at alpha popup dismisses popup instead of creating another assembly', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'asm1',
      alphaCode: 'AA',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());
    handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    await waitFor(() => expect(materialQueueApi.getCardLookup).toHaveBeenCalled());
    handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    await waitFor(() => expect(assemblyApi.create).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByText('AA')).toBeInTheDocument());

    // Clear the mock call count
    vi.mocked(assemblyApi.create).mockClear();

    // Scan save again while alpha popup is showing
    handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');

    // Should NOT create another assembly
    expect(assemblyApi.create).not.toHaveBeenCalled();
    // Popup should be dismissed — back to normal view
    await waitFor(() => expect(screen.queryByText('AA')).not.toBeInTheDocument());
  });

  it('shows red X on heads when head tank size does not match shell', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      shellSize: '120',
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'HEMI 37" ID',
      cardColor: 'Red',
      tankSize: 500,
    });

    const { props, container } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());

    handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    await waitFor(() => expect(materialQueueApi.getCardLookup).toHaveBeenCalled());

    await waitFor(() => {
      const mismatchBoxes = container.querySelectorAll('[class*="headMismatch"]');
      expect(mismatchBoxes.length).toBe(2);
    });
  });

  it('shows correct number of shell slots after scanning a 1000-gal shell', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 1000,
      shellSize: '1000',
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());

    await waitFor(() => {
      expect(screen.getByText('Shell 1')).toBeInTheDocument();
      expect(screen.getByText('Shell 2')).toBeInTheDocument();
    });
    expect(screen.getByText('SH001')).toBeInTheDocument();
    expect(screen.getByText('Scan Shell')).toBeInTheDocument();
  });

  it('handles tank size override via barcode', () => {
    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (handler) {
      handler({ prefix: 'TS', value: '1500', raw: 'TS;1500' }, 'TS;1500');
    }
    expect(props.showScanResult).toHaveBeenCalledWith(
      expect.objectContaining({ message: expect.stringContaining('1500') }),
    );
  });

  it('sends head heat/coil data when creating assembly', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardId: '03',
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'asm1',
      alphaCode: 'AA',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());
    handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    await waitFor(() => expect(materialQueueApi.getCardLookup).toHaveBeenCalled());
    handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');

    await waitFor(() => expect(assemblyApi.create).toHaveBeenCalledWith(
      expect.objectContaining({
        leftHeadHeatNumber: 'HEAT01',
        leftHeadCoilNumber: 'COIL01',
        rightHeadHeatNumber: 'HEAT01',
        rightHeadCoilNumber: 'COIL01',
      }),
    ));
  });

  it('preserves head lot after reset so operator does not need to rescan KC', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'HEMI 37" ID',
      cardColor: 'Red',
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());

    handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    await waitFor(() => expect(materialQueueApi.getCardLookup).toHaveBeenCalled());

    await waitFor(() => {
      expect(screen.getAllByText(/HEMI 37/).length).toBe(2);
    });

    handler({ prefix: 'INP', value: '2', raw: 'INP;2' }, 'INP;2');

    await waitFor(() => {
      expect(screen.getAllByText(/HEMI 37/).length).toBe(2);
    });
  });
});
