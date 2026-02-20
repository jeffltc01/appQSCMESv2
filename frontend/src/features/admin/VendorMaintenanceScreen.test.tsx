import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { VendorMaintenanceScreen } from './VendorMaintenanceScreen.tsx';
import { adminVendorApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminVendorApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <VendorMaintenanceScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockVendors = [
  {
    id: '1',
    name: 'Mill Co',
    vendorType: 'mill',
    isActive: true,
  },
];

describe('VendorMaintenanceScreen', () => {
  beforeEach(() => {
    vi.mocked(adminVendorApi.getAll).mockResolvedValue(mockVendors);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockVendors) => void;
    vi.mocked(adminVendorApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockVendors);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Mill Co')).toBeInTheDocument();
    });
    expect(screen.getByText('mill')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminVendorApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No vendors found.')).toBeInTheDocument();
    });
  });

  it('shows Add Vendor button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Vendor/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Vendor Maintenance')).toBeInTheDocument();
  });
});
