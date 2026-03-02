import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WhereUsedScreen } from './WhereUsedScreen.tsx';
import { siteApi, whereUsedApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  whereUsedApi: {
    search: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

const qualityTechUser = {
  plantCode: 'PLT1',
  plantName: 'Cleveland',
  displayName: 'Quality Tech User',
  roleTier: 5,
  defaultSiteId: 's1',
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <WhereUsedScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('WhereUsedScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ user: qualityTechUser, logout: vi.fn() });
    vi.mocked(siteApi.getSites).mockResolvedValue([]);
    vi.mocked(whereUsedApi.search).mockResolvedValue([]);
  });

  it('renders title and search fields', () => {
    renderScreen();
    expect(screen.getByText('Where Used')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter heat number')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter coil number')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter lot number')).toBeInTheDocument();
  });

  it('search button is disabled until at least one field has a value', async () => {
    const user = userEvent.setup();
    renderScreen();

    const searchButton = screen.getByRole('button', { name: /Search/i });
    expect(searchButton).toBeDisabled();

    await user.type(screen.getByPlaceholderText('Enter heat number'), 'H-100');
    expect(searchButton).not.toBeDisabled();
  });

  it('shows rows from API results', async () => {
    vi.mocked(whereUsedApi.search).mockResolvedValue([
      {
        plant: 'Cleveland (PLT1)',
        serialNumber: 'W00000001',
        productionNumber: '120 AG',
        tankSize: 120,
        hydroCompletedAt: '2026-03-01T14:00:00Z',
      },
    ]);

    const user = userEvent.setup();
    renderScreen();

    await user.type(screen.getByPlaceholderText('Enter heat number'), 'H-101');
    await user.click(screen.getByRole('button', { name: /Search/i }));

    await waitFor(() => {
      expect(screen.getByText('W00000001')).toBeInTheDocument();
    });
    expect(screen.getAllByText('Cleveland (PLT1)').length).toBeGreaterThan(0);
    expect(screen.getByText('120 AG')).toBeInTheDocument();
    expect(screen.getByTestId('where-used-active-filters')).toBeInTheDocument();
    expect(screen.getByText('Heat: H-101')).toBeInTheDocument();
  });

  it('shows empty state when API returns no rows', async () => {
    const user = userEvent.setup();
    renderScreen();

    await user.type(screen.getByPlaceholderText('Enter lot number'), 'LOT-500');
    await user.click(screen.getByRole('button', { name: /Search/i }));

    await waitFor(() => {
      expect(screen.getByText(/No finished serial numbers found/i)).toBeInTheDocument();
    });
  });

  it('shows error when API fails', async () => {
    vi.mocked(whereUsedApi.search).mockRejectedValue(new Error('fail'));

    const user = userEvent.setup();
    renderScreen();

    await user.type(screen.getByPlaceholderText('Enter coil number'), 'C-808');
    await user.click(screen.getByRole('button', { name: /Search/i }));

    await waitFor(() => {
      expect(screen.getByText(/Failed to load where used results/i)).toBeInTheDocument();
    });
  });
});
