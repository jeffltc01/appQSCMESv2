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
  adminPlantGearApi: {
    getAll: vi.fn().mockResolvedValue([]),
  },
}));

const { serialNumberApi, materialQueueApi, assemblyApi, workCenterApi, adminPlantGearApi } = await import('../../api/endpoints');

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
    vi.mocked(adminPlantGearApi.getAll).mockResolvedValue([]);
  });

  it('uses materialQueueForWCId when loading heads queue', async () => {
    renderFitup({
      workCenterId: 'wc-current',
      materialQueueForWCId: 'wc-feed',
      productionLineId: 'pl-inside',
    });

    await waitFor(() => {
      expect(workCenterApi.getMaterialQueue).toHaveBeenCalledWith('wc-feed', 'fitup', 'pl-inside');
    });
  });

  it('shows only queued head rows in Heads Queue', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'mq-queued',
        position: 1,
        status: 'queued',
        cardId: '03',
        cardColor: 'Red',
        productDescription: 'Queued Head Material',
        shellSize: '120',
        heatNumber: 'HEAT01',
        coilNumber: 'COIL01',
        lotNumber: 'LOT01',
        quantity: 1,
        quantityCompleted: 0,
      },
      {
        id: 'mq-active',
        position: 2,
        status: 'active',
        cardId: '04',
        cardColor: 'Blue',
        productDescription: 'Active Head Material',
        shellSize: '120',
        heatNumber: 'HEAT02',
        coilNumber: 'COIL02',
        lotNumber: 'LOT02',
        quantity: 1,
        quantityCompleted: 0,
      },
    ]);

    renderFitup();

    expect(await screen.findByText(/queued head material/i)).toBeInTheDocument();
    expect(screen.queryByText(/active head material/i)).not.toBeInTheDocument();
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
      tankSize: 1000,
      existingAssembly: {
        alphaCode: 'AB',
        tankSize: 1000,
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

    expect(screen.getByRole('button', { name: /replace mode/i })).toBeInTheDocument();
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

  it('hides split controls for 500 gallon and under reassembly', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 500,
      existingAssembly: {
        alphaCode: 'AB',
        tankSize: 500,
        shells: ['SH001'],
      },
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

    expect(screen.queryByRole('button', { name: /split keep left/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /split keep right/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /split s1\|s2/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /split s2\|s3/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /replace mode/i })).not.toBeInTheDocument();
  });

  it('uses queued-head dropdown in reassembly and normalizes KC-prefixed card input', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValueOnce([
      {
        id: 'mq-queued',
        position: 1,
        status: 'queued',
        cardId: '03',
        cardColor: '#ff0000',
        productDescription: 'HEMI 37"',
        shellSize: '120',
        heatNumber: 'HEAT01',
        coilNumber: 'COIL01',
        lotNumber: 'LOT01',
        quantity: 1,
        quantityCompleted: 0,
      },
    ]);
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
      lotNumber: 'LOT01',
      productDescription: 'HEMI 37"',
      cardColor: '#ff0000',
      tankSize: 120,
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });
    await waitFor(() => expect(screen.getByText('Reassembly Mode')).toBeInTheDocument());

    await userEvent.click(screen.getAllByRole('button', { name: /select \+ kc/i })[0]);
    expect(screen.getByRole('combobox')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('combobox'));
    await userEvent.click(screen.getByText(/120 gal · lot lot01 · card 03/i));
    await userEvent.click(screen.getByRole('button', { name: /^apply$/i }));

    await waitFor(() => {
      expect(materialQueueApi.getCardLookup).toHaveBeenCalledWith('wc-fitup', 'pl-1', '03');
    });

    await act(async () => {
      handler({ prefix: 'KC', value: 'KC;03', raw: 'KC;KC;03' }, 'KC;KC;03');
    });

    await waitFor(() => {
      expect(materialQueueApi.getCardLookup).toHaveBeenLastCalledWith('wc-fitup', 'pl-1', '03');
    });
  });

  it('shows changed tag and guidance in reassembly mode', async () => {
    vi.mocked(serialNumberApi.getContext)
      .mockResolvedValueOnce({
        serialNumber: 'SH001',
        tankSize: 120,
        existingAssembly: {
          alphaCode: 'AB',
          tankSize: 120,
          shells: ['SH001'],
        },
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
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });

    await waitFor(() => {
      expect(screen.getByText(/click a component in proposed to select it for replacement/i)).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole('button', { name: 'SH001' }));
    await userEvent.type(screen.getByPlaceholderText(/enter shell serial/i), 'SH002');
    await userEvent.click(screen.getByRole('button', { name: /^apply$/i }));

    await waitFor(() => {
      expect(screen.getAllByText(/changed/i).length).toBeGreaterThan(0);
    });
  });

  it('reset scan clears selected heads', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 120,
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });

    await waitFor(() => expect(screen.getAllByText(/head material/i).length).toBeGreaterThan(0));

    await act(async () => {
      handler({ prefix: 'INP', value: '2', raw: 'INP;2' }, 'INP;2');
    });

    await waitFor(() => expect(screen.getAllByText('Scan KC').length).toBeGreaterThan(0));
  });

  it('keeps selected heads after save when starting next assembly', async () => {
    vi.mocked(workCenterApi.getMaterialQueue).mockResolvedValue([
      {
        id: 'mq-1',
        position: 1,
        status: 'queued',
        cardId: '03',
        cardColor: 'Red',
        productDescription: 'Head Material',
        shellSize: '120',
        heatNumber: 'HEAT01',
        coilNumber: 'COIL01',
        lotNumber: 'LOT01',
        quantity: 1,
        quantityCompleted: 0,
      },
    ]);
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 120,
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'assembly-1',
      alphaCode: 'AB',
      timestamp: new Date().toISOString(),
    });

    renderFitup({ externalInput: false });

    const shellInput = screen.getByPlaceholderText(/shell serial number/i);
    await userEvent.type(shellInput, 'SH001');
    await userEvent.click(screen.getByRole('button', { name: /add shell/i }));

    const headQueueCard = screen.getByRole('button', { name: /card/i });
    await userEvent.click(headQueueCard);

    await waitFor(() => {
      expect(screen.queryAllByText('Scan KC')).toHaveLength(0);
    });

    await userEvent.click(screen.getByRole('button', { name: /^save$/i }));
    await waitFor(() => expect(screen.getByText('AB')).toBeInTheDocument());

    await userEvent.click(screen.getByRole('button', { name: /next assembly/i }));

    await waitFor(() => {
      expect(screen.queryAllByText('Scan KC')).toHaveLength(0);
    });
  });

  it('refreshes Building Assembly code after save and next assembly', async () => {
    vi.mocked(adminPlantGearApi.getAll)
      .mockResolvedValueOnce([
        {
          plantId: 'plant-1',
          plantName: 'Plant 1',
          plantCode: '000',
          nextTankAlphaCode: 'DD',
          gears: [],
        },
      ])
      .mockResolvedValueOnce([
        {
          plantId: 'plant-1',
          plantName: 'Plant 1',
          plantCode: '000',
          nextTankAlphaCode: 'DE',
          gears: [],
        },
      ]);
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 500,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 500,
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'assembly-1',
      alphaCode: 'DD',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup({ externalInput: false });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });

    await waitFor(() => expect(screen.getByText('DD')).toBeInTheDocument());
    await userEvent.click(screen.getByRole('button', { name: /next assembly/i }));

    await waitFor(() => {
      expect(screen.getByText('Building Assembly:')).toBeInTheDocument();
      expect(screen.getByText('DE')).toBeInTheDocument();
    });
  });

  it('accepts head scan as first scan after save screen', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 120,
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'assembly-1',
      alphaCode: 'AB',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });

    await waitFor(() => expect(screen.getByText('AB')).toBeInTheDocument());

    await act(async () => {
      handler({ prefix: 'KC', value: '04', raw: 'KC;04' }, 'KC;04');
    });

    await waitFor(() => {
      expect(materialQueueApi.getCardLookup).toHaveBeenCalledTimes(2);
    });
  });

  it('accepts shell scan as first scan after save screen', async () => {
    vi.mocked(serialNumberApi.getContext)
      .mockResolvedValueOnce({
        serialNumber: 'SH001',
        tankSize: 120,
      })
      .mockResolvedValueOnce({
        serialNumber: 'SH002',
        tankSize: 120,
      });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 120,
    });
    vi.mocked(assemblyApi.create).mockResolvedValue({
      id: 'assembly-1',
      alphaCode: 'AB',
      timestamp: new Date().toISOString(),
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });
    await act(async () => {
      handler({ prefix: 'INP', value: '3', raw: 'INP;3' }, 'INP;3');
    });

    await waitFor(() => expect(screen.getByText('AB')).toBeInTheDocument());

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH002', raw: 'SC;SH002' }, 'SC;SH002');
    });

    await waitFor(() => {
      expect(serialNumberApi.getContext).toHaveBeenCalledTimes(2);
      expect(serialNumberApi.getContext).toHaveBeenLastCalledWith('SH002');
    });
  });

  it('defaults tank size from head card only when empty', async () => {
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 500,
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });

    await waitFor(() => {
      expect(screen.getByText('500')).toBeInTheDocument();
    });
  });

  it('does not overwrite existing tank size when selecting head card', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 500,
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });
    await waitFor(() => expect(screen.getByText('120')).toBeInTheDocument());

    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });

    await waitFor(() => {
      expect(screen.getByText('120')).toBeInTheDocument();
    });
    expect(screen.queryByText('500')).not.toBeInTheDocument();
  });

  it('shows green check marker for valid shell slot', async () => {
    vi.mocked(serialNumberApi.getContext).mockResolvedValue({
      serialNumber: 'SH001',
      tankSize: 120,
    });

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'SC', value: 'SH001', raw: 'SC;SH001' }, 'SC;SH001');
    });

    await waitFor(() => {
      expect(screen.getByLabelText('Shell good')).toBeInTheDocument();
    });
  });

  it('shows Building Assembly next alpha after scan starts', async () => {
    vi.mocked(materialQueueApi.getCardLookup).mockResolvedValue({
      heatNumber: 'HEAT01',
      coilNumber: 'COIL01',
      productDescription: 'Head Material',
      cardColor: 'Red',
      tankSize: 500,
    });
    vi.mocked(adminPlantGearApi.getAll).mockResolvedValue([
      {
        plantId: 'plant-1',
        plantName: 'Plant 1',
        plantCode: '000',
        nextTankAlphaCode: 'BG',
        gears: [],
      },
    ]);

    const { props } = renderFitup({ externalInput: true });
    const handler = vi.mocked(props.registerBarcodeHandler).mock.calls[0]?.[0];
    if (!handler) throw new Error('no handler');

    await act(async () => {
      handler({ prefix: 'KC', value: '03', raw: 'KC;03' }, 'KC;03');
    });

    await waitFor(() => {
      expect(screen.getByText('Building Assembly:')).toBeInTheDocument();
      expect(screen.getByText('BG')).toBeInTheDocument();
    });
  });
});
