import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { LongSeamInspScreen } from './LongSeamInspScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../api/endpoints', () => ({
  serialNumberApi: {
    getContext: vi.fn(),
  },
  workCenterApi: {
    getDefectCodes: vi.fn().mockResolvedValue([
      { id: 'dc1', code: '042', name: 'Crack', severity: 'Major' },
      { id: 'dc2', code: '043', name: 'Porosity', severity: 'Minor' },
    ]),
    getDefectLocations: vi.fn().mockResolvedValue([
      { id: 'dl1', code: '003', name: 'Top' },
      { id: 'dl2', code: '004', name: 'Bottom' },
    ]),
    getCharacteristics: vi.fn().mockResolvedValue([
      { id: 'ch1', name: 'Long Seam' },
    ]),
  },
  inspectionRecordApi: {
    create: vi.fn(),
  },
}));

const { serialNumberApi, inspectionRecordApi } = await import('../../api/endpoints');

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-lsi',
    assetId: 'asset-1',
    productionLineId: 'pl-1',
    operatorId: 'op-1',
    welders: [],
    numberOfWelders: 0,
    externalInput: false,
    showScanResult: vi.fn(),
    refreshHistory: vi.fn(),
    registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

function renderInspection(overrides: Partial<WorkCenterProps> = {}) {
  const props = createProps(overrides);
  return {
    props,
    ...render(
      <FluentProvider theme={webLightTheme}>
        <LongSeamInspScreen {...props} />
      </FluentProvider>,
    ),
  };
}

describe('LongSeamInspScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('starts in WaitingForShell state', () => {
    renderInspection();
    expect(screen.getByText(/scan serial number/i)).toBeInTheDocument();
  });

  it('transitions to AwaitingDefects after shell scan', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      shellSize: '120',
    });

    renderInspection();
    const input = screen.getByPlaceholderText(/enter serial number/i);
    await user.type(input, 'SH001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(screen.getByText('AwaitingDefects')).toBeInTheDocument();
      expect(screen.getByText('SH001')).toBeInTheDocument();
    });
  });

  it('shows defect table headers in AwaitingDefects state', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });

    renderInspection();
    await user.type(screen.getByPlaceholderText(/enter serial number/i), 'SH001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(screen.getByText('AwaitingDefects')).toBeInTheDocument();
      expect(screen.getByText(/no defects/i)).toBeInTheDocument();
    });
  });

  it('shows empty defect message for clean pass', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });

    renderInspection();
    await user.type(screen.getByPlaceholderText(/enter serial number/i), 'SH001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(screen.getByText(/no defects/i)).toBeInTheDocument();
    });
  });

  it('saves clean pass with no defects', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(inspectionRecordApi.create).mockResolvedValue({
      id: 'ir1',
      serialNumber: 'SH001',
      workCenterId: 'wc-lsi',
      operatorId: 'op-1',
      timestamp: new Date().toISOString(),
      defects: [],
    });

    renderInspection();
    await user.type(screen.getByPlaceholderText(/enter serial number/i), 'SH001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(screen.getByText('AwaitingDefects')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(inspectionRecordApi.create).toHaveBeenCalledWith(
        expect.objectContaining({
          serialNumber: 'SH001',
          defects: [],
        }),
      );
    });
  });

  it('returns to WaitingForShell after save', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(inspectionRecordApi.create).mockResolvedValue({
      id: 'ir1',
      serialNumber: 'SH001',
      workCenterId: 'wc-lsi',
      operatorId: 'op-1',
      timestamp: new Date().toISOString(),
      defects: [],
    });

    renderInspection();
    await user.type(screen.getByPlaceholderText(/enter serial number/i), 'SH001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(screen.getByText('AwaitingDefects')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /^save$/i }));

    await waitFor(() => {
      expect(screen.getByText(/scan serial number/i)).toBeInTheDocument();
    });
  });

  it('has clear all button', async () => {
    const user = userEvent.setup();
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });

    renderInspection();
    await user.type(screen.getByPlaceholderText(/enter serial number/i), 'SH001');
    await user.click(screen.getByRole('button', { name: /submit/i }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /clear all/i })).toBeInTheDocument();
    });
  });

  it('registers barcode handler', () => {
    const { props } = renderInspection();
    expect(props.registerBarcodeHandler).toHaveBeenCalled();
  });

  it('disables input in external mode', () => {
    renderInspection({ externalInput: true });
    expect(screen.getByPlaceholderText(/enter serial number/i)).toBeDisabled();
  });
});
