import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SpotXrayCreateView } from './SpotXrayCreateView';
import { spotXrayApi } from '../../api/endpoints';

vi.mock('../../api/endpoints', () => ({
  spotXrayApi: {
    getLaneQueues: vi.fn(),
    createIncrements: vi.fn(),
    getDraftIncrements: vi.fn(),
  },
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: { plantCode: 'WJ', defaultSiteId: 'plant-1', displayName: 'Test User' },
  }),
}));

const defaultProps: Parameters<typeof SpotXrayCreateView>[0] = {
  workCenterId: 'wc-spot',
  productionLineId: 'pl-1',
  operatorId: 'op-1',
  onIncrementsCreated: vi.fn(),
  onOpenDraft: vi.fn(),
};

const mockLaneQueues = {
  lanes: [
    {
      laneName: 'Lane 1',
      draftCount: 0,
      tanks: [
        { position: 1, assemblySerialNumberId: 'sn-1', alphaCode: 'ABC-001', shellSerials: ['S1'], tankSize: 500, weldType: 'RS Lane 1', roundSeamWeldedAtUtc: '2026-03-02T12:00:00Z', seamWelders: 'RS1: Jeff | RS2: Jeff', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
        { position: 2, assemblySerialNumberId: 'sn-2', alphaCode: 'ABC-002', shellSerials: ['S2'], tankSize: 500, weldType: 'RS Lane 1', roundSeamWeldedAtUtc: '2026-03-02T12:10:00Z', seamWelders: 'RS1: Jeff | RS2: Jeff', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
      ],
    },
    {
      laneName: 'Lane 2',
      draftCount: 1,
      tanks: [
        { position: 1, assemblySerialNumberId: 'sn-3', alphaCode: 'ABC-003', shellSerials: ['S3'], tankSize: 250, weldType: 'RS Lane 2', roundSeamWeldedAtUtc: '2026-03-02T12:20:00Z', seamWelders: 'RS1: Joe | RS2: Joe', welderNames: ['Joe'], welderIds: ['w2'], sizeChanged: false, welderChanged: false },
      ],
    },
  ],
};

function renderView(props: Partial<Parameters<typeof SpotXrayCreateView>[0]> = {}) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <SpotXrayCreateView {...defaultProps} {...props} />
    </FluentProvider>,
  );
}

describe('SpotXrayCreateView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders loading spinner while fetching', () => {
    vi.mocked(spotXrayApi.getLaneQueues).mockReturnValue(new Promise(() => {}));
    renderView();
    expect(screen.getByText('Loading lane queues...')).toBeInTheDocument();
  });

  it('renders lane columns after data loads', async () => {
    vi.mocked(spotXrayApi.getLaneQueues).mockResolvedValue(mockLaneQueues);
    renderView();
    await waitFor(() => {
      expect(screen.getByText('Lane 1')).toBeInTheDocument();
      expect(screen.getByText('Lane 2')).toBeInTheDocument();
      expect(screen.getByText('ABC-001 (S1)')).toBeInTheDocument();
      expect(screen.getByText('ABC-003 (S3)')).toBeInTheDocument();
      expect(screen.getAllByText('Round Seam Date/Time').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Seam Welders').length).toBeGreaterThan(0);
      expect(screen.getAllByText('RS1: Jeff | RS2: Jeff').length).toBeGreaterThan(0);
      expect(screen.getByText('RS1: Joe | RS2: Joe')).toBeInTheDocument();
      expect(screen.getByText(/Auto-refresh every 15s/i)).toBeInTheDocument();
    });
  });

  it('shows empty message when no lanes/tanks', async () => {
    vi.mocked(spotXrayApi.getLaneQueues).mockResolvedValue({ lanes: [] });
    renderView();
    await waitFor(() => {
      expect(screen.getByText('No tanks available in any lane.')).toBeInTheDocument();
    });
  });

  it('shows API error message when lane queue load fails', async () => {
    vi.mocked(spotXrayApi.getLaneQueues).mockRejectedValue({ message: 'Plant PLT1 not found' });
    renderView();
    await waitFor(() => {
      expect(screen.getByText('Plant PLT1 not found')).toBeInTheDocument();
    });
  });

  it('Create button is disabled when no tanks selected', async () => {
    vi.mocked(spotXrayApi.getLaneQueues).mockResolvedValue(mockLaneQueues);
    renderView();
    await waitFor(() => {
      const btn = screen.getByRole('button', { name: /create increment/i });
      expect(btn).toBeDisabled();
    });
  });

  it('auto-refreshes lane queues and preserves valid selections', async () => {
    const intervalCallbacks: Array<() => void> = [];
    vi.spyOn(window, 'setInterval').mockImplementation((handler: TimerHandler) => {
      if (typeof handler === 'function') {
        intervalCallbacks.push(handler as () => void);
      }
      return intervalCallbacks.length;
    });
    vi.spyOn(window, 'clearInterval').mockImplementation(() => undefined);

    vi.mocked(spotXrayApi.getLaneQueues)
      .mockResolvedValueOnce(mockLaneQueues)
      .mockResolvedValueOnce({
        lanes: [
          {
            laneName: 'Lane 1',
            draftCount: 0,
            tanks: [
              { position: 1, assemblySerialNumberId: 'sn-1', alphaCode: 'ABC-001', shellSerials: ['S1'], tankSize: 500, weldType: 'RS Lane 1', roundSeamWeldedAtUtc: '2026-03-02T12:00:00Z', seamWelders: 'RS1: Jeff | RS2: Jeff', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
              { position: 2, assemblySerialNumberId: 'sn-2', alphaCode: 'ABC-002', shellSerials: ['S2'], tankSize: 500, weldType: 'RS Lane 1', roundSeamWeldedAtUtc: '2026-03-02T12:10:00Z', seamWelders: 'RS1: Jeff | RS2: Jeff', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
              { position: 3, assemblySerialNumberId: 'sn-9', alphaCode: 'ABC-009', shellSerials: ['S9'], tankSize: 500, weldType: 'RS Lane 1', roundSeamWeldedAtUtc: '2026-03-02T12:30:00Z', seamWelders: 'RS1: Jeff | RS2: Jeff', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
            ],
          },
          {
            laneName: 'Lane 2',
            draftCount: 1,
            tanks: [
              { position: 1, assemblySerialNumberId: 'sn-3', alphaCode: 'ABC-003', shellSerials: ['S3'], tankSize: 250, weldType: 'RS Lane 2', roundSeamWeldedAtUtc: '2026-03-02T12:20:00Z', seamWelders: 'RS1: Joe | RS2: Joe', welderNames: ['Joe'], welderIds: ['w2'], sizeChanged: false, welderChanged: false },
            ],
          },
        ],
      });

    renderView();

    await waitFor(() => {
      expect(screen.getByText('ABC-001 (S1)')).toBeInTheDocument();
    });
    expect(intervalCallbacks.length).toBeGreaterThan(0);

    fireEvent.click(screen.getByText('ABC-001 (S1)'));
    expect(screen.getByText('1 selected (max 5)')).toBeInTheDocument();

    await act(async () => {
      for (const callback of intervalCallbacks) {
        callback();
      }
    });

    await waitFor(() => {
      expect(spotXrayApi.getLaneQueues).toHaveBeenCalledTimes(2);
      expect(screen.getByText('ABC-009 (S9)')).toBeInTheDocument();
      expect(screen.getByText('1 selected (max 5)')).toBeInTheDocument();
    });
  });

  it('shows drafts list and opens selected draft', async () => {
    vi.mocked(spotXrayApi.getLaneQueues).mockResolvedValue(mockLaneQueues);
    vi.mocked(spotXrayApi.getDraftIncrements).mockResolvedValue([
      { id: 'inc-1', incrementNo: '2602220001-Lane1', laneNo: 'Lane 1', tankSize: 500, overallStatus: 'Pending', isDraft: true },
    ]);
    const onOpenDraft = vi.fn();
    renderView({ onOpenDraft });

    await waitFor(() => {
      expect(screen.getByText('Lane 1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Drafts' }));

    await waitFor(() => {
      expect(screen.getByText('2602220001-Lane1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Open' }));
    expect(onOpenDraft).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'inc-1', isDraft: true }),
    );
  });
});
