import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WhosOnFloorScreen } from './WhosOnFloorScreen.tsx';
import { activeSessionApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { defaultSiteId: '11111111-1111-1111-1111-111111111111', plantName: 'Cleveland', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  activeSessionApi: {
    getBySite: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <WhosOnFloorScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockActiveSessions = [
  {
    id: '1',
    userId: 'u1',
    userDisplayName: 'John Doe',
    employeeNumber: 'EMP001',
    plantId: '11111111-1111-1111-1111-111111111111',
    productionLineId: 'pl1',
    productionLineName: 'Line 1',
    workCenterId: 'wc1',
    workCenterName: 'Rolls 1',
    loginDateTime: '2025-01-01T00:00:00Z',
    lastHeartbeatDateTime: '2025-01-01T00:00:00Z',
    isStale: false,
  },
];

describe("WhosOnFloorScreen", () => {
  beforeEach(() => {
    vi.mocked(activeSessionApi.getBySite).mockResolvedValue(mockActiveSessions);
  });

  it('renders loading state initially', async () => {
    let resolveGetBySite!: (v: typeof mockActiveSessions) => void;
    vi.mocked(activeSessionApi.getBySite).mockImplementation(
      () => new Promise((r) => { resolveGetBySite = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetBySite(mockActiveSessions);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders session cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    });
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('EMP001')).toBeInTheDocument();
  });

  it('shows empty state when no sessions', async () => {
    vi.mocked(activeSessionApi.getBySite).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No active sessions at this site.')).toBeInTheDocument();
    });
  });

  it('calls getBySite with user defaultSiteId', async () => {
    renderScreen();
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
    expect(activeSessionApi.getBySite).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111');
  });

  it("displays correct title", async () => {
    renderScreen();
    expect(screen.getByText("Who's On the Floor")).toBeInTheDocument();
  });
});
