import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ProductionLogsScreen } from './ProductionLogsScreen.tsx';
import { logViewerApi, siteApi, adminAnnotationTypeApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  logViewerApi: {
    getRollsLog: vi.fn(),
    getFitupLog: vi.fn(),
    getHydroLog: vi.fn(),
    getRtXrayLog: vi.fn(),
    getSpotXrayLog: vi.fn(),
    createAnnotation: vi.fn(),
  },
  siteApi: { getSites: vi.fn() },
  adminAnnotationTypeApi: { getAll: vi.fn() },
}));

const mockUser = {
  id: 'u1',
  plantCode: 'PLT1',
  plantName: 'West Jordan',
  displayName: 'Test User',
  roleTier: 5,
  defaultSiteId: 's1',
};

const mockSites = [
  { id: 's1', code: 'WJ', name: 'West Jordan', timeZoneId: 'America/Denver' },
  { id: 's2', code: 'CLE', name: 'Cleveland', timeZoneId: 'America/New_York' },
];

const mockAnnotationTypes = [
  { id: 'at1', name: 'Note', abbreviation: 'N', requiresResolution: false, operatorCanCreate: true, displayColor: '#cc00ff' },
  { id: 'at2', name: 'Correction Needed', abbreviation: 'C', requiresResolution: true, operatorCanCreate: true, displayColor: '#ffff00' },
];

const mockRollsEntries = [
  {
    id: 'r1',
    timestamp: '2026-02-19T14:30:00Z',
    coilHeatLot: 'Coil:25B353895 Heat:B2551070',
    thickness: 'Pass',
    shellCode: '020401',
    tankSize: 500,
    welders: ['Jeff Phillips'],
    annotations: [{ abbreviation: 'AI', color: '#33cc33' }],
  },
];

const mockSpotXrayResponse = {
  shotCounts: [
    { date: '02/03/2026', count: 14 },
    { date: '02/04/2026', count: 8 },
  ],
  entries: [
    {
      id: 'sx1',
      timestamp: '2026-02-04T11:47:00Z',
      tanks: '20401 (OZ), 020402 (OY)',
      inspected: 'OX',
      tankSize: 500,
      operator: 'Jeff Phillips',
      result: 'Accept',
      shots: 'Seam 1: 1 (02/04/2026), Seam 2: 2 (02/04/2026)',
      annotations: [],
    },
  ],
};

function renderScreen(initialEntries = ['/menu/production-logs']) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter initialEntries={initialEntries}>
        <ProductionLogsScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('ProductionLogsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ user: mockUser, logout: vi.fn() });
    vi.mocked(siteApi.getSites).mockResolvedValue(mockSites);
    vi.mocked(adminAnnotationTypeApi.getAll).mockResolvedValue(mockAnnotationTypes);
  });

  it('renders title and filter bar', () => {
    renderScreen();
    expect(screen.getByText('Log Viewer')).toBeInTheDocument();
    expect(screen.getByText('Log Type')).toBeInTheDocument();
    expect(screen.getByText('Site')).toBeInTheDocument();
    expect(screen.getByText('Start Date')).toBeInTheDocument();
    expect(screen.getByText('End Date')).toBeInTheDocument();
    expect(screen.getByText('Go')).toBeInTheDocument();
  });

  it('shows empty state when no log type selected', () => {
    renderScreen();
    expect(screen.getByText('Select a log type to begin.')).toBeInTheDocument();
  });

  it('pre-selects log type from URL param', () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue(mockRollsEntries);
    renderScreen(['/menu/production-logs?logType=rolls']);
    const select = screen.getAllByRole('combobox')[0];
    expect(select).toHaveValue('rolls');
  });

  it('loads rolls log data and renders correct columns', async () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue(mockRollsEntries);
    renderScreen(['/menu/production-logs?logType=rolls']);

    await waitFor(() => {
      expect(screen.getByText('020401')).toBeInTheDocument();
    });
    expect(screen.getByText('Coil / Heat / Lot')).toBeInTheDocument();
    expect(screen.getByText('Thickness')).toBeInTheDocument();
    expect(screen.getByText('Shell Code')).toBeInTheDocument();
    expect(screen.getByText('Jeff Phillips')).toBeInTheDocument();
    expect(screen.getByText('Pass')).toBeInTheDocument();
    expect(screen.getByText('AI')).toBeInTheDocument();
  });

  it('Go button is disabled when no log type selected', () => {
    renderScreen();
    const goBtn = screen.getByText('Go');
    expect(goBtn).toBeDisabled();
  });

  it('loads data when Go button is clicked', async () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue(mockRollsEntries);
    const user = userEvent.setup();
    renderScreen();

    const logTypeSelect = screen.getAllByRole('combobox')[0];
    await user.selectOptions(logTypeSelect, 'rolls');

    const goBtn = screen.getByText('Go');
    await user.click(goBtn);

    await waitFor(() => {
      expect(logViewerApi.getRollsLog).toHaveBeenCalled();
    });
  });

  it('renders spot xray shot counts bar', async () => {
    vi.mocked(logViewerApi.getSpotXrayLog).mockResolvedValue(mockSpotXrayResponse);
    renderScreen(['/menu/production-logs?logType=spot-xray']);

    await waitFor(() => {
      expect(screen.getByText('Shot Counts:')).toBeInTheDocument();
    });
    expect(screen.getByText('02/03/2026 - 14')).toBeInTheDocument();
    expect(screen.getByText('02/04/2026 - 8')).toBeInTheDocument();
  });

  it('shows record count after search', async () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue(mockRollsEntries);
    renderScreen(['/menu/production-logs?logType=rolls']);

    await waitFor(() => {
      expect(screen.getByText('Showing 1 records')).toBeInTheDocument();
    });
  });

  it('shows no records message when search returns empty', async () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue([]);
    renderScreen(['/menu/production-logs?logType=rolls']);

    await waitFor(() => {
      expect(screen.getByText('No records found for the selected criteria.')).toBeInTheDocument();
    });
  });

  it('opens annotation modal when + button clicked', async () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue(mockRollsEntries);
    const user = userEvent.setup();
    renderScreen(['/menu/production-logs?logType=rolls']);

    await waitFor(() => {
      expect(screen.getByText('020401')).toBeInTheDocument();
    });

    const addBtns = screen.getAllByTitle('Add annotation');
    await user.click(addBtns[0]);

    expect(screen.getByText('Add Annotation')).toBeInTheDocument();
  });

  it('saves annotation and refreshes', async () => {
    vi.mocked(logViewerApi.getRollsLog).mockResolvedValue(mockRollsEntries);
    vi.mocked(logViewerApi.createAnnotation).mockResolvedValue({
      id: 'new-a',
      serialNumber: '020401',
      annotationTypeName: 'Note',
      annotationTypeId: 'at1',
      flag: true,
      notes: 'Test note',
      initiatedByName: 'Test User',
      createdAt: '2026-02-21T00:00:00Z',
    });
    const user = userEvent.setup();
    renderScreen(['/menu/production-logs?logType=rolls']);

    await waitFor(() => {
      expect(screen.getByText('020401')).toBeInTheDocument();
    });

    const addBtns = screen.getAllByTitle('Add annotation');
    await user.click(addBtns[0]);

    const saveBtn = screen.getByText('Save');
    await user.click(saveBtn);

    await waitFor(() => {
      expect(logViewerApi.createAnnotation).toHaveBeenCalledWith(
        expect.objectContaining({
          productionRecordId: 'r1',
          annotationTypeId: 'at1',
          initiatedByUserId: 'u1',
        }),
      );
    });
  });
});
