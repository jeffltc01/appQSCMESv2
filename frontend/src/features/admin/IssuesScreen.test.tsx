import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
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

vi.mock('../../auth/AuthContext.tsx', () => ({
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
    const needsApprovalToggle = await screen.findByRole('switch', { name: /needs approval only/i });
    await user.click(needsApprovalToggle);
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
    const needsApprovalToggle = await screen.findByRole('switch', { name: /needs approval only/i });
    await user.click(needsApprovalToggle);
    await user.click(await screen.findByRole('button', { name: /review broken scanner/i }));
    expect(await screen.findByText(/Issue Details: Broken scanner/i)).toBeInTheDocument();
    expect(screen.getAllByText('Scanner stops after first scan').length).toBeGreaterThan(0);
  });

  it('opens details modal from view details button', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([
      {
        id: 'ir-2',
        type: IssueRequestType.FeatureRequest,
        status: IssueRequestStatus.Pending,
        title: 'Add export button',
        area: 'Admin - Products',
        bodyJson: JSON.stringify({
          problem: 'Need to export records quickly',
          solution: 'Add CSV export',
          priority: 'Important',
        }),
        submittedByUserId: 'qm-1',
        submittedByName: 'QM User',
        submittedAt: '2026-02-24T01:00:00Z',
      },
    ]);
    vi.mocked(issueRequestApi.getPending).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await user.click(await screen.findByRole('button', { name: /view details for add export button/i }));
    expect(await screen.findByText(/Issue Details: Add export button/i)).toBeInTheDocument();
    expect(screen.getAllByText('Need to export records quickly').length).toBeGreaterThan(0);
  });
});
