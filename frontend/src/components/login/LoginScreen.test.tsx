import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { LoginScreen } from './LoginScreen';
import { AuthProvider } from '../../auth/AuthContext';

vi.mock('../../api/endpoints', () => ({
  authApi: {
    getLoginConfig: vi.fn(),
    login: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

const { authApi, siteApi } = await import('../../api/endpoints');

function renderLogin() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <AuthProvider>
          <LoginScreen />
        </AuthProvider>
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('LoginScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the login form with title', () => {
    renderLogin();
    expect(screen.getByText('MES Login')).toBeInTheDocument();
    expect(screen.getByText('v2.0.0')).toBeInTheDocument();
  });

  it('renders employee number input with auto-focus', () => {
    renderLogin();
    const empInput = screen.getByLabelText(/employee no/i);
    expect(empInput).toBeInTheDocument();
  });

  it('renders welder toggle defaulting to off', () => {
    renderLogin();
    const toggle = screen.getByRole('switch');
    expect(toggle).toBeInTheDocument();
    expect(toggle).not.toBeChecked();
  });

  it('renders login button initially disabled', () => {
    renderLogin();
    const loginBtn = screen.getByRole('button', { name: /login/i });
    expect(loginBtn).toBeDisabled();
  });

  it('shows error when employee number is not recognized', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.getLoginConfig).mockRejectedValue(new Error('Not found'));

    renderLogin();
    const empInput = screen.getByLabelText(/employee no/i);
    await user.type(empInput, '99999');
    await user.tab();

    await waitFor(() => {
      expect(screen.getByText('Employee number not recognized.')).toBeInTheDocument();
    });
  });

  it('shows PIN field when requiresPin is true', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.getLoginConfig).mockResolvedValue({
      requiresPin: true,
      defaultSiteId: 'site-1',
      allowSiteSelection: false,
      isWelder: false,
      userName: 'Test User',
    });
    vi.mocked(siteApi.getSites).mockResolvedValue([
      { id: 'site-1', code: 'PLT1', name: 'Plant 1', timeZoneId: 'America/Chicago' },
    ]);

    renderLogin();
    const empInput = screen.getByLabelText(/employee no/i);
    await user.type(empInput, '12345');
    await user.tab();

    await waitFor(() => {
      expect(screen.getByLabelText(/pin/i)).toBeInTheDocument();
    });
  });

  it('does not show PIN field when requiresPin is false', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.getLoginConfig).mockResolvedValue({
      requiresPin: false,
      defaultSiteId: 'site-1',
      allowSiteSelection: false,
      isWelder: false,
      userName: 'Test User',
    });
    vi.mocked(siteApi.getSites).mockResolvedValue([
      { id: 'site-1', code: 'PLT1', name: 'Plant 1', timeZoneId: 'America/Chicago' },
    ]);

    renderLogin();
    const empInput = screen.getByLabelText(/employee no/i);
    await user.type(empInput, '12345');
    await user.tab();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /login/i })).toBeEnabled();
    });
    expect(screen.queryByLabelText(/pin/i)).not.toBeInTheDocument();
  });

  it('enables login button after successful config fetch', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.getLoginConfig).mockResolvedValue({
      requiresPin: false,
      defaultSiteId: 'site-1',
      allowSiteSelection: false,
      isWelder: false,
      userName: 'Test User',
    });
    vi.mocked(siteApi.getSites).mockResolvedValue([
      { id: 'site-1', code: 'PLT1', name: 'Plant 1', timeZoneId: 'America/Chicago' },
    ]);

    renderLogin();
    const empInput = screen.getByLabelText(/employee no/i);
    await user.type(empInput, '12345');
    await user.tab();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /login/i })).toBeEnabled();
    });
  });

  it('shows error on failed login attempt', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.getLoginConfig).mockResolvedValue({
      requiresPin: false,
      defaultSiteId: 'site-1',
      allowSiteSelection: false,
      isWelder: false,
      userName: 'Test User',
    });
    vi.mocked(siteApi.getSites).mockResolvedValue([
      { id: 'site-1', code: 'PLT1', name: 'Plant 1', timeZoneId: 'America/Chicago' },
    ]);
    vi.mocked(authApi.login).mockRejectedValue(new Error('Auth failed'));

    renderLogin();
    const empInput = screen.getByLabelText(/employee no/i);
    await user.type(empInput, '12345');
    await user.tab();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /login/i })).toBeEnabled();
    });

    await user.click(screen.getByRole('button', { name: /login/i }));

    await waitFor(() => {
      expect(screen.getByText(/login failed/i)).toBeInTheDocument();
    });
  });

  it('toggles welder switch', async () => {
    const user = userEvent.setup();
    renderLogin();
    const toggle = screen.getByRole('switch');
    await user.click(toggle);
    expect(toggle).toBeChecked();
  });
});
