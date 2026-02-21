import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DefectCodesScreen } from './DefectCodesScreen.tsx';
import { adminDefectCodeApi, adminWorkCenterApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 };
const tier3User = { plantCode: '000', plantName: 'Cleveland', displayName: 'QM User', roleTier: 3 };

vi.mock('../../api/endpoints.ts', () => ({
  adminDefectCodeApi: {
    getAll: vi.fn(),
  },
  adminWorkCenterApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <DefectCodesScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockDefectCodes = [
  {
    id: '1',
    code: '101',
    name: 'Burn Through',
    severity: 'Major',
    workCenterIds: ['wc1'],
    isActive: true,
  },
];

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

describe('DefectCodesScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminDefectCodeApi.getAll).mockResolvedValue(mockDefectCodes);
    vi.mocked(adminWorkCenterApi.getAll).mockResolvedValue(mockWorkCenters);
  });

  it('renders loading state initially', async () => {
    let resolveCodes!: (v: typeof mockDefectCodes) => void;
    let resolveWcs!: (v: typeof mockWorkCenters) => void;
    vi.mocked(adminDefectCodeApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveCodes = r; }),
    );
    vi.mocked(adminWorkCenterApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveWcs = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveCodes(mockDefectCodes);
    resolveWcs(mockWorkCenters);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/101/)).toBeInTheDocument();
      expect(screen.getByText(/Burn Through/)).toBeInTheDocument();
    });
    expect(screen.getByText('Major')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminDefectCodeApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No defect codes found.')).toBeInTheDocument();
    });
  });

  it('shows Add Code button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Code/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Defect Codes')).toBeInTheDocument();
  });

  describe('Tier 3 read-only behavior', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
    });

    it('hides Add Code button for Tier 3', async () => {
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText(/101/)).toBeInTheDocument();
      });
      expect(screen.queryByRole('button', { name: /Add Code/i })).not.toBeInTheDocument();
    });

    it('hides edit and delete buttons for Tier 3', async () => {
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText(/101/)).toBeInTheDocument();
      });
      expect(screen.queryByLabelText(/edit/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/delete/i)).not.toBeInTheDocument();
    });
  });
});
