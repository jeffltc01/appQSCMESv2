import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DowntimeReasonsScreen } from './DowntimeReasonsScreen.tsx';
import { downtimeReasonCategoryApi, downtimeReasonApi, siteApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { defaultSiteId: 'plant1', displayName: 'Test Admin', roleTier: 2 },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  siteApi: { getSites: vi.fn() },
  downtimeReasonCategoryApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
  downtimeReasonApi: {
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <DowntimeReasonsScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockPlants = [
  { id: 'plant1', name: 'Cleveland', code: 'CLE' },
  { id: 'plant2', name: 'Houston', code: 'HOU' },
];

const mockCategories = [
  {
    id: 'cat1',
    plantId: 'plant1',
    name: 'Mechanical',
    isActive: true,
    sortOrder: 0,
    reasons: [
      { id: 'r1', downtimeReasonCategoryId: 'cat1', categoryName: 'Mechanical', name: 'Bearing Failure', isActive: true, sortOrder: 0 },
      { id: 'r2', downtimeReasonCategoryId: 'cat1', categoryName: 'Mechanical', name: 'Belt Snap', isActive: true, sortOrder: 1 },
    ],
  },
  {
    id: 'cat2',
    plantId: 'plant1',
    name: 'Electrical',
    isActive: true,
    sortOrder: 1,
    reasons: [
      { id: 'r3', downtimeReasonCategoryId: 'cat2', categoryName: 'Electrical', name: 'Power Outage', isActive: true, sortOrder: 0 },
    ],
  },
];

describe('DowntimeReasonsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(siteApi.getSites).mockResolvedValue(mockPlants as any);
    vi.mocked(downtimeReasonCategoryApi.getAll).mockResolvedValue(mockCategories as any);
    vi.mocked(downtimeReasonCategoryApi.create).mockResolvedValue({} as any);
    vi.mocked(downtimeReasonApi.create).mockResolvedValue({} as any);
    vi.mocked(downtimeReasonCategoryApi.delete).mockResolvedValue(undefined as any);
    vi.mocked(downtimeReasonApi.delete).mockResolvedValue(undefined as any);
  });

  it('renders title correctly', async () => {
    renderScreen();
    expect(screen.getByText('Downtime Reasons')).toBeInTheDocument();
  });

  it('shows loading spinner initially', async () => {
    let resolveCategories!: (v: typeof mockCategories) => void;
    vi.mocked(downtimeReasonCategoryApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveCategories = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveCategories(mockCategories as any);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders categories after loading', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Mechanical')).toBeInTheDocument();
      expect(screen.getByText('Electrical')).toBeInTheDocument();
    });
  });

  it('shows reason count for each category', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('2 reasons')).toBeInTheDocument();
      expect(screen.getByText('1 reason')).toBeInTheDocument();
    });
  });

  it('renders reasons for the first selected category', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Bearing Failure')).toBeInTheDocument();
      expect(screen.getByText('Belt Snap')).toBeInTheDocument();
    });
  });

  it('switches reasons when clicking a different category', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Mechanical')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('Electrical'));
    await waitFor(() => {
      expect(screen.getByText('Power Outage')).toBeInTheDocument();
    });
  });

  it('shows empty state when no categories exist', async () => {
    vi.mocked(downtimeReasonCategoryApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No categories defined for this plant.')).toBeInTheDocument();
    });
  });

  it('shows plant selector for directors (roleTier <= 2)', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Cleveland')).toBeInTheDocument();
    });
  });

  it('shows Add buttons for users with edit permission', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Mechanical')).toBeInTheDocument();
    });
    const addButtons = screen.getAllByText('Add');
    expect(addButtons.length).toBeGreaterThanOrEqual(2);
  });

  it('opens add category modal when Add is clicked', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Mechanical')).toBeInTheDocument();
    });
    const addButtons = screen.getAllByText('Add');
    fireEvent.click(addButtons[0]);
    await waitFor(() => {
      expect(screen.getByText('Add Category')).toBeInTheDocument();
    });
  });

  it('opens add reason modal from the reasons panel', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Mechanical')).toBeInTheDocument();
    });
    const addButtons = screen.getAllByText('Add');
    fireEvent.click(addButtons[addButtons.length - 1]);
    await waitFor(() => {
      expect(screen.getByText('Add Reason')).toBeInTheDocument();
    });
  });
});
