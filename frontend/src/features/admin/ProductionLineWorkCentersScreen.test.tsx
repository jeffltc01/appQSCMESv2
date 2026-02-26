import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ProductionLineWorkCentersScreen } from './ProductionLineWorkCentersScreen.tsx';
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
        <ProductionLineWorkCentersScreen />
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
    siteConfigs: [],
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
      { id: 'r1', downtimeReasonCategoryId: 'cat1', categoryName: 'Equipment', name: 'Breakdown', isActive: true, countsAsDowntime: true, sortOrder: 0 },
      { id: 'r2', downtimeReasonCategoryId: 'cat1', categoryName: 'Equipment', name: 'Maintenance', isActive: true, countsAsDowntime: true, sortOrder: 1 },
    ],
  },
];

const mockDowntimeConfig = {
  downtimeTrackingEnabled: true,
  downtimeThresholdMinutes: 5,
  applicableReasons: [
    { id: 'r1', downtimeReasonCategoryId: 'cat1', categoryName: 'Equipment', name: 'Breakdown', isActive: true, countsAsDowntime: true, sortOrder: 0 },
  ],
};

describe('ProductionLineWorkCentersScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue(mockGroups);
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue(mockPlConfigs);
    vi.mocked(productionLineApi.getAll).mockResolvedValue(mockProductionLines);
    vi.mocked(downtimeReasonCategoryApi.getAll).mockResolvedValue(mockReasonCategories);
    vi.mocked(downtimeConfigApi.get).mockResolvedValue(mockDowntimeConfig);
    vi.mocked(downtimeConfigApi.setReasons).mockResolvedValue(undefined as never);
  });

  it('displays correct title', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Production Line Work Centers')).toBeInTheDocument();
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

    const lineLabel = await screen.findByText('Line 1 (Cleveland)');
    const plRow = lineLabel.closest('div');
    expect(plRow).not.toBeNull();
    const plEditButton = within(plRow as HTMLElement).getAllByRole('button')[0];
    await user.click(plEditButton);

    await waitFor(() => {
      expect(screen.getByText('Edit Production Line Config')).toBeInTheDocument();
      expect(screen.getByText('Applicable Reason Codes')).toBeInTheDocument();
    });
  });

  it('blocks save when downtime tracking is enabled and no reasons are selected', async () => {
    vi.mocked(downtimeConfigApi.get).mockResolvedValueOnce({
      downtimeTrackingEnabled: false,
      downtimeThresholdMinutes: 5,
      applicableReasons: [],
    });

    renderScreen();
    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
    });

    const lineLabel = await screen.findByText('Line 1 (Cleveland)');
    const plRow = lineLabel.closest('div');
    expect(plRow).not.toBeNull();
    const plEditButton = within(plRow as HTMLElement).getAllByRole('button')[0];
    await user.click(plEditButton);

    await waitFor(() => {
      expect(screen.getByText('Edit Production Line Config')).toBeInTheDocument();
    });

    const downtimeToggle = screen.getByLabelText('Enable Downtime Tracking');
    await user.click(downtimeToggle);
    await user.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(screen.getByText('Select at least one downtime reason when downtime tracking is enabled.')).toBeInTheDocument();
    });
    expect(vi.mocked(adminWorkCenterApi.updateProductionLineConfig)).not.toHaveBeenCalled();
    expect(vi.mocked(downtimeConfigApi.setReasons)).not.toHaveBeenCalled();
  });
});
