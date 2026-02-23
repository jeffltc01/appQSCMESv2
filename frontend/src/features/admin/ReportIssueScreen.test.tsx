import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { ReportIssueScreen } from './ReportIssueScreen';
import type { IssueRequestDto } from '../../types/api';

vi.mock('../../api/endpoints');
vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'u1',
      displayName: 'Test User',
      roleTier: 1,
      roleName: 'Admin',
      defaultSiteId: 'site-1',
      employeeNumber: 'EMP001',
      plantCode: 'TST',
      plantName: 'Test Plant',
      plantTimeZoneId: 'America/Denver',
      isCertifiedWelder: false,
      userType: 0,
    },
    logout: vi.fn(),
    login: vi.fn(),
    isAuthenticated: true,
    token: 'test-token',
    isWelder: false,
  }),
}));

vi.mock('../../utils/dateFormat', () => ({
  formatDateOnly: vi.fn((iso: string) => iso),
}));

vi.mock('./AdminLayout', () => ({
  AdminLayout: ({ children, title, onAdd, addLabel }: any) => (
    <div>
      <h1>{title}</h1>
      {onAdd && <button onClick={onAdd}>{addLabel}</button>}
      {children}
    </div>
  ),
}));

const { issueRequestApi } = await import('../../api/endpoints');

const mockIssues: IssueRequestDto[] = [
  {
    id: 'iss-1',
    type: 0,
    status: 0,
    title: 'App crashes on login',
    area: 'Login / Authentication',
    bodyJson: '{}',
    submittedByUserId: 'u1',
    submittedByName: 'Test User',
    submittedAt: '2026-02-20T10:00:00Z',
  },
  {
    id: 'iss-2',
    type: 1,
    status: 1,
    title: 'Add dark mode',
    area: 'Menu / Navigation',
    bodyJson: '{}',
    submittedByUserId: 'u1',
    submittedByName: 'Test User',
    submittedAt: '2026-02-21T14:00:00Z',
  },
];

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <ReportIssueScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('ReportIssueScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(issueRequestApi.getMine).mockReturnValue(new Promise(() => {}));
    renderScreen();

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders list view with submitted issues', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue(mockIssues);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('App crashes on login')).toBeInTheDocument();
      expect(screen.getByText('Add dark mode')).toBeInTheDocument();
    });
  });

  it('renders empty state when no issues', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('No issue requests submitted yet.')).toBeInTheDocument();
    });
  });

  it('clicking New Issue button opens form view', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Issue')).toBeInTheDocument();
    });

    await user.click(screen.getByText('New Issue'));

    await waitFor(() => {
      expect(screen.getByText('New Bug Report')).toBeInTheDocument();
    });
  });

  it('renders Bug Report form fields', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Issue')).toBeInTheDocument();
    });
    await user.click(screen.getByText('New Issue'));

    await waitFor(() => {
      expect(screen.getByText('Describe the Bug')).toBeInTheDocument();
      expect(screen.getByText('Steps to Reproduce')).toBeInTheDocument();
      expect(screen.getByText('Expected Behavior')).toBeInTheDocument();
      expect(screen.getByText('Actual Behavior')).toBeInTheDocument();
      expect(screen.getByText('Browser')).toBeInTheDocument();
      expect(screen.getByText('Severity')).toBeInTheDocument();
    });
  });

  it('renders Feature Request form fields when type button clicked', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Issue')).toBeInTheDocument();
    });
    await user.click(screen.getByText('New Issue'));

    await waitFor(() => {
      expect(screen.getByText('Feature Request')).toBeInTheDocument();
    });
    await user.click(screen.getByText('Feature Request'));

    await waitFor(() => {
      expect(screen.getByText("What Problem Does This Solve?")).toBeInTheDocument();
      expect(screen.getByText("Describe the Feature You'd Like")).toBeInTheDocument();
      expect(screen.getByText('How Important Is This to You?')).toBeInTheDocument();
    });
  });

  it('renders General Question form fields when type button clicked', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Issue')).toBeInTheDocument();
    });
    await user.click(screen.getByText('New Issue'));

    await waitFor(() => {
      expect(screen.getByText('General Question')).toBeInTheDocument();
    });
    await user.click(screen.getByText('General Question'));

    await waitFor(() => {
      expect(screen.getByText('Your Question')).toBeInTheDocument();
      expect(screen.getByText('Additional Context')).toBeInTheDocument();
    });
  });

  it('shows validation error when title is empty on submit', async () => {
    vi.mocked(issueRequestApi.getMine).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Issue')).toBeInTheDocument();
    });
    await user.click(screen.getByText('New Issue'));

    await waitFor(() => {
      expect(screen.getByText('Submit')).toBeInTheDocument();
    });
    await user.click(screen.getByText('Submit'));

    await waitFor(() => {
      expect(screen.getByText('Title is required.')).toBeInTheDocument();
    });
  });
});
