import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SellableTankStatusScreen } from './SellableTankStatusScreen.tsx';
import { sellableTankStatusApi, siteApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  sellableTankStatusApi: {
    getStatus: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

const supervisorUser = {
  plantCode: 'PLT1',
  plantName: 'Cleveland',
  displayName: 'Supervisor User',
  roleTier: 4,
  defaultSiteId: 's1',
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <SellableTankStatusScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('SellableTankStatusScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ user: supervisorUser, logout: vi.fn() });
    vi.mocked(siteApi.getSites).mockResolvedValue([]);
    vi.mocked(sellableTankStatusApi.getStatus).mockResolvedValue([]);
  });

  it('renders the title', () => {
    renderScreen();
    expect(screen.getByText('Sellable Tank Daily Status')).toBeInTheDocument();
  });

  it('renders date input and search button', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /Search/i })).toBeInTheDocument();
  });

  it('shows empty state when no tanks found', async () => {
    const user = userEvent.setup();
    renderScreen();

    await user.click(screen.getByRole('button', { name: /Search/i }));

    await waitFor(() => {
      expect(screen.getByText(/No sellable tanks found/i)).toBeInTheDocument();
    });
  });

  it('shows tank data with gate check results', async () => {
    vi.mocked(sellableTankStatusApi.getStatus).mockResolvedValue([
      {
        serialNumber: 'SELL-001',
        productNumber: '120 AG',
        tankSize: 120,
        rtXrayResult: 'Accept',
        spotXrayResult: 'Reject',
        hydroResult: null,
      },
    ]);

    const user = userEvent.setup();
    renderScreen();

    await user.click(screen.getByRole('button', { name: /Search/i }));

    await waitFor(() => {
      expect(screen.getByText('SELL-001')).toBeInTheDocument();
    });
    expect(screen.getByText('120 AG')).toBeInTheDocument();
    expect(screen.getByText('Accept')).toBeInTheDocument();
    expect(screen.getByText('Reject')).toBeInTheDocument();
  });

  it('shows error on API failure', async () => {
    vi.mocked(sellableTankStatusApi.getStatus).mockRejectedValue(new Error('fail'));

    const user = userEvent.setup();
    renderScreen();

    await user.click(screen.getByRole('button', { name: /Search/i }));

    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeInTheDocument();
    });
  });
});
