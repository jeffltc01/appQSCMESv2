import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../api/endpoints', () => ({
  workCenterApi: {
    getHistory: vi.fn().mockResolvedValue({ dayCount: 0, recentRecords: [] }),
    getWorkCenters: vi.fn().mockResolvedValue([]),
    getQueueTransactions: vi.fn().mockResolvedValue([]),
    lookupWelder: vi.fn(),
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
vi.mock('../../hooks/useBarcode', () => ({
  useBarcode: () => ({ inputRef: { current: null }, handleKeyDown: vi.fn(), focusLost: mockFocusLost.value }),
}));
vi.mock('../../hooks/useHeartbeat', () => ({ useHeartbeat: vi.fn() }));
vi.mock('../../hooks/useInactivityTracker', () => ({
  useInactivityTracker: () => ({ resetTimer: vi.fn() }),
}));
vi.mock('../../help/useCurrentHelpArticle', () => ({
  useCurrentHelpArticle: () => null,
}));

vi.mock('./TopBar', () => ({ TopBar: () => <div data-testid="top-bar" /> }));
vi.mock('./BottomBar', () => ({ BottomBar: () => <div data-testid="bottom-bar" /> }));
vi.mock('./LeftPanel', () => ({ LeftPanel: () => <div data-testid="left-panel" /> }));
vi.mock('./WCHistory', () => ({ WCHistory: () => <div data-testid="wc-history" /> }));
vi.mock('./QueueHistory', () => ({ QueueHistory: () => <div data-testid="queue-history" /> }));
vi.mock('./ScanOverlay.tsx', () => ({ ScanOverlay: () => null }));
vi.mock('./DowntimeOverlay', () => ({ DowntimeOverlay: () => null }));

vi.mock('../../features/rolls/RollsScreen.tsx', () => ({ RollsScreen: () => <div data-testid="rolls-screen" /> }));
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
});
