import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { IssuesScreen } from './IssuesScreen';
import { IssueRequestStatus, IssueRequestType } from '../../types/api';

vi.mock('../../api/endpoints', () => ({
  issueRequestApi: {
    getMine: vi.fn().mockResolvedValue([]),
    getPending: vi.fn().mockResolvedValue([]),
    submit: vi.fn().mockResolvedValue({}),
    approve: vi.fn().mockResolvedValue({}),
    reject: vi.fn().mockResolvedValue({}),
  },
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'qm-1',
      displayName: 'QM User',
      roleTier: 3,
      roleName: 'Quality Manager',
      plantCode: 'PLT1',
      plantName: 'Plant 1',
    },
    logout: vi.fn(),
  }),
}));

const { issueRequestApi } = await import('../../api/endpoints');

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <IssuesScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('IssuesScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders issues list title', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    renderScreen();
    expect(await screen.findByText('Issues')).toBeInTheDocument();
  });

  it('shows pending requests when Needs Approval Only is enabled', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    vi.mocked(issueRequestApi.getPending).mockResolvedValue([
      {
        id: 'ir-1',
        type: IssueRequestType.Bug,
        status: IssueRequestStatus.Pending,
        title: 'Broken scanner',
        area: 'Fitup Queue',
        bodyJson: '{}',
        submittedByUserId: 'u2',
        submittedByName: 'Operator A',
        submittedAt: '2026-02-24T01:00:00Z',
      },
    ]);
    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByText('Issues')).toBeInTheDocument());
    await user.click(screen.getByRole('switch', { name: /needs approval only/i }));
    expect(await screen.findByText('Broken scanner')).toBeInTheDocument();
  });

  it('opens review dialog from approval icon', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    vi.mocked(issueRequestApi.getPending).mockResolvedValue([
      {
        id: 'ir-1',
        type: IssueRequestType.Bug,
        status: IssueRequestStatus.Pending,
        title: 'Broken scanner',
        area: 'Fitup Queue',
        bodyJson: JSON.stringify({ description: 'Scanner stops after first scan' }),
        submittedByUserId: 'u2',
        submittedByName: 'Operator A',
        submittedAt: '2026-02-24T01:00:00Z',
      },
    ]);
    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByText('Issues')).toBeInTheDocument());
    await user.click(screen.getByRole('switch', { name: /needs approval only/i }));
    await user.click(await screen.findByRole('button', { name: /review broken scanner/i }));
    expect(await screen.findByText(/Review: Broken scanner/i)).toBeInTheDocument();
    expect(screen.getByText('Scanner stops after first scan')).toBeInTheDocument();
  });
});
