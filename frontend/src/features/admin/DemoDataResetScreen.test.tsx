import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DemoDataResetScreen } from './DemoDataResetScreen.tsx';
import { demoDataAdminApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../api/endpoints.ts')>();
  return {
    ...actual,
    demoDataAdminApi: {
      resetSeed: vi.fn(),
      refreshDates: vi.fn(),
    },
  };
});

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <DemoDataResetScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('DemoDataResetScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({
      user: { displayName: 'Admin User', roleTier: 1, plantCode: '000', plantName: 'Cleveland' },
      logout: vi.fn(),
    });

    vi.mocked(demoDataAdminApi.resetSeed).mockResolvedValue({
      executedAtUtc: '2026-02-24T16:30:00Z',
      deleted: [{ table: 'ProductionRecords', count: 10 }],
      inserted: [{ table: 'ProductionRecords', count: 12 }],
    });
    vi.mocked(demoDataAdminApi.refreshDates).mockResolvedValue({
      executedAtUtc: '2026-02-24T16:45:00Z',
      appliedDeltaHours: 12.5,
      updated: [{ table: 'ProductionRecords', count: 12 }],
    });
  });

  it('keeps reset button disabled until confirmation phrase is entered', async () => {
    renderScreen();
    const button = screen.getByRole('button', { name: 'Run Reset + Seed' });
    expect(button).toBeDisabled();

    await userEvent.type(screen.getByPlaceholderText('RESET DEMO DATA'), 'RESET DEMO DATA');
    expect(button).toBeEnabled();
  });

  it('runs reset + seed and renders result counts', async () => {
    renderScreen();
    await userEvent.type(screen.getByPlaceholderText('RESET DEMO DATA'), 'RESET DEMO DATA');
    await userEvent.click(screen.getByRole('button', { name: 'Run Reset + Seed' }));

    await waitFor(() => {
      expect(demoDataAdminApi.resetSeed).toHaveBeenCalled();
      expect(screen.getByText('Reset + Seed Completed')).toBeInTheDocument();
      expect(screen.getByText('ProductionRecords: 12')).toBeInTheDocument();
    });
  });

  it('runs date refresh and renders applied delta', async () => {
    renderScreen();
    await userEvent.click(screen.getByRole('button', { name: 'Run Date Refresh' }));

    await waitFor(() => {
      expect(demoDataAdminApi.refreshDates).toHaveBeenCalled();
      expect(screen.getByText('Date Refresh Completed')).toBeInTheDocument();
      expect(screen.getByText(/Applied Delta/)).toBeInTheDocument();
    });
  });
});
