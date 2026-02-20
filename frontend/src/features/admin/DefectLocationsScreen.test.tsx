import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DefectLocationsScreen } from './DefectLocationsScreen.tsx';
import { adminDefectLocationApi, adminCharacteristicApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminDefectLocationApi: {
    getAll: vi.fn(),
  },
  adminCharacteristicApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <DefectLocationsScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockDefectLocations = [
  {
    id: '1',
    code: '01',
    name: 'T-Joint',
    characteristicId: 'c1',
    characteristicName: 'Long Seam',
    isActive: true,
  },
];

const mockCharacteristics = [
  {
    id: 'c1',
    name: 'Long Seam',
    specHigh: 10,
    specLow: 1,
    specTarget: 5,
    productTypeName: 'Shell',
    workCenterIds: ['wc1'],
  },
];

describe('DefectLocationsScreen', () => {
  beforeEach(() => {
    vi.mocked(adminDefectLocationApi.getAll).mockResolvedValue(mockDefectLocations);
    vi.mocked(adminCharacteristicApi.getAll).mockResolvedValue(mockCharacteristics);
  });

  it('renders loading state initially', async () => {
    let resolveLocs!: (v: typeof mockDefectLocations) => void;
    let resolveChars!: (v: typeof mockCharacteristics) => void;
    vi.mocked(adminDefectLocationApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveLocs = r; }),
    );
    vi.mocked(adminCharacteristicApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveChars = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveLocs(mockDefectLocations);
    resolveChars(mockCharacteristics);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/01/)).toBeInTheDocument();
      expect(screen.getByText(/T-Joint/)).toBeInTheDocument();
    });
    expect(screen.getByText('Long Seam')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminDefectLocationApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No defect locations found.')).toBeInTheDocument();
    });
  });

  it('shows Add Location button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Location/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Defect Locations')).toBeInTheDocument();
  });
});
