import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MobilePlantsScreen } from './MobileViews.tsx';

const mockUseAuth = vi.fn();
const getAllMock = vi.fn();
const setGearMock = vi.fn();

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminPlantGearApi: {
    getAll: (...args: unknown[]) => getAllMock(...args),
    setGear: (...args: unknown[]) => setGearMock(...args),
  },
  productionLineApi: {
    getProductionLines: vi.fn().mockResolvedValue([]),
  },
  activeSessionApi: {
    getBySite: vi.fn().mockResolvedValue([]),
  },
  digitalTwinApi: {
    getSnapshot: vi.fn().mockResolvedValue({
      stations: [],
      materialFeeds: [],
      throughput: { unitsToday: 0, unitsDelta: 0, unitsPerHour: 0 },
      avgCycleTimeMinutes: 0,
      lineEfficiencyPercent: 0,
      unitTracker: [],
    }),
  },
  issueRequestApi: {
    getPending: vi.fn().mockResolvedValue([]),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <MobilePlantsScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('MobilePlantsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getAllMock.mockResolvedValue([
      {
        plantId: 'plant-1',
        plantName: 'Cleveland',
        plantCode: '000',
        currentPlantGearId: 'gear-1',
        currentGearLevel: 1,
        nextTankAlphaCode: 'AA',
        gears: [
          { id: 'gear-1', name: 'Gear 1', level: 1, plantId: 'plant-1' },
          { id: 'gear-2', name: 'Gear 2', level: 2, plantId: 'plant-1' },
        ],
      },
    ]);
    setGearMock.mockResolvedValue(undefined);
  });

  it('lets ops director change a plant gear', async () => {
    mockUseAuth.mockReturnValue({
      user: { roleTier: 2, roleName: 'Operations Director', plantName: 'Cleveland', plantCode: '000' },
      logout: vi.fn(),
    });

    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Current gear: Gear 1')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: '2' }));

    expect(setGearMock).toHaveBeenCalledWith('plant-1', { plantGearId: 'gear-2' });
    await waitFor(() => {
      expect(screen.getByText('Current gear: Gear 2')).toBeInTheDocument();
    });
  });

  it('shows read-only gear state for non-director roles', async () => {
    mockUseAuth.mockReturnValue({
      user: { roleTier: 4, roleName: 'Supervisor', plantName: 'Cleveland', plantCode: '000' },
      logout: vi.fn(),
    });

    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Current gear: Gear 1')).toBeInTheDocument();
    });
    expect(screen.getByText('Read-only for this role.')).toBeInTheDocument();
    expect(setGearMock).not.toHaveBeenCalled();
  });
});
