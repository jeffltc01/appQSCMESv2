import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ProductMaintenanceScreen } from './ProductMaintenanceScreen.tsx';
import { adminProductApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminProductApi: {
    getAll: vi.fn(),
    getTypes: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn().mockResolvedValue([]),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <ProductMaintenanceScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockProducts = [
  {
    id: '1',
    productNumber: 'SH-100',
    tankSize: 100,
    tankType: 'Shell',
    productTypeName: 'Shell',
    productTypeId: 'pt1',
    isActive: true,
  },
];

const mockTypes = [{ id: 'pt1', name: 'Shell' }];

describe('ProductMaintenanceScreen', () => {
  beforeEach(() => {
    vi.mocked(adminProductApi.getAll).mockResolvedValue(mockProducts);
    vi.mocked(adminProductApi.getTypes).mockResolvedValue(mockTypes);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockProducts) => void;
    let resolveGetTypes!: (v: typeof mockTypes) => void;
    vi.mocked(adminProductApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    vi.mocked(adminProductApi.getTypes).mockImplementation(
      () => new Promise((r) => { resolveGetTypes = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockProducts);
    resolveGetTypes(mockTypes);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SH-100')).toBeInTheDocument();
    });
    expect(screen.getByText('Product Type')).toBeInTheDocument();
    expect(screen.getAllByText('Shell').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminProductApi.getAll).mockResolvedValue([]);
    vi.mocked(adminProductApi.getTypes).mockResolvedValue(mockTypes);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No products found.')).toBeInTheDocument();
    });
  });

  it('shows Add Product button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Product/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Product Maintenance')).toBeInTheDocument();
  });
});
