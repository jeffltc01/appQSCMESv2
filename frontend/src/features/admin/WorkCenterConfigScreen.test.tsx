import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkCenterConfigScreen } from './WorkCenterConfigScreen.tsx';
import { adminWorkCenterApi, productionLineApi, downtimeConfigApi, downtimeReasonCategoryApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminWorkCenterApi: {
    getGrouped: vi.fn(),
    getProductionLineConfigs: vi.fn(),
    createProductionLineConfig: vi.fn(),
    updateProductionLineConfig: vi.fn(),
    deleteProductionLineConfig: vi.fn(),
  },
  productionLineApi: {
    getAll: vi.fn(),
  },
  downtimeConfigApi: {
    get: vi.fn(),
    update: vi.fn(),
    setReasons: vi.fn(),
  },
  downtimeReasonCategoryApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <WorkCenterConfigScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockGroups = [
  {
    groupId: 'g1',
    baseName: 'Rolls 1',
    workCenterTypeName: 'Rolls',
    dataEntryType: 'Rolls',
    siteConfigs: [
      {
        workCenterId: 'wc1',
        siteName: 'Rolls 1',
        numberOfWelders: 2,
        materialQueueForWCId: undefined,
        materialQueueForWCName: undefined,
      },
    ],
  },
];

const mockPlConfigs = [
  {
    id: 'wcpl1',
    workCenterId: 'g1',
    productionLineId: 'pl1',
    productionLineName: 'Line 1',
    plantName: 'Cleveland',
    displayName: 'Rolls Station A',
    numberOfWelders: 3,
    downtimeTrackingEnabled: false,
    downtimeThresholdMinutes: 5,
  },
];

const mockProductionLines = [
  { id: 'pl1', name: 'Line 1', plantId: 'p1', plantName: 'Cleveland' },
  { id: 'pl2', name: 'Line 2', plantId: 'p1', plantName: 'Cleveland' },
];

const mockReasonCategories = [
  {
    id: 'cat1',
    plantId: 'p1',
    name: 'Equipment',
    isActive: true,
    sortOrder: 0,
    reasons: [
      { id: 'r1', downtimeReasonCategoryId: 'cat1', categoryName: 'Equipment', name: 'Breakdown', isActive: true, sortOrder: 0 },
      { id: 'r2', downtimeReasonCategoryId: 'cat1', categoryName: 'Equipment', name: 'Maintenance', isActive: true, sortOrder: 1 },
    ],
  },
];

const mockDowntimeConfig = {
  downtimeTrackingEnabled: true,
  downtimeThresholdMinutes: 5,
  applicableReasons: [
    { id: 'r1', downtimeReasonCategoryId: 'cat1', categoryName: 'Equipment', name: 'Breakdown', isActive: true, sortOrder: 0 },
  ],
};

describe('WorkCenterConfigScreen', () => {
  beforeEach(() => {
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue(mockGroups);
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue(mockPlConfigs);
    vi.mocked(productionLineApi.getAll).mockResolvedValue(mockProductionLines);
    vi.mocked(downtimeReasonCategoryApi.getAll).mockResolvedValue(mockReasonCategories);
    vi.mocked(downtimeConfigApi.get).mockResolvedValue(mockDowntimeConfig);
    vi.mocked(downtimeConfigApi.setReasons).mockResolvedValue(undefined as never);
  });

  it('renders loading state initially', async () => {
    let resolveGetGrouped!: (v: typeof mockGroups) => void;
    vi.mocked(adminWorkCenterApi.getGrouped).mockImplementation(
      () => new Promise((r) => { resolveGetGrouped = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetGrouped(mockGroups);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders work center group cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getAllByText('Rolls 1').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Work Center Config')).toBeInTheDocument();
  });

  it('renders without error when no groups', async () => {
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue([]);
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([]);
    renderScreen();
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
    expect(screen.getByText('Work Center Config')).toBeInTheDocument();
  });

  it('shows per-production-line section on card', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Per-Production Line')).toBeInTheDocument();
    });
  });

  it('shows per-line config data from API', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
      expect(screen.getByText('Welders: 3')).toBeInTheDocument();
      expect(screen.getByText('Line 1 (Cleveland)')).toBeInTheDocument();
    });
  });

  it('shows action buttons for admin user', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getAllByText('Rolls 1').length).toBeGreaterThanOrEqual(1);
    });
    const allButtons = screen.getAllByRole('button');
    expect(allButtons.length).toBeGreaterThanOrEqual(3);
  });

  it('shows message when no per-line overrides', async () => {
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No per-line overrides configured.')).toBeInTheDocument();
    });
  });

  it('loads and displays downtime reason codes when editing PL config with tracking enabled', async () => {
    const plConfigsWithDowntime = [{
      ...mockPlConfigs[0],
      downtimeTrackingEnabled: true,
      downtimeThresholdMinutes: 5,
    }];
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue(plConfigsWithDowntime);

    renderScreen();
    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
    });

    // Buttons: Menu, Help, Logout, WC Edit, PL Add, PL Edit, PL Delete
    const allButtons = screen.getAllByRole('button');
    const plEditButton = allButtons[5];
    await user.click(plEditButton);

    await waitFor(() => {
      expect(screen.getByText('Edit Production Line Config')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('Applicable Reason Codes')).toBeInTheDocument();
      expect(screen.getByText('Equipment')).toBeInTheDocument();
      expect(screen.getByText('Breakdown')).toBeInTheDocument();
      expect(screen.getByText('Maintenance')).toBeInTheDocument();
    });
  });
});
