import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { BrowserRouter } from 'react-router-dom';
import { FrontendTelemetryScreen } from './FrontendTelemetryScreen.tsx';
import { frontendTelemetryApi } from '../../api/endpoints.ts';

vi.mock('../../api/endpoints', () => ({
  frontendTelemetryApi: {
    getFilters: vi.fn(),
    getCount: vi.fn(),
    getEvents: vi.fn(),
    archiveOldest: vi.fn(),
  },
}));
const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext', () => ({ useAuth: () => mockUseAuth() }));

describe('FrontendTelemetryScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({
      user: { roleTier: 1, displayName: 'Admin', roleName: 'Administrator', plantCode: '000' },
      logout: vi.fn(),
      login: vi.fn(),
      isAuthenticated: true,
      token: 'token',
      isWelder: false,
    });
    vi.mocked(frontendTelemetryApi.getFilters).mockResolvedValue({
      categories: ['runtime_error'],
      sources: ['window_error'],
      severities: ['error'],
    });
    vi.mocked(frontendTelemetryApi.getCount).mockResolvedValue({
      rowCount: 1000,
      warningThreshold: 250000,
      isWarning: false,
    });
    vi.mocked(frontendTelemetryApi.getEvents).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    vi.mocked(frontendTelemetryApi.archiveOldest).mockResolvedValue({
      deletedRows: 0,
      remainingRows: 1000,
    });
  });

  function renderScreen() {
    return render(
      <BrowserRouter>
        <FluentProvider theme={webLightTheme}>
          <FrontendTelemetryScreen />
        </FluentProvider>
      </BrowserRouter>,
    );
  }

  it('renders title and loads initial telemetry list', async () => {
    renderScreen();

    expect(screen.getByText('Frontend Telemetry')).toBeInTheDocument();
    await waitFor(() => {
      expect(frontendTelemetryApi.getEvents).toHaveBeenCalled();
      expect(frontendTelemetryApi.getFilters).toHaveBeenCalled();
      expect(frontendTelemetryApi.getCount).toHaveBeenCalled();
    });
  });
});
