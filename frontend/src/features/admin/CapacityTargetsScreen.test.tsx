import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { CapacityTargetsScreen } from './CapacityTargetsScreen.tsx';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  siteApi: { getSites: vi.fn() },
  adminPlantGearApi: { getAll: vi.fn() },
  adminProductionLineApi: { getAll: vi.fn() },
  capacityTargetApi: {
    getAll: vi.fn(),
    getTankSizes: vi.fn(),
    bulkUpsert: vi.fn(),
  },
  workCenterApi: { getWorkCenters: vi.fn() },
  adminWorkCenterApi: { getProductionLineConfigs: vi.fn() },
}));

import {
  siteApi,
  adminPlantGearApi,
  adminProductionLineApi,
  capacityTargetApi,
  workCenterApi,
  adminWorkCenterApi,
} from '../../api/endpoints.ts';

const testUser = {
  plantCode: '000',
  plantName: 'Cleveland',
  displayName: 'Test User',
  roleTier: 4,
  defaultSiteId: 'plant-1',
};

const mockPlants = [
  { id: 'plant-1', code: '000', name: 'Cleveland', timeZoneId: 'EST' },
  { id: 'plant-2', code: '600', name: 'Fremont', timeZoneId: 'EST' },
];

const mockGearData = [
  {
    plantId: 'plant-1',
    plantName: 'Cleveland',
    plantCode: '000',
    gears: [
      { id: 'gear-1', name: 'Gear 1', level: 1, plantId: 'plant-1' },
      { id: 'gear-2', name: 'Gear 2', level: 2, plantId: 'plant-1' },
    ],
  },
];

const mockLines = [
  { id: 'line-1', name: 'Line 1', plantId: 'plant-1', plantName: 'Cleveland' },
];

const mockWorkCenters = [
  { id: 'wc-1', name: 'Rolls', workCenterTypeId: 'wct-1', workCenterTypeName: 'Rolls', numberOfWelders: 1 },
];

const mockWcpls = [
  {
    id: 'wcpl-1',
    workCenterId: 'wc-1',
    productionLineId: 'line-1',
    productionLineName: 'Line 1',
    plantName: 'Cleveland',
    displayName: 'Rolls',
    numberOfWelders: 1,
    downtimeTrackingEnabled: false,
    downtimeThresholdMinutes: 5,
  },
];

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <CapacityTargetsScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('CapacityTargetsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ user: testUser, logout: vi.fn() });

    vi.mocked(siteApi.getSites).mockResolvedValue(mockPlants);
    vi.mocked(adminPlantGearApi.getAll).mockResolvedValue(mockGearData);
    vi.mocked(adminProductionLineApi.getAll).mockResolvedValue(mockLines);
    vi.mocked(capacityTargetApi.getAll).mockResolvedValue([]);
    vi.mocked(capacityTargetApi.getTankSizes).mockResolvedValue([120, 250, 320]);
    vi.mocked(capacityTargetApi.bulkUpsert).mockResolvedValue([]);
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue(mockWorkCenters);
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue(mockWcpls);
  });

  it('renders the title', async () => {
    renderScreen();
    expect(screen.getByText('Capacity Targets')).toBeInTheDocument();
  });

  it('renders gear column headers after loading', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Gear 1')).toBeInTheDocument();
      expect(screen.getByText('Gear 2')).toBeInTheDocument();
    });
  });

  it('renders work center rows', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
    });
  });

  it('pre-fills existing target values', async () => {
    vi.mocked(capacityTargetApi.getAll).mockResolvedValue([
      {
        id: 'ct-1',
        workCenterProductionLineId: 'wcpl-1',
        workCenterName: 'Rolls',
        productionLineName: 'Line 1',
        tankSize: null,
        plantGearId: 'gear-1',
        gearLevel: 1,
        targetUnitsPerHour: 12,
      },
    ]);

    renderScreen();

    await waitFor(() => {
      const inputs = screen.getAllByRole('spinbutton');
      const gear1Input = inputs.find(i => (i as HTMLInputElement).value === '12');
      expect(gear1Input).toBeDefined();
    });
  });

  it('shows empty state when no work centers configured', async () => {
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockResolvedValue([]);

    renderScreen();

    await waitFor(() => {
      expect(screen.getByText(/No work centers are configured/i)).toBeInTheDocument();
    });
  });

  it('save button is disabled when no changes', async () => {
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
    });

    const saveBtn = screen.getByRole('button', { name: /Save Changes/i });
    expect(saveBtn).toBeDisabled();
  });

  it('save button enables after editing a cell', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
    });

    const inputs = screen.getAllByRole('spinbutton');
    await user.type(inputs[0], '15');

    await waitFor(() => {
      const saveBtn = screen.getByRole('button', { name: /Save Changes/i });
      expect(saveBtn).toBeEnabled();
    });
  });

  it('calls bulkUpsert API when saving', async () => {
    const user = userEvent.setup();
    vi.mocked(capacityTargetApi.bulkUpsert).mockResolvedValue([]);

    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
    });

    const inputs = screen.getAllByRole('spinbutton');
    await user.type(inputs[0], '15');

    const saveBtn = screen.getByRole('button', { name: /Save Changes/i });
    await user.click(saveBtn);

    await waitFor(() => {
      expect(capacityTargetApi.bulkUpsert).toHaveBeenCalledWith(
        expect.objectContaining({
          productionLineId: 'line-1',
          targets: expect.arrayContaining([
            expect.objectContaining({ targetUnitsPerHour: 15 }),
          ]),
        }),
      );
    });
  });

  it('shows tank size inputs after toggling expand icon', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
    });

    const toggleIcons = screen.getAllByTitle('Switch to per-tank-size targets');
    await user.click(toggleIcons[0]);

    await waitFor(() => {
      expect(screen.getByText('120 gal:')).toBeInTheDocument();
      expect(screen.getByText('250 gal:')).toBeInTheDocument();
      expect(screen.getByText('320 gal:')).toBeInTheDocument();
    });
  });

  it('copies Rolls value to other work centers in the same gear column', async () => {
    const multiWcs = [
      { id: 'wc-1', name: 'Rolls', workCenterTypeId: 'wct-1', workCenterTypeName: 'Rolls', numberOfWelders: 1 },
      { id: 'wc-2', name: 'Long Seam', workCenterTypeId: 'wct-2', workCenterTypeName: 'Long Seam', numberOfWelders: 1 },
      { id: 'wc-3', name: 'Fitup', workCenterTypeId: 'wct-3', workCenterTypeName: 'Fitup', numberOfWelders: 1 },
    ];
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue(multiWcs);
    vi.mocked(adminWorkCenterApi.getProductionLineConfigs).mockImplementation(async (wcId: string) => {
      const map: Record<string, typeof mockWcpls> = {
        'wc-1': [{ ...mockWcpls[0], id: 'wcpl-1', workCenterId: 'wc-1' }],
        'wc-2': [{ id: 'wcpl-2', workCenterId: 'wc-2', productionLineId: 'line-1', productionLineName: 'Line 1', plantName: 'Cleveland', displayName: 'Long Seam', numberOfWelders: 1, downtimeTrackingEnabled: false, downtimeThresholdMinutes: 5 }],
        'wc-3': [{ id: 'wcpl-3', workCenterId: 'wc-3', productionLineId: 'line-1', productionLineName: 'Line 1', plantName: 'Cleveland', displayName: 'Fitup', numberOfWelders: 1, downtimeTrackingEnabled: false, downtimeThresholdMinutes: 5 }],
      };
      return map[wcId] ?? [];
    });

    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
      expect(screen.getByText('Fitup')).toBeInTheDocument();
    });

    const rows = screen.getAllByRole('row');
    const rollsRow = rows.find(r => within(r).queryByText('Rolls'));
    const rollsInputs = within(rollsRow!).getAllByRole('spinbutton');
    await user.type(rollsInputs[0], '10');

    await waitFor(() => {
      const allInputs = screen.getAllByRole('spinbutton');
      const gear1Inputs = allInputs.filter((_, idx) => idx % 2 === 0);
      for (const input of gear1Inputs) {
        expect((input as HTMLInputElement).value).toBe('10');
      }
    });
  });

  it('shows collapse icon after expanding and can toggle back', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Rolls')).toBeInTheDocument();
    });

    const toggleIcons = screen.getAllByTitle('Switch to per-tank-size targets');
    await user.click(toggleIcons[0]);

    await waitFor(() => {
      expect(screen.getByTitle('Switch to default (single value)')).toBeInTheDocument();
    });

    await user.click(screen.getByTitle('Switch to default (single value)'));

    await waitFor(() => {
      expect(screen.queryByText('120 gal:')).not.toBeInTheDocument();
    });
  });
});
