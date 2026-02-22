import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { DigitalTwinScreen } from './DigitalTwinScreen';
import type { DigitalTwinSnapshot } from '../../types/domain';

vi.mock('../../api/endpoints', () => ({
  siteApi: {
    getSites: vi.fn().mockResolvedValue([
      { id: 'site-1', code: '000', name: 'Cleveland', timeZoneId: 'America/Chicago' },
    ]),
  },
  productionLineApi: {
    getProductionLines: vi.fn().mockResolvedValue([
      { id: 'pl-1', name: 'Production Line 1', plantId: 'site-1' },
    ]),
  },
  digitalTwinApi: {
    getSnapshot: vi.fn().mockResolvedValue(null),
  },
}));

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'sup-user-1', displayName: 'Supervisor', roleTier: 4,
      plantCode: '000', plantName: 'Cleveland', defaultSiteId: 'site-1',
    },
    isAuthenticated: true,
    logout: vi.fn(),
  }),
}));

const { digitalTwinApi } = await import('../../api/endpoints');

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <DigitalTwinScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

const mockSnapshot: DigitalTwinSnapshot = {
  stations: [
    { workCenterId: 'wc-1', name: 'Rolls', sequence: 1, wipCount: 3, status: 'Active', isBottleneck: false, isGateCheck: false, currentOperator: 'B. Jones', unitsToday: 48, avgCycleTimeMinutes: 15.2, firstPassYieldPercent: 99 },
    { workCenterId: 'wc-2', name: 'Long Seam', sequence: 2, wipCount: 2, status: 'Active', isBottleneck: false, isGateCheck: false, currentOperator: 'K. Lee', unitsToday: 46, avgCycleTimeMinutes: 17.8, firstPassYieldPercent: 98 },
    { workCenterId: 'wc-3', name: 'LS Inspect', sequence: 3, wipCount: 1, status: 'Active', isBottleneck: false, isGateCheck: false, currentOperator: 'M. Chen', unitsToday: 45, avgCycleTimeMinutes: 12.5, firstPassYieldPercent: 95 },
    { workCenterId: 'wc-4', name: 'RT X-ray', sequence: 4, wipCount: 4, status: 'Active', isBottleneck: true, isGateCheck: true, currentOperator: 'D. Patel', unitsToday: 40, avgCycleTimeMinutes: 32.1, firstPassYieldPercent: 92 },
    { workCenterId: 'wc-5', name: 'Fitup', sequence: 5, wipCount: 2, status: 'Active', isBottleneck: false, isGateCheck: false, currentOperator: 'S. Kim', unitsToday: 42, avgCycleTimeMinutes: 21.3, firstPassYieldPercent: 97 },
    { workCenterId: 'wc-6', name: 'Round Seam', sequence: 6, wipCount: 1, status: 'Active', isBottleneck: false, isGateCheck: false, currentOperator: 'A. Garcia', unitsToday: 41, avgCycleTimeMinutes: 19.6, firstPassYieldPercent: 96 },
    { workCenterId: 'wc-7', name: 'RS Inspect', sequence: 7, wipCount: 0, status: 'Slow', isBottleneck: false, isGateCheck: false, currentOperator: 'T. Nguyen', unitsToday: 41, avgCycleTimeMinutes: 14.1, firstPassYieldPercent: 94 },
    { workCenterId: 'wc-8', name: 'Spot X-ray', sequence: 8, wipCount: 0, status: 'Idle', isBottleneck: false, isGateCheck: true, currentOperator: undefined, unitsToday: 38, avgCycleTimeMinutes: undefined, firstPassYieldPercent: undefined },
    { workCenterId: 'wc-9', name: 'Nameplate', sequence: 9, wipCount: 1, status: 'Active', isBottleneck: false, isGateCheck: false, currentOperator: 'J. Brown', unitsToday: 39, avgCycleTimeMinutes: 8.4, firstPassYieldPercent: 99 },
    { workCenterId: 'wc-10', name: 'Hydro', sequence: 10, wipCount: 2, status: 'Active', isBottleneck: false, isGateCheck: true, currentOperator: 'R. Wilson', unitsToday: 37, avgCycleTimeMinutes: 25.7, firstPassYieldPercent: 96 },
  ],
  materialFeeds: [
    { workCenterName: 'Rolls Material', queueLabel: '12 plates', itemCount: 12, feedsIntoStation: 'Rolls' },
    { workCenterName: 'Heads Queue', queueLabel: '8 lots', itemCount: 8, feedsIntoStation: 'Fitup' },
  ],
  throughput: { unitsToday: 47, unitsDelta: 3, unitsPerHour: 5.9 },
  avgCycleTimeMinutes: 18.4,
  lineEfficiencyPercent: 87,
  unitTracker: [
    { serialNumber: '012744', productName: '120 Propane', currentStationName: 'Rolls', currentStationSequence: 1, enteredCurrentStationAt: '2026-02-22T10:45:00Z', isAssembly: false },
    { serialNumber: 'AB', productName: '500 Propane', currentStationName: 'Fitup', currentStationSequence: 5, enteredCurrentStationAt: '2026-02-22T11:05:00Z', isAssembly: true },
  ],
};

describe('DigitalTwinScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders title and toolbar', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Plant')).toBeInTheDocument();
    });
  });

  it('fetches and displays snapshot data', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getAllByText('Rolls').length).toBeGreaterThanOrEqual(1);
    });

    expect(screen.getAllByText('Long Seam').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Hydro').length).toBeGreaterThanOrEqual(1);
  });

  it('displays all 10 station nodes in the pipeline', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getAllByText('Rolls').length).toBeGreaterThanOrEqual(1);
    });

    const stationNames = ['Rolls', 'Long Seam', 'LS Inspect', 'RT X-ray', 'Fitup',
      'Round Seam', 'RS Inspect', 'Spot X-ray', 'Nameplate', 'Hydro'];
    for (const name of stationNames) {
      expect(screen.getAllByText(name).length).toBeGreaterThanOrEqual(1);
    }
  });

  it('shows bottleneck label for flagged station', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText(/Bottleneck/)).toBeInTheDocument();
    });
  });

  it('displays KPI cards with correct values', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Line Throughput')).toBeInTheDocument();
    });

    expect(screen.getAllByText('Avg Cycle Time').length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('Line Efficiency')).toBeInTheDocument();
    expect(screen.getByText('47')).toBeInTheDocument();
    expect(screen.getByText('18.4')).toBeInTheDocument();
    expect(screen.getByText('87')).toBeInTheDocument();
  });

  it('displays station detail table with all stations', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Station Detail')).toBeInTheDocument();
    });

    expect(screen.getByText('B. Jones')).toBeInTheDocument();
    expect(screen.getByText('D. Patel')).toBeInTheDocument();
  });

  it('displays material feed labels', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls Material')).toBeInTheDocument();
    });

    expect(screen.getByText('12 plates')).toBeInTheDocument();
    expect(screen.getByText('Heads Queue')).toBeInTheDocument();
    expect(screen.getByText('8 lots')).toBeInTheDocument();
  });

  it('shows unit tracker with serial numbers', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Live Unit Tracker')).toBeInTheDocument();
    });

    expect(screen.getByText('012744')).toBeInTheDocument();
    expect(screen.getByText('AB')).toBeInTheDocument();
  });

  it('shows throughput delta correctly', async () => {
    vi.mocked(digitalTwinApi.getSnapshot).mockResolvedValueOnce(mockSnapshot);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText(/\+3 vs yesterday/)).toBeInTheDocument();
    });
  });
});
