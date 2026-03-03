import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ProductionLineWorkCentersScreen } from './ProductionLineWorkCentersScreen.tsx';
import { adminWorkCenterApi, productionLineApi, downtimeConfigApi, downtimeReasonCategoryApi } from '../../api/endpoints.ts';
import { openDialogByTrigger } from '../../test/dialogTestUtils.ts';

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
  {
    id: 'wcpl2',
    workCenterId: 'g1',
    productionLineId: 'pl2',
    productionLineName: 'Line 2',
    plantName: 'Springfield',
    displayName: 'Rolls Station B',
    numberOfWelders: 2,
    downtimeTrackingEnabled: true,
    downtimeThresholdMinutes: 5,
  },
];

const mockProductionLines = [
  { id: 'pl1', name: 'Line 1', plantId: 'p1', plantName: 'Cleveland' },
  { id: 'pl2', name: 'Line 2', plantId: 'p2', plantName: 'Springfield' },
  { id: 'pl3', name: 'Line 3', plantId: 'p3', plantName: 'Toledo' },
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
      expect(screen.getByText('Line 1 / Rolls 1')).toBeInTheDocument();
      expect(screen.getByText('Line 2 / Rolls 1')).toBeInTheDocument();
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
      expect(screen.getByText('Rolls Station B')).toBeInTheDocument();
      expect(screen.getByText('Cleveland')).toBeInTheDocument();
      expect(screen.getByText('Springfield')).toBeInTheDocument();
      expect(screen.getAllByText('Downtime Tracking').length).toBeGreaterThan(0);
      expect(screen.getByText('Enabled')).toBeInTheDocument();
      expect(screen.getByText('Disabled')).toBeInTheDocument();
    });
  });

  it('does not render work center Type on cards', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.queryByText(/^Type$/)).not.toBeInTheDocument();
    });
  });

  it('filters cards by selected plant', async () => {
    renderScreen();
    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
      expect(screen.getByText('Rolls Station B')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox', { name: 'Plant' }));
    await user.click(await screen.findByRole('option', { name: 'Springfield' }));

    await waitFor(() => {
      expect(screen.queryByText('Rolls Station A')).not.toBeInTheDocument();
      expect(screen.getByText('Rolls Station B')).toBeInTheDocument();
    });
  });

  it('shows empty state when plant filter has no matching cards', async () => {
    renderScreen();
    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox', { name: 'Plant' }));
    await user.click(await screen.findByRole('option', { name: 'Toledo' }));

    await waitFor(() => {
      expect(screen.getByText('No production line work centers match the selected plant.')).toBeInTheDocument();
    });
  });

  it('keeps edit and delete actions functional', async () => {
    vi.mocked(adminWorkCenterApi.deleteProductionLineConfig).mockResolvedValue(undefined as never);
    renderScreen();
    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('Rolls Station A')).toBeInTheDocument();
      expect(screen.getByText('Rolls Station B')).toBeInTheDocument();
    });

    const editButtons = screen.getAllByRole('button', { name: 'Edit production line config' });
    await user.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('Edit Production Line Config')).toBeInTheDocument();
    });

    const cancelOrCloseButton =
      screen.queryByRole('button', { name: 'Cancel' })
      ?? screen.queryByRole('button', { name: 'Close' });
    if (cancelOrCloseButton) {
      await user.click(cancelOrCloseButton);
    } else {
      await user.keyboard('{Escape}');
    }

    const deleteButtons = screen.getAllByRole('button', { name: 'Delete production line config' });
    await user.click(deleteButtons[0]);

    await waitFor(() => {
      expect(adminWorkCenterApi.deleteProductionLineConfig).toHaveBeenCalledWith('g1', 'pl1');
      expect(screen.queryByText('Rolls Station A')).not.toBeInTheDocument();
      expect(screen.getByText('Rolls Station B')).toBeInTheDocument();
    });
  });

  it('shows message when no per-line overrides', async () => {
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No per-line overrides configured.')).toBeInTheDocument();
    });
  });

  it('allows creating first per-line override from top-level add action', async () => {
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([]);
    vi.mocked(adminWorkCenterApi.createProductionLineConfig).mockResolvedValue({
      id: 'wcpl-new',
      workCenterId: 'g1',
      productionLineId: 'pl1',
      productionLineName: 'Line 1',
      plantName: 'Cleveland',
      displayName: 'Rolls 1',
      numberOfWelders: 0,
      downtimeTrackingEnabled: false,
      downtimeThresholdMinutes: 5,
    });

    renderScreen();
    const user = userEvent.setup();

    await waitFor(() => {
      expect(screen.getByText('No per-line overrides configured.')).toBeInTheDocument();
    });

    const dialog = await openDialogByTrigger(
      user,
      screen.getByRole('button', { name: 'Add Config' }),
      'Add Production Line Config',
    );
    const [workCenterCombobox, productionLineCombobox] = within(dialog).getAllByRole('combobox', { hidden: true });

    await user.click(workCenterCombobox);
    await user.click(await screen.findByRole('option', { name: 'Rolls 1' }));

    await user.click(productionLineCombobox);
    await user.click(await screen.findByRole('option', { name: 'Line 1 (Cleveland)' }));

    await user.click(within(dialog).getByRole('button', { name: 'Save', hidden: true }));

    await waitFor(() => {
      expect(adminWorkCenterApi.createProductionLineConfig).toHaveBeenCalledWith('g1', {
        productionLineId: 'pl1',
        displayName: 'Rolls 1',
        numberOfWelders: 0,
        enableWorkCenterChecklist: false,
        enableSafetyChecklist: false,
      });
      expect(screen.getByText('Line 1 / Rolls 1')).toBeInTheDocument();
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

    const editButtons = screen.getAllByRole('button', { name: 'Edit production line config' });
    await user.click(editButtons[0]);

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

    const editButtons = screen.getAllByRole('button', { name: 'Edit production line config' });
    await user.click(editButtons[0]);

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
