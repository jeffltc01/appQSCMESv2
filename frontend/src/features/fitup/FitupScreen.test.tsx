import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
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

const { serialNumberApi, materialQueueApi, assemblyApi, workCenterApi } = await import('../../api/endpoints');

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
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValue([]);
  });

  it('duplicate shell scan enters reassembly prompt', async () => {
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
      await act(async () => {
        handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
      });
    }

    await waitFor(() => {
      expect(screen.getByText(/reassembling/i)).toBeInTheDocument();
    });
  });

  it('entering reassembly disables external input and cancel restores it', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      existingAssembly: {
        alphaCode: 'AB',
        tankSize: 120,
        shells: ['SH001'],
      },
    });

    const setExternalInput = vi.fn();
    const { props } = renderFitup({ externalInput: true, setExternalInput });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });
    await waitFor(() => expect(setExternalInput).toHaveBeenCalledWith(false));
    expect(screen.getByText('Reassembly Mode')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /cancel reassembly/i }));
    await waitFor(() => expect(setExternalInput).toHaveBeenCalledWith(true));
  });

  it('slot targeted replacement updates reassembly payload', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      existingAssembly: {
        alphaCode: 'AB',
        tankSize: 120,
        shells: ['SH001'],
      },
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
    });
    vi.mocked(assemblyApi.reassemble).mockResolvedValue({
      sourceAlphaCode: 'AB',
      createdAssemblies: [{ id: 'new-1', alphaCode: 'AC', timestamp: new Date().toISOString() }],
    });
    vi.mocked(serialNumberApi.getContext)
      .mockResolvedValueOnce({
        serialNumber: 'SH001',
        tankSize: 120,
        existingAssembly: { alphaCode: 'AB', tankSize: 120, shells: ['SH001'] },
      })
      .mockResolvedValueOnce({
        serialNumber: 'SH002',
        tankSize: 120,
      });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());

    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3'); // enter mode
    });
    await waitFor(() => expect(screen.getByText('Proposed A')).toBeInTheDocument());

    await userEvent.click(screen.getByRole('button', { name: 'SH001' }));
    const input = screen.getByPlaceholderText(/enter shell serial/i);
    await userEvent.type(input, 'SH002');
    await userEvent.click(screen.getByRole('button', { name: /^apply$/i }));

    await waitFor(() => expect(screen.getByText('SH002')).toBeInTheDocument());
    await userEvent.click(screen.getByRole('button', { name: /create reassembly/i }));

    await waitFor(() => expect(assemblyApi.reassemble).toHaveBeenCalledWith(
      'AB',
      expect.objectContaining({
        operationType: 'replace',
        primaryAssembly: expect.objectContaining({ shells: ['SH002'] }),
      }),
    ));
  });

  it('split mode sends secondary assembly payload', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
      existingAssembly: {
        alphaCode: 'AB',
        tankSize: 120,
        shells: ['SH001', 'SH002'],
      },
    });
    vi.mocked(assemblyApi.reassemble).mockResolvedValue({
      sourceAlphaCode: 'AB',
      createdAssemblies: [
        { id: 'new-1', alphaCode: 'AC', timestamp: new Date().toISOString() },
        { id: 'new-2', alphaCode: 'AD', timestamp: new Date().toISOString() },
      ],
    });

    const { props } = renderFitup();
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await waitFor(() => expect(serialNumberApi.getContext).toHaveBeenCalled());
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });

    await userEvent.click(screen.getByRole('button', { name: /split keep left/i }));
    await userEvent.click(screen.getByRole('button', { name: /create reassembly/i }));

    await waitFor(() => expect(assemblyApi.reassemble).toHaveBeenCalledWith(
      'AB',
      expect.objectContaining({
        operationType: 'split',
        secondaryAssembly: expect.objectContaining({ shells: ['SH002'] }),
      }),
    ));
  });
});
