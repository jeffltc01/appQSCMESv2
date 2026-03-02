import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SpotXrayCreateView } from './SpotXrayCreateView';
import { spotXrayApi } from '../../api/endpoints';

vi.mock('../../api/endpoints', () => ({
  spotXrayApi: {
    getLaneQueues: vi.fn(),
    createIncrements: vi.fn(),
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
};

const mockLaneQueues = {
  lanes: [
    {
      laneName: 'Lane 1',
      draftCount: 0,
      tanks: [
        { position: 1, assemblySerialNumberId: 'sn-1', alphaCode: 'ABC-001', shellSerials: ['S1'], tankSize: 500, weldType: 'RS', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
        { position: 2, assemblySerialNumberId: 'sn-2', alphaCode: 'ABC-002', shellSerials: ['S2'], tankSize: 500, weldType: 'RS', welderNames: ['Jeff'], welderIds: ['w1'], sizeChanged: false, welderChanged: false },
      ],
    },
    {
      laneName: 'Lane 2',
      draftCount: 1,
      tanks: [
        { position: 1, assemblySerialNumberId: 'sn-3', alphaCode: 'ABC-003', shellSerials: ['S3'], tankSize: 250, weldType: 'RS', welderNames: ['Joe'], welderIds: ['w2'], sizeChanged: false, welderChanged: false },
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
      expect(screen.getByText('ABC-001')).toBeInTheDocument();
      expect(screen.getByText('ABC-003')).toBeInTheDocument();
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
});
