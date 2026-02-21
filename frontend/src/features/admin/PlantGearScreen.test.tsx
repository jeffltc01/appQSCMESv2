import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { PlantGearScreen } from './PlantGearScreen.tsx';
import { adminPlantGearApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 };
const tier3User = { plantCode: '000', plantName: 'Cleveland', displayName: 'QM User', roleTier: 3 };

vi.mock('../../api/endpoints.ts', () => ({
  adminPlantGearApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <PlantGearScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockPlantGear = [
  {
    plantId: 'p1',
    plantName: 'Plant 1',
    plantCode: 'PLT1',
    currentPlantGearId: 'g1',
    currentGearLevel: 1,
    gears: [
      { id: 'g1', name: 'Gear 1', level: 1, plantId: 'p1' },
    ],
  },
];

describe('PlantGearScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminPlantGearApi.getAll).mockResolvedValue(mockPlantGear);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockPlantGear) => void;
    vi.mocked(adminPlantGearApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockPlantGear);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders plant cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/Plant 1 \(PLT1\)/)).toBeInTheDocument();
    });
    expect(screen.getByText('Gear 1')).toBeInTheDocument();
  });

  it('does not show Add button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/Plant 1/)).toBeInTheDocument();
    });
    expect(screen.queryByRole('button', { name: /Add/i })).not.toBeInTheDocument();
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Plant Gear')).toBeInTheDocument();
  });

  it('renders without error when no plants', async () => {
    vi.mocked(adminPlantGearApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
    expect(screen.getByText('Plant Gear')).toBeInTheDocument();
  });

  describe('Tier 3 read-only behavior', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
    });

    it('hides gear buttons for Tier 3', async () => {
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText(/Plant 1 \(PLT1\)/)).toBeInTheDocument();
      });
      expect(screen.queryByRole('button', { name: '1' })).not.toBeInTheDocument();
    });
  });
});
