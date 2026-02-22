import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { IssueApprovalsScreen } from './IssueApprovalsScreen';
import { IssueRequestType, IssueRequestStatus } from '../../types/api';
import type { IssueRequestDto } from '../../types/api';

vi.mock('../../api/endpoints', () => ({
  issueRequestApi: {
    getPending: vi.fn().mockResolvedValue([]),
    approve: vi.fn().mockResolvedValue({}),
    reject: vi.fn().mockResolvedValue({}),
    getMine: vi.fn().mockResolvedValue([]),
    submit: vi.fn().mockResolvedValue({}),
  },
  siteApi: { getSites: vi.fn().mockResolvedValue([]) },
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'qm-1', displayName: 'QM User', roleTier: 3, plantCode: 'PLT1', plantName: 'Plant 1' },
    isAuthenticated: true,
    logout: vi.fn(),
  }),
}));

const { issueRequestApi } = await import('../../api/endpoints');

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <IssueApprovalsScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

const pendingItems: IssueRequestDto[] = [
  {
    id: 'ir-1', type: IssueRequestType.Bug, status: IssueRequestStatus.Pending,
    title: 'Button broken', area: 'Login / Authentication',
    bodyJson: JSON.stringify({ description: 'Cannot click login', steps: '1. Open app\n2. Click login', expected: 'Login works', actual: 'Nothing', browser: 'Chrome', severity: 'High' }),
    submittedByUserId: 'user-1', submittedByName: 'Jane Operator',
    submittedAt: '2026-02-20T10:00:00Z',
  },
  {
    id: 'ir-2', type: IssueRequestType.FeatureRequest, status: IssueRequestStatus.Pending,
    title: 'Dark mode', area: 'Menu / Navigation',
    bodyJson: JSON.stringify({ problem: 'Bright screen', solution: 'Add dark mode', priority: 'Nice to have' }),
    submittedByUserId: 'user-2', submittedByName: 'Bob Welder',
    submittedAt: '2026-02-21T08:00:00Z',
  },
];

describe('IssueApprovalsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows empty state when no pending requests', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/no pending issue requests/i)).toBeInTheDocument();
    });
  });

  it('displays pending requests as cards', async () => {
    vi.mocked(issueRequestApi.getPending).mockResolvedValue(pendingItems);

    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Button broken')).toBeInTheDocument();
      expect(screen.getByText('Dark mode')).toBeInTheDocument();
      expect(screen.getByText('Jane Operator')).toBeInTheDocument();
      expect(screen.getByText('Bob Welder')).toBeInTheDocument();
    });
  });

  it('opens review view when card is clicked', async () => {
    vi.mocked(issueRequestApi.getPending).mockResolvedValue(pendingItems);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Button broken')).toBeInTheDocument();
    });

    await user.click(screen.getByText('Button broken'));

    expect(screen.getByText('Back to List')).toBeInTheDocument();
    expect(screen.getByText(/approve & create github issue/i)).toBeInTheDocument();
    expect(screen.getByText('Reject')).toBeInTheDocument();
    expect(screen.getByText('Cannot click login')).toBeInTheDocument();
  });

  it('calls approve API when Approve button is clicked', async () => {
    vi.mocked(issueRequestApi.getPending).mockResolvedValue(pendingItems);
    vi.mocked(issueRequestApi.approve).mockResolvedValue({
      ...pendingItems[0], status: IssueRequestStatus.Approved,
      gitHubIssueNumber: 10, gitHubIssueUrl: 'https://github.com/test/issues/10',
    });

    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Button broken')).toBeInTheDocument();
    });
    await user.click(screen.getByText('Button broken'));
    await user.click(screen.getByText(/approve & create github issue/i));

    await waitFor(() => {
      expect(issueRequestApi.approve).toHaveBeenCalledWith('ir-1', { reviewerUserId: 'qm-1' });
    });
  });

  it('opens reject dialog with notes field', async () => {
    vi.mocked(issueRequestApi.getPending).mockResolvedValue(pendingItems);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Button broken')).toBeInTheDocument();
    });
    await user.click(screen.getByText('Button broken'));
    await user.click(screen.getByText('Reject'));

    expect(screen.getByText('Reject Issue Request')).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/reason for rejection/i)).toBeInTheDocument();
  });
});
