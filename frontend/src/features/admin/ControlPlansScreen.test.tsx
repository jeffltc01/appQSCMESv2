import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ControlPlansScreen } from './ControlPlansScreen.tsx';
import { adminControlPlanApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 };
const tier3User = { plantCode: '000', plantName: 'Cleveland', displayName: 'QM User', roleTier: 3 };

vi.mock('../../api/endpoints.ts', () => ({
  adminControlPlanApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
  },
  adminCharacteristicApi: {
    getAll: vi.fn().mockResolvedValue([]),
  },
  adminWorkCenterApi: {
    getAll: vi.fn().mockResolvedValue([]),
    getProductionLineConfigs: vi.fn().mockResolvedValue([]),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <ControlPlansScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockControlPlans = [
  {
    id: '1',
    characteristicId: 'ch1',
    characteristicName: 'Long Seam',
    workCenterProductionLineId: 'wcpl1',
    workCenterName: 'Rolls 1',
    productionLineName: 'Line 1',
    isEnabled: true,
    resultType: 'PassFail',
    isGateCheck: false,
    codeRequired: false,
    isActive: true,
  },
  {
    id: '2',
    characteristicId: 'ch2',
    characteristicName: 'RS1',
    workCenterProductionLineId: 'wcpl2',
    workCenterName: 'Round Seam',
    productionLineName: 'Line 2',
    isEnabled: true,
    resultType: 'AcceptReject',
    isGateCheck: true,
    codeRequired: true,
    isActive: true,
  },
];

describe('ControlPlansScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminControlPlanApi.getAll).mockResolvedValue(mockControlPlans);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockControlPlans) => void;
    vi.mocked(adminControlPlanApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockControlPlans);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    expect(screen.getByText('PassFail')).toBeInTheDocument();
  });

  it('shows ProductionLine name on cards', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.getByText('Line 1')).toBeInTheDocument();
    expect(screen.getByText('Line 2')).toBeInTheDocument();
  });

  it('shows Code Required badge', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('RS1')).toBeInTheDocument();
    });
    expect(screen.getByText('Code Required')).toBeInTheDocument();
  });

  it('renders all ResultType values', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('PassFail')).toBeInTheDocument();
      expect(screen.getByText('AcceptReject')).toBeInTheDocument();
    });
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminControlPlanApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No control plans found.')).toBeInTheDocument();
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
    expect(screen.getByText('Control Plans')).toBeInTheDocument();
  });

  describe('Tier 3 read-only behavior', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
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
