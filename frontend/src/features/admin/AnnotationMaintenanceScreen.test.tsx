import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AnnotationMaintenanceScreen } from './AnnotationMaintenanceScreen.tsx';
import { adminAnnotationApi, adminAnnotationTypeApi, siteApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminAnnotationApi: {
    getAll: vi.fn(),
    update: vi.fn(),
  },
  adminAnnotationTypeApi: {
    getAll: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

const directorUser = {
  id: 'u1',
  plantCode: 'PLT1',
  plantName: 'Cleveland',
  displayName: 'Director User',
  roleTier: 1,
  defaultSiteId: 's1',
};

const tier3User = {
  id: 'u2',
  plantCode: 'PLT1',
  plantName: 'Cleveland',
  displayName: 'QM User',
  roleTier: 3,
  defaultSiteId: 's1',
};

const mockAnnotations = [
  {
    id: 'a1',
    serialNumber: 'SN-001',
    annotationTypeName: 'Weld Defect',
    annotationTypeId: 'at1',
    flag: true,
    notes: 'Bad weld on seam',
    initiatedByName: 'Operator Joe',
    resolvedByName: null,
    resolvedNotes: null,
    createdAt: '2026-02-20T10:00:00Z',
  },
  {
    id: 'a2',
    serialNumber: 'SN-002',
    annotationTypeName: 'Surface Scratch',
    annotationTypeId: 'at2',
    flag: false,
    notes: 'Minor surface issue',
    initiatedByName: 'Operator Jane',
    resolvedByName: 'Inspector Smith',
    resolvedNotes: 'Buffed out',
    createdAt: '2026-02-19T14:30:00Z',
  },
];

const mockTypes = [
  { id: 'at1', name: 'Weld Defect', abbreviation: 'WD', requiresResolution: true, operatorCanCreate: false, displayColor: '#FF0000' },
  { id: 'at2', name: 'Surface Scratch', abbreviation: 'SS', requiresResolution: false, operatorCanCreate: true, displayColor: '#00FF00' },
];

const mockSites = [
  { id: 's1', code: 'PLT1', name: 'Cleveland', timeZoneId: 'America/New_York' },
  { id: 's2', code: 'PLT2', name: 'Houston', timeZoneId: 'America/Chicago' },
];

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <AnnotationMaintenanceScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('AnnotationMaintenanceScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: directorUser, logout: vi.fn() });
    vi.mocked(adminAnnotationApi.getAll).mockResolvedValue(mockAnnotations);
    vi.mocked(adminAnnotationTypeApi.getAll).mockResolvedValue(mockTypes);
    vi.mocked(siteApi.getSites).mockResolvedValue(mockSites);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockAnnotations) => void;
    vi.mocked(adminAnnotationApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockAnnotations);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders table rows after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });
    expect(screen.getByText('SN-002')).toBeInTheDocument();
    expect(screen.getAllByText('Weld Defect').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Surface Scratch').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminAnnotationApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No annotations found.')).toBeInTheDocument();
    });
  });

  it('displays correct title', () => {
    renderScreen();
    expect(screen.getByText('Annotations')).toBeInTheDocument();
  });

  it('shows site filter for director+', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });
    expect(screen.getByText('Site')).toBeInTheDocument();
  });

  it('hides site filter for tier 3 users', async () => {
    mockUseAuth.mockReturnValue({ user: tier3User, logout: vi.fn() });
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });
    expect(screen.queryByText('Site')).not.toBeInTheDocument();
  });

  it('filters by search text', async () => {
    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Serial, notes, initiated by...');
    await user.type(searchInput, 'SN-001');

    expect(screen.getByText('SN-001')).toBeInTheDocument();
    expect(screen.queryByText('SN-002')).not.toBeInTheDocument();
  });

  it('shows flag badges correctly', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const yesFlags = screen.getAllByText('Yes');
    const noFlags = screen.getAllByText('No');
    expect(yesFlags.length).toBeGreaterThanOrEqual(1);
    expect(noFlags.length).toBeGreaterThanOrEqual(1);
  });

  it('shows resolved/unresolved status', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Unresolved')).toBeInTheDocument();
    });
    expect(screen.getByText('Inspector Smith')).toBeInTheDocument();
  });

  it('opens edit modal when edit button clicked', async () => {
    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });

    const editButtons = screen.getAllByRole('button', { name: /edit annotation/i });
    await user.click(editButtons[0]);

    expect(screen.getByText('Edit Annotation')).toBeInTheDocument();
  });

  it('does not show Add button (annotations are read/edit only)', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('SN-001')).toBeInTheDocument();
    });
    expect(screen.queryByRole('button', { name: /add/i })).not.toBeInTheDocument();
  });

  it('shows annotation count', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/Showing 2 of 2 annotations/)).toBeInTheDocument();
    });
  });
});
