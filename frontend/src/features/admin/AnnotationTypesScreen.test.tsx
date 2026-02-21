import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AnnotationTypesScreen } from './AnnotationTypesScreen.tsx';
import { adminAnnotationTypeApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1, defaultSiteId: 's1' };
const tier3User = { plantCode: '000', plantName: 'Cleveland', displayName: 'QM User', roleTier: 3, defaultSiteId: 's1' };

vi.mock('../../api/endpoints.ts', () => ({
  adminAnnotationTypeApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <AnnotationTypesScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockItems = [
  {
    id: '1',
    name: 'Weld Defect',
    abbreviation: 'WD',
    requiresResolution: true,
    operatorCanCreate: false,
    displayColor: '#FF0000',
  },
  {
    id: '2',
    name: 'Surface Scratch',
    abbreviation: 'SS',
    requiresResolution: false,
    operatorCanCreate: true,
    displayColor: '#00FF00',
  },
];

describe('AnnotationTypesScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(adminAnnotationTypeApi.getAll).mockResolvedValue(mockItems);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockItems) => void;
    vi.mocked(adminAnnotationTypeApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockItems);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Weld Defect')).toBeInTheDocument();
    });
    expect(screen.getByText('Surface Scratch')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminAnnotationTypeApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No annotation types found.')).toBeInTheDocument();
    });
  });

  it('shows Add button for admin', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Annotation Type/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Annotation Types')).toBeInTheDocument();
  });

  describe('Tier 3 read-only behavior', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
    });

    it('hides Add button for non-admin users', async () => {
      renderScreen();
      await waitFor(() => {
        expect(screen.getByText('Weld Defect')).toBeInTheDocument();
      });
      expect(screen.queryByRole('button', { name: /Add Annotation Type/i })).not.toBeInTheDocument();
    });
  });
});
