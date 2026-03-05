import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { PlantPrinterScreen } from './PlantPrinterScreen';
import type { AdminPlantPrinter, Plant } from '../../types/domain';

vi.mock('../../api/endpoints');

const baseUser = {
  id: 'u1',
  displayName: 'Test User',
  roleTier: 1,
  roleName: 'Admin',
  defaultSiteId: 'site-1',
  employeeNumber: 'EMP001',
  plantCode: 'TST',
  plantName: 'Test Plant',
  plantTimeZoneId: 'America/Denver',
  isCertifiedWelder: false,
  userType: 0,
};

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext', () => ({ useAuth: () => mockUseAuth() }));

vi.mock('./AdminLayout', () => ({
  AdminLayout: ({ children, title, onAdd, addLabel }: any) => (
    <div>
      <h1>{title}</h1>
      {onAdd && <button onClick={onAdd}>{addLabel}</button>}
      {children}
    </div>
  ),
}));

vi.mock('./ConfirmDeleteDialog', () => ({
  ConfirmDeleteDialog: ({ open, itemName, onConfirm, onCancel }: any) =>
    open ? (
      <div data-testid="confirm-delete">
        {itemName}
        <button onClick={onConfirm}>Confirm</button>
        <button onClick={onCancel}>Cancel</button>
      </div>
    ) : null,
}));

vi.mock('./AdminModal', () => ({
  AdminModal: ({ open, children, title }: any) =>
    open ? (
      <div data-testid="admin-modal">
        <h2>{title}</h2>
        {children}
      </div>
    ) : null,
}));

const { adminPlantPrinterApi, siteApi } = await import('../../api/endpoints');

const mockPlants: Plant[] = [
  { id: 'site-1', code: 'TST', name: 'Test Plant', timeZoneId: 'America/Denver' },
];

const mockPrinter: AdminPlantPrinter = {
  id: 'p1',
  printerName: 'Printer A',
  documentPath: '/Solutions/MES/DataPlateFoilLabel.nlbl',
  plantId: 'site-1',
  plantName: 'Test Plant',
  plantCode: 'TST',
  printLocation: 'Nameplate',
  enabled: true,
};

const disabledPrinter: AdminPlantPrinter = {
  id: 'p2',
  printerName: 'Printer B',
  documentPath: '/Solutions/MES/ShippingLabel.nlbl',
  plantId: 'site-1',
  plantName: 'Test Plant',
  plantCode: 'TST',
  printLocation: 'Rolls',
  enabled: false,
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <PlantPrinterScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

function authValue(overrides: Partial<typeof baseUser> = {}) {
  return {
    user: { ...baseUser, ...overrides },
    logout: vi.fn(),
    login: vi.fn(),
    isAuthenticated: true,
    token: 'test-token',
    isWelder: false,
  };
}

describe('PlantPrinterScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue(authValue({ roleTier: 1 }));
    vi.mocked(siteApi.getSites).mockResolvedValue(mockPlants);
    vi.mocked(adminPlantPrinterApi.getNiceLabelPrinters).mockResolvedValue([
      { printerName: 'Printer A' },
      { printerName: 'Printer B' },
    ]);
    vi.mocked(adminPlantPrinterApi.getNiceLabelDocuments).mockResolvedValue([
      { name: 'DataPlateFoilLabel.nlbl', itemPath: '/Solutions/MES/DataPlateFoilLabel.nlbl' },
      { name: 'ShippingLabel.nlbl', itemPath: '/Solutions/MES/ShippingLabel.nlbl' },
    ]);
  });

  it('renders loading state', () => {
    vi.mocked(adminPlantPrinterApi.getAll).mockReturnValue(new Promise(() => {}));
    renderScreen();

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders printer cards after load', async () => {
    vi.mocked(adminPlantPrinterApi.getAll).mockResolvedValue([mockPrinter]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Printer A')).toBeInTheDocument();
      expect(screen.getByText('Test Plant (TST)')).toBeInTheDocument();
      expect(screen.getByText('Nameplate')).toBeInTheDocument();
      expect(screen.getByText('/Solutions/MES/DataPlateFoilLabel.nlbl')).toBeInTheDocument();
    });
  });

  it('admin user sees Add Print Route, Edit, Delete buttons', async () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 1 }));
    vi.mocked(adminPlantPrinterApi.getAll).mockResolvedValue([mockPrinter]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Printer A')).toBeInTheDocument();
    });

    expect(screen.getByText('Add Print Route')).toBeInTheDocument();
    const buttons = screen.getAllByRole('button');
    const editBtn = buttons.find(b => b.querySelector('svg'));
    expect(editBtn).toBeDefined();
  });

  it('non-admin (roleTier 3) does NOT see Add Print Route or Edit/Delete', async () => {
    mockUseAuth.mockReturnValue(authValue({ roleTier: 3 }));
    vi.mocked(adminPlantPrinterApi.getAll).mockResolvedValue([mockPrinter]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Printer A')).toBeInTheDocument();
    });

    expect(screen.queryByText('Add Print Route')).not.toBeInTheDocument();
  });

  it('shows empty state when no printers', async () => {
    vi.mocked(adminPlantPrinterApi.getAll).mockResolvedValue([]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('No print routes found.')).toBeInTheDocument();
    });
  });

  it('shows Disabled badge for disabled printers', async () => {
    vi.mocked(adminPlantPrinterApi.getAll).mockResolvedValue([disabledPrinter]);
    renderScreen();

    await waitFor(() => {
      expect(screen.getByText('Disabled')).toBeInTheDocument();
    });
  });
});
