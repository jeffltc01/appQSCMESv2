import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { AIReviewScreen } from './AIReviewScreen';
import type { AIReviewRecord } from '../../types/domain';

vi.mock('../../api/endpoints', () => ({
  workCenterApi: {
    getWorkCenters: vi.fn().mockResolvedValue([
      { id: 'wc-1', name: 'Rolls', workCenterTypeId: 'wct-1', workCenterTypeName: 'Production', numberOfWelders: 0 },
      { id: 'wc-2', name: 'Round Seam Insp', workCenterTypeId: 'wct-2', workCenterTypeName: 'Inspection', numberOfWelders: 0 },
    ]),
  },
  aiReviewApi: {
    getRecords: vi.fn().mockResolvedValue([]),
    submitReview: vi.fn().mockResolvedValue({ annotationsCreated: 0 }),
  },
  siteApi: { getSites: vi.fn().mockResolvedValue([]) },
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'ai-user-1', displayName: 'AI Inspector', roleTier: 5.5,
      plantCode: 'PLT1', plantName: 'Plant 1', defaultSiteId: 'site-1',
    },
    isAuthenticated: true,
    logout: vi.fn(),
  }),
}));

const { aiReviewApi } = await import('../../api/endpoints');

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <AIReviewScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

const mockRecords: AIReviewRecord[] = [
  {
    id: 'rec-1', timestamp: '2026-02-21T14:00:00Z',
    serialOrIdentifier: 'SN-001', tankSize: '120',
    operatorName: 'Jane Doe', alreadyReviewed: false,
  },
  {
    id: 'rec-2', timestamp: '2026-02-21T13:30:00Z',
    serialOrIdentifier: 'SN-002', tankSize: '500',
    operatorName: 'Bob Smith', alreadyReviewed: true,
  },
];

describe('AIReviewScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows prompt to select a work center initially', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/select a work center to begin/i)).toBeInTheDocument();
    });
  });

  it('renders work center dropdown with options', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/select a work center to begin/i)).toBeInTheDocument();
    });
    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('loads and displays records when work center is selected', async () => {
    vi.mocked(aiReviewApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
      expect(screen.getByText('SN-002')).toBeInTheDocument();
      expect(screen.getByText('Jane Doe')).toBeInTheDocument();
      expect(screen.getByText('Bob Smith')).toBeInTheDocument();
    });
  });

  it('shows AI Reviewed badge for already-reviewed records', async () => {
    vi.mocked(aiReviewApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('AI Reviewed')).toBeInTheDocument();
    });
  });

  it('calls submitReview with checked record IDs and comment', async () => {
    vi.mocked(aiReviewApi.getRecords).mockResolvedValue(mockRecords);
    vi.mocked(aiReviewApi.submitReview).mockResolvedValue({ annotationsCreated: 1 });
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const checkboxes = screen.getAllByRole('checkbox');
    const enabledCheckbox = checkboxes.find((cb) => !cb.hasAttribute('disabled'));
    expect(enabledCheckbox).toBeDefined();
    await user.click(enabledCheckbox!);

    const commentBox = screen.getByPlaceholderText(/add a note/i);
    await user.type(commentBox, 'Good weld quality');

    const submitBtn = screen.getByRole('button', { name: /mark 1 as reviewed/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(aiReviewApi.submitReview).toHaveBeenCalledWith({
        productionRecordIds: ['rec-1'],
        comment: 'Good weld quality',
      });
    });
  });

  it('shows success message after submitting review', async () => {
    vi.mocked(aiReviewApi.getRecords).mockResolvedValue(mockRecords);
    vi.mocked(aiReviewApi.submitReview).mockResolvedValue({ annotationsCreated: 1 });
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const checkboxes = screen.getAllByRole('checkbox');
    const enabledCheckbox = checkboxes.find((cb) => !cb.hasAttribute('disabled'));
    await user.click(enabledCheckbox!);

    const submitBtn = screen.getByRole('button', { name: /mark 1 as reviewed/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/1 record\(s\) marked as AI Reviewed/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no records for selected work center', async () => {
    vi.mocked(aiReviewApi.getRecords).mockResolvedValue([]);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText(/no production records today/i)).toBeInTheDocument();
    });
  });
});
