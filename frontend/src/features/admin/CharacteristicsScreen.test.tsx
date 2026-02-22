import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { CharacteristicsScreen } from './CharacteristicsScreen.tsx';
import { adminCharacteristicApi, adminProductApi, adminWorkCenterApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 };
const tier3User = { plantCode: '000', plantName: 'Cleveland', displayName: 'QM User', roleTier: 3 };

vi.mock('../../api/endpoints.ts', () => ({
  adminCharacteristicApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
  },
  adminProductApi: {
    getTypes: vi.fn(),
  },
  adminWorkCenterApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <CharacteristicsScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockCharacteristics = [
  {
    id: 'c1',
    code: '001',
    name: 'Long Seam',
    specHigh: 10,
    specLow: 1,
    specTarget: 5,
    minTankSize: null,
    productTypeName: 'Shell',
    workCenterIds: ['wc1'],
    isActive: true,
  },
  {
    id: 'c2',
    code: '002',
    name: 'RS1',
    specHigh: null,
    specLow: null,
    specTarget: null,
    minTankSize: 0,
    productTypeName: null,
    workCenterIds: [],
    isActive: false,
  },
];

const mockTypes = [{ id: 'pt1', name: 'Shell' }];

const mockWorkCenters = [
  {
    id: 'wc1',
    name: 'Rolls 1',
    workCenterTypeName: 'Rolls',
    productionLineName: 'Line 1',
    numberOfWelders: 2,
    dataEntryType: 'standard',
  },
];

describe('CharacteristicsScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminCharacteristicApi.getAll).mockResolvedValue(mockCharacteristics);
    vi.mocked(adminProductApi.getTypes).mockResolvedValue(mockTypes);
    vi.mocked(adminWorkCenterApi.getAll).mockResolvedValue(mockWorkCenters);
  });

  it('renders loading state initially', async () => {
    let resolveChars!: (v: typeof mockCharacteristics) => void;
    let resolveTypes!: (v: typeof mockTypes) => void;
    let resolveWcs!: (v: typeof mockWorkCenters) => void;
    vi.mocked(adminCharacteristicApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveChars = r; }),
    );
    vi.mocked(adminProductApi.getTypes).mockImplementation(
      () => new Promise((r) => { resolveTypes = r; }),
    );
    vi.mocked(adminWorkCenterApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveWcs = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveChars(mockCharacteristics);
    resolveTypes(mockTypes);
    resolveWcs(mockWorkCenters);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.getByText(/1.*10/)).toBeInTheDocument();
  });

  it('renders Code and MinTankSize on cards', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.getByText('001')).toBeInTheDocument();
    expect(screen.getByText('002')).toBeInTheDocument();
    expect(screen.getByText('0')).toBeInTheDocument();
  });

  it('shows inactive badge for deactivated items', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('RS1')).toBeInTheDocument();
    });
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminCharacteristicApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No characteristics found.')).toBeInTheDocument();
    });
  });

  it('shows Add button for Admin', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /Add/i })).toBeInTheDocument();
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Characteristics')).toBeInTheDocument();
  });

  describe('Tier 3 read-only behavior', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
    });

    it('hides edit buttons for Tier 3', async () => {
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText('Long Seam')).toBeInTheDocument();
      });
      expect(screen.queryByLabelText(/edit/i)).not.toBeInTheDocument();
    });

    it('hides Add button for non-Admin', async () => {
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText('Long Seam')).toBeInTheDocument();
      });
      expect(screen.queryByRole('button', { name: /Add/i })).not.toBeInTheDocument();
    });
  });
});
