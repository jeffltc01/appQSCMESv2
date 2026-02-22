import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { SupervisorDashboardScreen } from './SupervisorDashboardScreen';
import type { SupervisorDashboardMetrics, SupervisorRecord } from '../../types/domain';

vi.mock('recharts', async (importOriginal) => {
  const actual = await importOriginal<typeof import('recharts')>();
  return {
    ...actual,
    ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
      <div style={{ width: 500, height: 300 }}>{children}</div>
    ),
  };
});

vi.mock('../../api/endpoints', () => ({
  workCenterApi: {
    getWorkCenters: vi.fn().mockResolvedValue([
      { id: 'wc-1', name: 'Rolls', workCenterTypeId: 'wct-1', workCenterTypeName: 'Production', numberOfWelders: 0, dataEntryType: 'Rolls' },
      { id: 'wc-2', name: 'Hydro', workCenterTypeId: 'wct-2', workCenterTypeName: 'Production', numberOfWelders: 0, dataEntryType: 'Hydro' },
    ]),
  },
  supervisorDashboardApi: {
    getMetrics: vi.fn().mockResolvedValue(null),
    getRecords: vi.fn().mockResolvedValue([]),
    submitAnnotation: vi.fn().mockResolvedValue({ annotationsCreated: 0 }),
  },
  siteApi: { getSites: vi.fn().mockResolvedValue([]) },
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'sup-user-1', displayName: 'Supervisor', roleTier: 4,
      plantCode: 'PLT1', plantName: 'Plant 1', defaultSiteId: 'site-1',
    },
    isAuthenticated: true,
    logout: vi.fn(),
  }),
}));

const { supervisorDashboardApi } = await import('../../api/endpoints');

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <SupervisorDashboardScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

const emptyHourlyCounts = Array.from({ length: 24 }, (_, i) => ({ hour: i, count: 0 }));
const emptyDailyCounts = Array.from({ length: 7 }, (_, i) => ({ date: `2026-02-${16 + i}`, count: 0 }));

const mockMetrics: SupervisorDashboardMetrics = {
  dayCount: 42,
  weekCount: 187,
  supportsFirstPassYield: true,
  dayFPY: 96.5,
  weekFPY: 94.2,
  dayDefects: 3,
  weekDefects: 12,
  dayAvgTimeBetweenScans: 135,
  weekAvgTimeBetweenScans: 142,
  dayQtyPerHour: 8.2,
  weekQtyPerHour: 7.6,
  hourlyCounts: emptyHourlyCounts.map((h) =>
    h.hour >= 6 && h.hour <= 16 ? { ...h, count: Math.floor(Math.random() * 5) + 1 } : h,
  ),
  weekDailyCounts: emptyDailyCounts.map((d, i) => ({ ...d, count: (i + 1) * 5 })),
  operators: [
    { id: 'op-1', displayName: 'John D.', recordCount: 25 },
    { id: 'op-2', displayName: 'Jane S.', recordCount: 17 },
  ],
  oeeAvailability: null,
  oeePerformance: null,
  oeeQuality: null,
  oeeOverall: null,
  oeePlannedMinutes: null,
  oeeDowntimeMinutes: null,
  oeeRunTimeMinutes: null,
};

const mockRecords: SupervisorRecord[] = [
  {
    id: 'rec-1', timestamp: '2026-02-22T14:00:00Z',
    serialOrIdentifier: 'SN-001', tankSize: '120',
    operatorName: 'John D.', annotations: [],
  },
  {
    id: 'rec-2', timestamp: '2026-02-22T13:30:00Z',
    serialOrIdentifier: 'SN-002', tankSize: '500',
    operatorName: 'Jane S.',
    annotations: [{ annotationTypeId: 'a1000001-0000-0000-0000-000000000001', typeName: 'Note', abbreviation: 'N', displayColor: '#cc00ff' }],
  },
];

describe('SupervisorDashboardScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows prompt to select a work center initially', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/select a work center to view/i)).toBeInTheDocument();
    });
  });

  it('renders work center dropdown with options', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });
  });

  it('loads metrics and displays KPI cards when work center is selected', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('42')).toBeInTheDocument();
      expect(screen.getByText('187')).toBeInTheDocument();
    });
  });

  it('shows FPY cards with correct values', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('96.5%')).toBeInTheDocument();
      expect(screen.getByText('94.2%')).toBeInTheDocument();
    });
  });

  it('displays operator chips', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('John D. (25)')).toBeInTheDocument();
      expect(screen.getByText('Jane S. (17)')).toBeInTheDocument();
      expect(screen.getByText('All')).toBeInTheDocument();
    });
  });

  it('clicking operator chip triggers metrics reload with operator filter', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('John D. (25)')).toBeInTheDocument();
    });

    vi.mocked(supervisorDashboardApi.getMetrics).mockClear();
    await user.click(screen.getByText('John D. (25)'));

    await waitFor(() => {
      expect(supervisorDashboardApi.getMetrics).toHaveBeenCalledWith(
        'wc-1', 'site-1', expect.any(String), 'op-1',
      );
    });
  });

  it('shows records table with annotation badges', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
      expect(screen.getByText('SN-002')).toBeInTheDocument();
      expect(screen.getByText('N')).toBeInTheDocument();
    });
  });

  it('submits annotation with selected type', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    vi.mocked(supervisorDashboardApi.submitAnnotation).mockResolvedValue({ annotationsCreated: 1 });
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getAllByRole('combobox')[0]);
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const checkboxes = screen.getAllByRole('checkbox');
    await user.click(checkboxes[0]);

    const submitBtn = screen.getByRole('button', { name: /annotate 1 record/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(supervisorDashboardApi.submitAnnotation).toHaveBeenCalledWith({
        recordIds: ['rec-1'],
        annotationTypeId: 'a1000001-0000-0000-0000-000000000001',
        comment: undefined,
      });
    });
  });

  it('shows success message after annotation', async () => {
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(mockMetrics);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue(mockRecords);
    vi.mocked(supervisorDashboardApi.submitAnnotation).mockResolvedValue({ annotationsCreated: 1 });
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getAllByRole('combobox')[0]);
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const checkboxes = screen.getAllByRole('checkbox');
    await user.click(checkboxes[0]);

    const submitBtn = screen.getByRole('button', { name: /annotate 1 record/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/1 annotation\(s\) created/i)).toBeInTheDocument();
    });
  });

  it('hides FPY and defect cards when not applicable', async () => {
    const metricsNoFpy: SupervisorDashboardMetrics = {
      ...mockMetrics,
      supportsFirstPassYield: false,
      dayFPY: null,
      weekFPY: null,
    };
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(metricsNoFpy);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue([]);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Hydro'));

    await waitFor(() => {
      expect(screen.getByText(metricsNoFpy.dayCount.toString())).toBeInTheDocument();
    });

    expect(screen.queryByText('First Pass Yield')).not.toBeInTheDocument();
    expect(screen.queryByText('Total Defects')).not.toBeInTheDocument();
  });

  it('shows FPY card with dashes when day data is null but WC supports FPY', async () => {
    const metricsNullDay: SupervisorDashboardMetrics = {
      ...mockMetrics,
      supportsFirstPassYield: true,
      dayFPY: null,
      weekFPY: 97.3,
    };
    vi.mocked(supervisorDashboardApi.getMetrics).mockResolvedValue(metricsNullDay);
    vi.mocked(supervisorDashboardApi.getRecords).mockResolvedValue([]);
    const user = userEvent.setup();

    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox'));
    await user.click(screen.getByText('Rolls'));

    await waitFor(() => {
      expect(screen.getByText('First Pass Yield')).toBeInTheDocument();
    });

    expect(screen.getByText('--')).toBeInTheDocument();
    expect(screen.getByText('97.3%')).toBeInTheDocument();
  });
});
