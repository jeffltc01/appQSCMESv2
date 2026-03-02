import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { reportTelemetry } from '../../telemetry/telemetryClient.ts';

vi.mock('../../api/endpoints', () => ({
  workCenterApi: {
    getHistory: vi.fn().mockResolvedValue({ dayCount: 0, recentRecords: [] }),
    getWorkCenters: vi.fn().mockResolvedValue([]),
    getQueueTransactions: vi.fn().mockResolvedValue([]),
    lookupWelder: vi.fn(),
  },
  supervisorDashboardApi: {
    getMetrics: vi.fn().mockResolvedValue({
      dayCount: 15,
      hourlyCounts: [
        { hour: 10, count: 2 },
        { hour: 11, count: 3 },
        { hour: 12, count: 4 },
        { hour: 13, count: 5 },
        { hour: 14, count: 6 },
      ],
    }),
    getPerformanceTable: vi.fn().mockResolvedValue({
      rows: [
        { label: '10:00', planned: 4, actual: 2, delta: -2, fpy: null, downtimeMinutes: 0 },
        { label: '11:00', planned: 4, actual: 3, delta: -1, fpy: null, downtimeMinutes: 0 },
        { label: '12:00', planned: 4, actual: 4, delta: 0, fpy: null, downtimeMinutes: 0 },
        { label: '13:00', planned: 4, actual: 5, delta: 1, fpy: null, downtimeMinutes: 0 },
        { label: '14:00', planned: 4, actual: 6, delta: 2, fpy: null, downtimeMinutes: 0 },
      ],
      totalRow: { label: 'Total', planned: 20, actual: 20, delta: 0, fpy: null, downtimeMinutes: 0 },
    }),
  },
  adminWorkCenterApi: {
    getProductionLineConfigs: vi.fn().mockResolvedValue([]),
    getProductionLineConfig: vi.fn().mockResolvedValue({ numberOfWelders: 0 }),
  },
  adminPlantGearApi: {
    getAll: vi.fn().mockResolvedValue([]),
  },
  downtimeConfigApi: {
    get: vi.fn().mockResolvedValue({ downtimeTrackingEnabled: false, downtimeThresholdMinutes: 5, applicableReasons: [] }),
  },
  downtimeEventApi: {
    create: vi.fn().mockResolvedValue({}),
  },
}));

vi.mock('../../auth/AuthContext', () => {
  const user = {
    id: 'u1',
    displayName: 'Test User',
    defaultSiteId: 'site-1',
    roleTier: 5,
    employeeNumber: 'EMP001',
    plantCode: 'TST',
    plantTimeZoneId: 'America/Denver',
  };
  const logout = vi.fn();
  return { useAuth: () => ({ user, logout }) };
});

vi.mock('../../hooks/useLocalStorage', () => ({
  getTabletCache: vi.fn(() => ({
    cachedWorkCenterId: 'wc1',
    cachedWorkCenterName: 'Rolls',
    cachedWorkCenterDisplayName: 'Rolls',
    cachedDataEntryType: 'Rolls',
    cachedProductionLineId: 'pl1',
    cachedProductionLineName: 'Line 1',
    cachedAssetId: 'a1',
    cachedAssetName: 'Asset 1',
    cachedNumberOfWelders: 0,
  })),
}));

const mockFocusLost = { value: false };
const inactivityMock = {
  onInactivityDetected: null as ((lastActivityTimestamp: number) => void) | null,
  resetTimer: vi.fn(),
};
vi.mock('../../hooks/useBarcode', () => ({
  useBarcode: () => ({ inputRef: { current: null }, handleKeyDown: vi.fn(), focusLost: mockFocusLost.value }),
}));
vi.mock('../../hooks/useHeartbeat', () => ({ useHeartbeat: vi.fn() }));
vi.mock('../../hooks/useInactivityTracker', () => ({
  useInactivityTracker: (options: { onInactivityDetected: (lastActivityTimestamp: number) => void }) => {
    inactivityMock.onInactivityDetected = options.onInactivityDetected;
    return { resetTimer: inactivityMock.resetTimer };
  },
}));
vi.mock('../../help/useCurrentHelpArticle', () => ({
  useCurrentHelpArticle: () => null,
}));
vi.mock('../../telemetry/telemetryClient.ts', () => ({
  reportException: vi.fn(),
  reportTelemetry: vi.fn(),
}));

vi.mock('./TopBar', () => ({ TopBar: () => <div data-testid="top-bar" /> }));
vi.mock('./BottomBar', () => ({ BottomBar: () => <div data-testid="bottom-bar" /> }));
vi.mock('./LeftPanel', () => ({ LeftPanel: () => <div data-testid="left-panel" /> }));
vi.mock('./WCHistory', () => ({ WCHistory: () => <div data-testid="wc-history" /> }));
vi.mock('./QueueHistory', () => ({ QueueHistory: () => <div data-testid="queue-history" /> }));
vi.mock('./ScanOverlay.tsx', () => ({ ScanOverlay: () => null }));

const mockRollsScreenBehavior = { emitErrorOnMount: false };
vi.mock('../../features/rolls/RollsScreen.tsx', async () => {
  const React = await import('react');
  return {
    RollsScreen: (props: { showScanResult?: (result: { type: 'error' | 'success' | 'warning'; message?: string }) => void }) => {
      const emittedRef = React.useRef(false);
      React.useEffect(() => {
        if (mockRollsScreenBehavior.emitErrorOnMount && !emittedRef.current) {
          emittedRef.current = true;
          props.showScanResult?.({ type: 'error', message: 'Mock scan error' });
        }
      }, []);
      return <div data-testid="rolls-screen" />;
    },
  };
});
vi.mock('../../features/hydro/HydroScreen.tsx', () => ({ HydroScreen: () => <div data-testid="hydro-screen" /> }));
vi.mock('../../features/fitup/FitupScreen.tsx', () => ({ FitupScreen: () => <div data-testid="fitup-screen" /> }));
vi.mock('../../features/spotXray/SpotXrayScreen.tsx', () => ({ SpotXrayScreen: () => <div data-testid="spot-screen" /> }));
vi.mock('../../features/nameplate/NameplateScreen.tsx', () => ({ NameplateScreen: () => <div data-testid="nameplate-screen" /> }));
vi.mock('../../features/rtXrayQueue/RtXrayQueueScreen.tsx', () => ({ RtXrayQueueScreen: () => <div data-testid="rt-xray-screen" /> }));
vi.mock('../../features/rollsMaterial/RollsMaterialScreen.tsx', () => ({ RollsMaterialScreen: () => <div data-testid="rolls-material-screen" /> }));
vi.mock('../../features/fitupQueue/FitupQueueScreen.tsx', () => ({ FitupQueueScreen: () => <div data-testid="fitup-queue-screen" /> }));
vi.mock('../../features/longSeam/LongSeamScreen.tsx', () => ({ LongSeamScreen: () => <div data-testid="long-seam-screen" /> }));
vi.mock('../../features/longSeamInsp/LongSeamInspScreen.tsx', () => ({ LongSeamInspScreen: () => <div data-testid="long-seam-insp-screen" /> }));
vi.mock('../../features/roundSeam/RoundSeamScreen.tsx', () => ({ RoundSeamScreen: () => <div data-testid="round-seam-screen" /> }));
vi.mock('../../features/roundSeamInsp/RoundSeamInspScreen.tsx', () => ({ RoundSeamInspScreen: () => <div data-testid="round-seam-insp-screen" /> }));

import { OperatorLayout } from './OperatorLayout';

function renderOperatorLayout() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <OperatorLayout />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('OperatorLayout', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockRollsScreenBehavior.emitErrorOnMount = false;
    inactivityMock.onInactivityDetected = null;
  });

  it('renders TopBar, BottomBar, and LeftPanel', async () => {
    renderOperatorLayout();
    await waitFor(() => {
      expect(screen.getByTestId('top-bar')).toBeInTheDocument();
      expect(screen.getByTestId('bottom-bar')).toBeInTheDocument();
      expect(screen.getByTestId('left-panel')).toBeInTheDocument();
    });
  });

  it('renders WCHistory for non-queue screen types', async () => {
    renderOperatorLayout();
    await waitFor(() => {
      expect(screen.getByTestId('wc-history')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('queue-history')).not.toBeInTheDocument();
  });

  it('loads history on mount', async () => {
    const { workCenterApi } = await import('../../api/endpoints');
    renderOperatorLayout();
    await waitFor(() => {
      expect(vi.mocked(workCenterApi.getHistory)).toHaveBeenCalled();
    });
  });

  it('loads queue transactions using current wc and production line for queue screens', async () => {
    const { getTabletCache } = await import('../../hooks/useLocalStorage');
    const { workCenterApi } = await import('../../api/endpoints');
    vi.mocked(getTabletCache).mockReturnValueOnce({
      cachedWorkCenterId: 'wc-current',
      cachedWorkCenterName: 'Fitup Queue',
      cachedWorkCenterDisplayName: 'Fitup Queue',
      cachedDataEntryType: 'MatQueue-Fitup',
      cachedProductionLineId: 'pl-current',
      cachedProductionLineName: 'Line 1',
      cachedAssetId: 'a1',
      cachedAssetName: 'Asset 1',
      cachedNumberOfWelders: 0,
      cachedMaterialQueueForWCId: 'wc-shared-old',
    });

    renderOperatorLayout();

    await waitFor(() => {
      expect(vi.mocked(workCenterApi.getQueueTransactions)).toHaveBeenCalledWith(
        'wc-current',
        'pl-current',
        'site-1',
        5,
        'added',
      );
    });
  });

  it('renders the correct work center screen based on dataEntryType', async () => {
    renderOperatorLayout();
    await waitFor(() => {
      expect(screen.getByTestId('rolls-screen')).toBeInTheDocument();
    });
  });

  it('does not show focus-lost banner when externalInput is off', async () => {
    renderOperatorLayout();
    await waitFor(() => {
      expect(screen.getByTestId('top-bar')).toBeInTheDocument();
    });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('does not show focus-lost banner when focusLost is false', async () => {
    mockFocusLost.value = false;
    renderOperatorLayout();
    await waitFor(() => {
      expect(screen.getByTestId('top-bar')).toBeInTheDocument();
    });
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('renders the floating capacity summary card', async () => {
    renderOperatorLayout();
    await waitFor(() => {
      expect(screen.getByLabelText('Operator capacity indicator')).toBeInTheDocument();
      expect(screen.getByText('Today')).toBeInTheDocument();
      expect(screen.getByTestId('today-total-count-chip')).toHaveTextContent('0');
      expect(screen.getByText('Plan')).toBeInTheDocument();
      expect(screen.getByText('Actual')).toBeInTheDocument();
    });
  });

  it('renders dynamic tank-size chips from history data', async () => {
    const { workCenterApi } = await import('../../api/endpoints');
    vi.mocked(workCenterApi.getHistory).mockResolvedValueOnce({
      dayCount: 3,
      tankSizeCounts: [
        { tankSize: 250, count: 2 },
        { tankSize: 500, count: 1 },
      ],
      recentRecords: [],
    });

    renderOperatorLayout();

    await waitFor(() => {
      expect(screen.getByText(/250:\s*2/)).toBeInTheDocument();
      expect(screen.getByText(/500:\s*1/)).toBeInTheDocument();
      expect(screen.getByTestId('today-total-count-chip')).toHaveTextContent('3');
      expect(screen.queryByText(/Total:\s*/)).not.toBeInTheDocument();
    });
  });

  it('hides Plan row when no capacity target is configured', async () => {
    const { supervisorDashboardApi } = await import('../../api/endpoints');
    vi.mocked(supervisorDashboardApi.getPerformanceTable).mockResolvedValueOnce({
      rows: [
        { label: '10:00', planned: null, actual: 2, delta: 2, fpy: null, downtimeMinutes: 0 },
        { label: '11:00', planned: null, actual: 3, delta: 3, fpy: null, downtimeMinutes: 0 },
        { label: '12:00', planned: null, actual: 4, delta: 4, fpy: null, downtimeMinutes: 0 },
        { label: '13:00', planned: null, actual: 5, delta: 5, fpy: null, downtimeMinutes: 0 },
        { label: '14:00', planned: null, actual: 6, delta: 6, fpy: null, downtimeMinutes: 0 },
      ],
      totalRow: { label: 'Total', planned: null, actual: 20, delta: 20, fpy: null, downtimeMinutes: 0 },
    });

    renderOperatorLayout();

    await waitFor(() => {
      expect(screen.getByLabelText('Operator capacity indicator')).toBeInTheDocument();
      expect(screen.getByText('Actual')).toBeInTheDocument();
    });
    expect(screen.queryByText('Plan')).not.toBeInTheDocument();
  });

  it('logs telemetry when an error scan overlay is shown', async () => {
    mockRollsScreenBehavior.emitErrorOnMount = true;
    renderOperatorLayout();

    await waitFor(() => {
      expect(vi.mocked(reportTelemetry)).toHaveBeenCalledWith(
        expect.objectContaining({
          category: 'scan_feedback',
          source: 'operator_scan_overlay',
          severity: 'error',
          message: 'Mock scan error',
        }),
      );
    });
  });

  it('re-fetches downtime config when inactivity is detected', async () => {
    const { downtimeConfigApi } = await import('../../api/endpoints');
    vi.mocked(downtimeConfigApi.get)
      .mockResolvedValueOnce({ downtimeTrackingEnabled: true, downtimeThresholdMinutes: 5, applicableReasons: [] })
      .mockResolvedValueOnce({ downtimeTrackingEnabled: true, downtimeThresholdMinutes: 10, applicableReasons: [] });

    renderOperatorLayout();

    await waitFor(() => {
      expect(vi.mocked(downtimeConfigApi.get)).toHaveBeenCalledTimes(1);
    });

    await act(async () => {
      inactivityMock.onInactivityDetected?.(Date.now() - 60_000);
    });

    await waitFor(() => {
      expect(vi.mocked(downtimeConfigApi.get)).toHaveBeenCalledTimes(2);
    });
  });

  it('shows downtime overlay with assigned reasons when inactivity is detected', async () => {
    const { downtimeConfigApi, adminWorkCenterApi } = await import('../../api/endpoints');
    vi.mocked(downtimeConfigApi.get).mockResolvedValue({
      downtimeTrackingEnabled: true,
      downtimeThresholdMinutes: 5,
      applicableReasons: [
        {
          id: 'reason-1',
          downtimeReasonCategoryId: 'category-1',
          categoryName: 'Equipment',
          name: 'Breakdown',
          isActive: true,
          countsAsDowntime: true,
          sortOrder: 0,
        },
      ],
    });
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([
      { id: 'wcpl-1', productionLineId: 'pl1' } as never,
    ]);

    renderOperatorLayout();

    await waitFor(() => {
      expect(vi.mocked(downtimeConfigApi.get)).toHaveBeenCalledTimes(1);
    });

    await act(async () => {
      inactivityMock.onInactivityDetected?.(Date.now() - 60_000);
    });

    await waitFor(() => {
      expect(screen.getByText('Looks like there was some downtime')).toBeInTheDocument();
      expect(screen.getByText('Breakdown')).toBeInTheDocument();
    });
  });

  it('shows a visible error state when downtime config fetch fails during inactivity', async () => {
    const { downtimeConfigApi, adminWorkCenterApi } = await import('../../api/endpoints');
    vi.mocked(downtimeConfigApi.get)
      .mockResolvedValueOnce({
        downtimeTrackingEnabled: true,
        downtimeThresholdMinutes: 5,
        applicableReasons: [],
      })
      .mockRejectedValueOnce(new Error('Network failed'));
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([
      { id: 'wcpl-1', productionLineId: 'pl1' } as never,
    ]);

    renderOperatorLayout();

    await waitFor(() => {
      expect(vi.mocked(downtimeConfigApi.get)).toHaveBeenCalledTimes(1);
    });

    await act(async () => {
      inactivityMock.onInactivityDetected?.(Date.now() - 60_000);
    });

    await waitFor(() => {
      expect(screen.getByTestId('downtime-config-error')).toBeInTheDocument();
      expect(screen.getByText(/Unable to load downtime reasons for this station/)).toBeInTheDocument();
    });
  });
});
