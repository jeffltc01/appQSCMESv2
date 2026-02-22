import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { IssueRequestDialog } from './IssueRequestDialog';
import { IssueRequestStatus, IssueRequestType } from '../../types/api';
import type { IssueRequestDto } from '../../types/api';

vi.mock('../../api/endpoints', () => ({
  issueRequestApi: {
    getMine: vi.fn().mockResolvedValue([]),
    submit: vi.fn().mockResolvedValue({}),
    getPending: vi.fn().mockResolvedValue([]),
    approve: vi.fn().mockResolvedValue({}),
    reject: vi.fn().mockResolvedValue({}),
  },
}));

const { issueRequestApi } = await import('../../api/endpoints');

function renderDialog(props: Partial<Parameters<typeof IssueRequestDialog>[0]> = {}) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <IssueRequestDialog
        open={true}
        onClose={vi.fn()}
        userId="user-1"
        roleTier={6}
        {...props}
      />
    </FluentProvider>,
  );
}

describe('IssueRequestDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the list view with empty state', async () => {
    renderDialog();
    await waitFor(() => {
      expect(screen.getByText(/no issue requests submitted yet/i)).toBeInTheDocument();
    });
  });

  it('shows past submissions in the list', async () => {
    const mockRequests: IssueRequestDto[] = [
      {
        id: '1', type: IssueRequestType.Bug, status: IssueRequestStatus.Pending,
        title: 'Scan broken', area: 'Scan Overlay', bodyJson: '{}',
        submittedByUserId: 'user-1', submittedByName: 'John',
        submittedAt: '2026-02-20T00:00:00Z',
      },
      {
        id: '2', type: IssueRequestType.FeatureRequest, status: IssueRequestStatus.Approved,
        title: 'Add dashboard', area: 'Menu / Navigation', bodyJson: '{}',
        submittedByUserId: 'user-1', submittedByName: 'John',
        submittedAt: '2026-02-19T00:00:00Z', gitHubIssueNumber: 42,
        gitHubIssueUrl: 'https://github.com/test/repo/issues/42',
      },
    ];
    vi.mocked(issueRequestApi.getMine).mockResolvedValue(mockRequests);

    renderDialog();
    await waitFor(() => {
      expect(screen.getByText('Scan broken')).toBeInTheDocument();
      expect(screen.getByText('Add dashboard')).toBeInTheDocument();
      expect(screen.getByText('Pending')).toBeInTheDocument();
      expect(screen.getByText('Approved')).toBeInTheDocument();
      expect(screen.getByText('#42')).toBeInTheDocument();
    });
  });

  it('opens the form when Report Issue is clicked', async () => {
    const user = userEvent.setup();
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText(/report issue/i)).toBeInTheDocument();
    });
    await user.click(screen.getByText(/report issue/i));

    expect(screen.getByText('New Bug Report')).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/brief summary/i)).toBeInTheDocument();
  });

  it('switches between issue type forms', async () => {
    const user = userEvent.setup();
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText(/report issue/i)).toBeInTheDocument();
    });
    await user.click(screen.getByText(/report issue/i));

    await user.click(screen.getByText('Feature Request'));
    expect(screen.getByText('New Feature Request')).toBeInTheDocument();
    expect(screen.getByText(/what problem does this solve/i)).toBeInTheDocument();

    await user.click(screen.getByText('General Question'));
    expect(screen.getByText('New General Question')).toBeInTheDocument();
    expect(screen.getByText(/your question/i)).toBeInTheDocument();
  });

  it('validates required fields before submitting', async () => {
    const user = userEvent.setup();
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText(/report issue/i)).toBeInTheDocument();
    });
    await user.click(screen.getByText(/report issue/i));
    await user.click(screen.getByText('Submit'));

    expect(screen.getByText(/title is required/i)).toBeInTheDocument();
    expect(issueRequestApi.submit).not.toHaveBeenCalled();
  });
});
