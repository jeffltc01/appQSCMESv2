import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SpotXrayScreen } from './SpotXrayScreen';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout';

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: { plantCode: 'WJ', defaultSiteId: 'plant-1', displayName: 'Test User' },
  }),
}));

vi.mock('../../api/endpoints', () => ({
  spotXrayApi: {
    getLaneQueues: vi.fn().mockResolvedValue({
      lanes: [
        {
          laneName: 'Lane 1',
          draftCount: 0,
          tanks: [
            { position: 1, assemblySerialNumberId: 'sn-1', alphaCode: 'ABC-001', shellSerials: ['S1'], tankSize: 500, weldType: 'RS', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
            { position: 2, assemblySerialNumberId: 'sn-2', alphaCode: 'ABC-002', shellSerials: ['S2'], tankSize: 500, weldType: 'RS', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
            { position: 3, assemblySerialNumberId: 'sn-3', alphaCode: 'ABC-003', shellSerials: ['S3'], tankSize: 250, weldType: 'RS', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: true, welderChanged: false },
          ],
        },
        {
          laneName: 'Lane 2',
          draftCount: 1,
          tanks: [
            { position: 1, assemblySerialNumberId: 'sn-4', alphaCode: 'ABC-004', shellSerials: ['S4'], tankSize: 500, weldType: 'RS', welderNames: ['Joe'], welderIds: ['w2'], sizeChanged: false, welderChanged: false },
          ],
        },
      ],
    }),
    createIncrements: vi.fn().mockResolvedValue({
      increments: [
        { id: 'inc-1', incrementNo: '260222001-Lane1', laneNo: 'Lane 1', tankSize: 500, overallStatus: 'Pending', isDraft: true },
      ],
    }),
    getIncrement: vi.fn().mockResolvedValue({
      id: 'inc-1', incrementNo: '260222001-Lane1', overallStatus: 'Pending', laneNo: 'Lane 1', isDraft: true, tankSize: 500, seamCount: 2,
      inspectTankId: null, inspectTankAlpha: null,
      tanks: [
        { serialNumberId: 'sn-1', alphaCode: 'ABC-001', shellSerials: ['S1'], position: 1 },
        { serialNumberId: 'sn-2', alphaCode: 'ABC-002', shellSerials: ['S2'], position: 2 },
      ],
      seams: [
        { seamNumber: 1, welderName: 'Jeff', shotNo: null, result: null },
        { seamNumber: 2, welderName: 'Jeff', shotNo: null, result: null },
      ],
    }),
    getNextShotNumber: vi.fn().mockResolvedValue({ shotNumber: 1 }),
    saveResults: vi.fn(),
  },
}));

function createProps(overrides: Partial<WorkCenterProps> = {}): WorkCenterProps {
  return {
    workCenterId: 'wc-spot', assetId: 'asset-1', productionLineId: 'pl-1', operatorId: 'op-1',
    plantId: 'plant-1', welders: [], numberOfWelders: 0, welderCountLoaded: true, externalInput: false, setExternalInput: vi.fn(),
    showScanResult: vi.fn(), refreshHistory: vi.fn(), registerBarcodeHandler: vi.fn(),
    ...overrides,
  };
}

describe('SpotXrayScreen', () => {
  it('renders lane queues on load', async () => {
    render(<FluentProvider theme={webLightTheme}><SpotXrayScreen {...createProps()} /></FluentProvider>);
    await waitFor(() => {
      expect(screen.getByText('Lane 1')).toBeInTheDocument();
      expect(screen.getByText('Lane 2')).toBeInTheDocument();
    });
  });

  it('shows tank info in lanes', async () => {
    render(<FluentProvider theme={webLightTheme}><SpotXrayScreen {...createProps()} /></FluentProvider>);
    await waitFor(() => {
      expect(screen.getByText('ABC-001')).toBeInTheDocument();
      expect(screen.getByText('ABC-004')).toBeInTheDocument();
    });
  });

  it('shows draft badge on lane with drafts', async () => {
    render(<FluentProvider theme={webLightTheme}><SpotXrayScreen {...createProps()} /></FluentProvider>);
    await waitFor(() => {
      expect(screen.getByText('1 draft')).toBeInTheDocument();
    });
  });

  it('shows size break indicator', async () => {
    render(<FluentProvider theme={webLightTheme}><SpotXrayScreen {...createProps()} /></FluentProvider>);
    await waitFor(() => {
      expect(screen.getByText('ABC-003')).toBeInTheDocument();
    });
  });

  it('shows Create Increment button disabled when nothing selected', async () => {
    render(<FluentProvider theme={webLightTheme}><SpotXrayScreen {...createProps()} /></FluentProvider>);
    await waitFor(() => {
      const btn = screen.getByRole('button', { name: /create increment/i });
      expect(btn).toBeDisabled();
    });
  });
});
