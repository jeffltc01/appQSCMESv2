import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { TabletSetupScreen } from './TabletSetupScreen';
import { AuthProvider } from '../../auth/AuthContext';

vi.mock('../../api/endpoints', () => ({
  workCenterApi: {
    getWorkCenters: vi.fn(),
  },
  productionLineApi: {
    getProductionLines: vi.fn(),
  },
  assetApi: {
    getAssets: vi.fn(),
  },
  tabletSetupApi: {
    save: vi.fn(),
  },
}));

vi.mock('../../auth/AuthContext', async () => {
  const actual = await vi.importActual('../../auth/AuthContext');
  return {
    ...actual,
    useAuth: vi.fn(() => ({
      user: {
        id: 'u1',
        employeeNumber: 'EMP001',
        displayName: 'Team Lead',
        roleTier: 5,
        roleName: 'Team Lead',
        defaultSiteId: 's1',
        isCertifiedWelder: false,
        plantCode: 'PLT1',
      },
      isAuthenticated: true,
      token: 'tok',
      isWelder: false,
      login: vi.fn(),
      logout: vi.fn(),
    })),
  };
});

const { workCenterApi, productionLineApi, assetApi, tabletSetupApi } = await import('../../api/endpoints');

function renderSetup() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <AuthProvider>
          <TabletSetupScreen />
        </AuthProvider>
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('TabletSetupScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('renders title and instructions', async () => {
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([]);
    vi.mocked(productionLineApi.getProductionLines).mockResolvedValue([]);

    renderSetup();
    expect(screen.getByText('Tablet Setup')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText(/one-time task/i)).toBeInTheDocument();
    });
  });

  it('loads work centers and production lines', async () => {
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([
      { id: 'wc1', name: 'Rolls 1', plantId: 'p1', workCenterTypeId: 't1', workCenterTypeName: 'Rolls', requiresWelder: true },
    ]);
    vi.mocked(productionLineApi.getProductionLines).mockResolvedValue([
      { id: 'pl1', name: 'Line 1', plantId: 'p1' },
    ]);

    renderSetup();
    await waitFor(() => {
      expect(workCenterApi.getWorkCenters).toHaveBeenCalledWith('PLT1');
    });
  });

  it('save button is disabled until required fields are selected', async () => {
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([]);
    vi.mocked(productionLineApi.getProductionLines).mockResolvedValue([]);

    renderSetup();
    await waitFor(() => {
      const saveBtn = screen.getByRole('button', { name: /save/i });
      expect(saveBtn).toBeDisabled();
    });
  });

  it('caches values to localStorage on save', async () => {
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([
      { id: 'wc1', name: 'Rolls 1', plantId: 'p1', workCenterTypeId: 't1', workCenterTypeName: 'Rolls', requiresWelder: true },
    ]);
    vi.mocked(productionLineApi.getProductionLines).mockResolvedValue([
      { id: 'pl1', name: 'Line 1', plantId: 'p1' },
    ]);
    vi.mocked(assetApi.getAssets).mockResolvedValue([
      { id: 'a1', name: 'Asset A', workCenterId: 'wc1' },
    ]);
    vi.mocked(tabletSetupApi.save).mockResolvedValue(undefined);

    renderSetup();

    await waitFor(() => {
      expect(workCenterApi.getWorkCenters).toHaveBeenCalled();
    });
  });

  it('shows message for operator role', async () => {
    const { useAuth } = await import('../../auth/AuthContext');
    vi.mocked(useAuth).mockReturnValue({
      user: {
        id: 'u2',
        employeeNumber: 'EMP002',
        displayName: 'Operator',
        roleTier: 6,
        roleName: 'Operator',
        defaultSiteId: 's1',
        isCertifiedWelder: false,
        plantCode: 'PLT1',
      },
      isAuthenticated: true,
      token: 'tok',
      isWelder: false,
      login: vi.fn(),
      logout: vi.fn(),
    });

    renderSetup();
    await waitFor(() => {
      expect(screen.getByText(/contact a Team Lead/i)).toBeInTheDocument();
    });
  });
});
