import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { ShiftScheduleScreen } from './ShiftScheduleScreen';
import type { ShiftSchedule, Plant } from '../../types/domain';

vi.mock('../../api/endpoints');

const baseUser = {
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
};

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext', () => ({ useAuth: () => mockUseAuth() }));

vi.mock('./AdminLayout', () => ({
  AdminLayout: ({ children, title, onAdd, addLabel }: any) => (
    <div>
      <h1>{title}</h1>
      {onAdd && <button onClick={onAdd}>{addLabel}</button>}
      {children}
    </div>
  ),
}));

const { shiftScheduleApi, siteApi } = await import('../../api/endpoints');

const mockPlants: Plant[] = [
  { id: 'site-1', code: 'TST', name: 'Test Plant', timeZoneId: 'America/Denver' },
  { id: 'site-2', code: 'PLT', name: 'Other Plant', timeZoneId: 'America/Chicago' },
];

const mockSchedule: ShiftSchedule = {
  id: 's1',
  plantId: 'site-1',
  effectiveDate: '2026-02-22',
  mondayHours: 8,
  mondayBreakMinutes: 30,
  tuesdayHours: 8,
  tuesdayBreakMinutes: 30,
  wednesdayHours: 8,
  wednesdayBreakMinutes: 30,
  thursdayHours: 8,
  thursdayBreakMinutes: 30,
  fridayHours: 8,
  fridayBreakMinutes: 30,
  saturdayHours: 0,
  saturdayBreakMinutes: 0,
  sundayHours: 0,
  sundayBreakMinutes: 0,
  createdAt: '2026-02-20T10:00:00Z',
  createdByName: 'Admin',
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <ShiftScheduleScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

function authValue(overrides: Partial<typeof baseUser> = {}) {
  return {
    user: { ...baseUser, ...overrides },
    logout: vi.fn(),
    login: vi.fn(),
    isAuthenticated: true,
    token: 'test-token',
    isWelder: false,
  };
}

describe('ShiftScheduleScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue(authValue({ roleTier: 1 }));
    vi.mocked(siteApi.getSites).mockResolvedValue(mockPlants);
  });

  it('renders loading spinner initially', () => {
    vi.mocked(shiftScheduleApi.getAll).mockReturnValue(new Promise(() => {}));
    renderScreen();

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('shows schedules table after load', async () => {
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([mockSchedule]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Admin')).toBeInTheDocument();
    });

    const cells = screen.getAllByText('8h / 30m');
    expect(cells.length).toBe(5);
  });

  it('shows empty state when no schedules', async () => {
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([]);
    renderScreen();

    await waitFor(() => {
      expect(
        screen.getByText(/No shift schedules configured/),
      ).toBeInTheDocument();
    });
  });

  it('New Schedule button opens form', async () => {
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Schedule')).toBeInTheDocument();
    });

    await user.click(screen.getByText('New Schedule'));

    await waitFor(() => {
      expect(screen.getByText('New Shift Schedule')).toBeInTheDocument();
    });
  });

  it('5x8s Preset button fills Mon-Fri with 8h/30min', async () => {
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([]);
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('New Schedule')).toBeInTheDocument();
    });
    await user.click(screen.getByText('New Schedule'));

    await waitFor(() => {
      expect(screen.getByText('5x8s Preset')).toBeInTheDocument();
    });
    await user.click(screen.getByText('5x8s Preset'));

    const inputs = screen.getAllByRole('spinbutton');
    const hoursInputs = inputs.filter((_, i) => i % 2 === 0);
    const breakInputs = inputs.filter((_, i) => i % 2 === 1);

    for (let i = 0; i < 5; i++) {
      expect(hoursInputs[i]).toHaveValue(8);
      expect(breakInputs[i]).toHaveValue(30);
    }
    expect(hoursInputs[5]).toHaveValue(0);
    expect(hoursInputs[6]).toHaveValue(0);
  });

  it('shows plant dropdown for roleTier <= 2', async () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 2 }));
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });
  });

  it('shows read-only plant label for roleTier > 2', async () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 3 }));
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Test Plant')).toBeInTheDocument();
    });
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
  });
});
