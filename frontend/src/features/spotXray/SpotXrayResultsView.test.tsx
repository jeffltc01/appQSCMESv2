import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SpotXrayResultsView } from './SpotXrayResultsView';
import { spotXrayApi } from '../../api/endpoints';
import type { SpotXrayIncrementSummary } from '../../types/domain';

vi.mock('../../api/endpoints', () => ({
  spotXrayApi: {
    getIncrement: vi.fn(),
    getNextShotNumber: vi.fn(),
    saveResults: vi.fn(),
  },
}));

const mockSummaries: SpotXrayIncrementSummary[] = [
  { id: 'inc-1', incrementNo: '260222001-L1', laneNo: 'Lane 1', tankSize: 500, overallStatus: 'Pending', isDraft: true },
  { id: 'inc-2', incrementNo: '260222002-L2', laneNo: 'Lane 2', tankSize: 250, overallStatus: 'Accept', isDraft: false },
];

const mockDetail = {
  id: 'inc-1',
  incrementNo: '260222001-L1',
  overallStatus: 'Pending',
  laneNo: 'Lane 1',
  isDraft: true,
  tankSize: 500,
  seamCount: 2,
  inspectTankId: null,
  inspectTankAlpha: null,
  tanks: [
    { serialNumberId: 'sn-1', alphaCode: 'ABC-001', shellSerials: ['S1'], position: 1 },
    { serialNumberId: 'sn-2', alphaCode: 'ABC-002', shellSerials: ['S2'], position: 2 },
  ],
  seams: [
    { seamNumber: 1, welderName: 'Jeff', shotNo: null, result: null },
    { seamNumber: 2, welderName: 'Jeff', shotNo: null, result: null },
  ],
};

const mockDetailFinalized = {
  ...mockDetail,
  id: 'inc-2',
  incrementNo: '260222002-L2',
  overallStatus: 'Accept',
  isDraft: false,
};

const defaultProps: Parameters<typeof SpotXrayResultsView>[0] = {
  incrementSummaries: mockSummaries,
  operatorId: 'op-1',
  plantId: 'plant-1',
  onBackToCreate: vi.fn(),
};

function renderView(props: Partial<Parameters<typeof SpotXrayResultsView>[0]> = {}) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <SpotXrayResultsView {...defaultProps} {...props} />
    </FluentProvider>,
  );
}

describe('SpotXrayResultsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state', () => {
    vi.mocked(spotXrayApi.getIncrement).mockReturnValue(new Promise(() => {}));
    renderView();
    expect(screen.getByText('Loading increment...')).toBeInTheDocument();
  });

  it('renders increment detail with seam cards after data loads', async () => {
    vi.mocked(spotXrayApi.getIncrement).mockResolvedValue(mockDetail);
    renderView();
    await waitFor(() => {
      expect(screen.getByText('260222001-L1')).toBeInTheDocument();
      expect(screen.getByText('Seam 1')).toBeInTheDocument();
      expect(screen.getByText('Seam 2')).toBeInTheDocument();
    });
  });

  it('tab switching fetches new increment detail', async () => {
    const user = userEvent.setup();
    vi.mocked(spotXrayApi.getIncrement).mockResolvedValue(mockDetail);
    renderView();

    await waitFor(() => {
      expect(screen.getByText('Seam 1')).toBeInTheDocument();
    });

    vi.mocked(spotXrayApi.getIncrement).mockResolvedValue(mockDetailFinalized);
    const tab2 = screen.getByText('Lane 2 — 260222002-L2');
    await user.click(tab2);

    await waitFor(() => {
      expect(spotXrayApi.getIncrement).toHaveBeenCalledWith('inc-2');
    });
  });

  it('shows finalized state message when isFinalized is true', async () => {
    vi.mocked(spotXrayApi.getIncrement).mockResolvedValue(mockDetailFinalized);
    renderView({ incrementSummaries: [mockSummaries[1]] });

    await waitFor(() => {
      const submitBtn = screen.getByRole('button', { name: /submit/i });
      expect(submitBtn).toBeDisabled();
      const draftBtn = screen.getByRole('button', { name: /save draft/i });
      expect(draftBtn).toBeDisabled();
    });
  });
});
