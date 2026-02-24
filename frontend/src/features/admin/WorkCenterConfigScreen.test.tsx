import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkCenterConfigScreen } from './WorkCenterConfigScreen.tsx';
import { adminWorkCenterApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminWorkCenterApi: {
    getGrouped: vi.fn(),
    getTypes: vi.fn(),
    create: vi.fn(),
    updateGroup: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <WorkCenterConfigScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockGroups = [
  {
    groupId: 'g1',
    baseName: 'Rolls 1',
    workCenterTypeName: 'Rolls',
    dataEntryType: 'Rolls',
    siteConfigs: [
      {
        workCenterId: 'wc1',
        siteName: 'Rolls 1',
        numberOfWelders: 2,
        materialQueueForWCId: undefined,
        materialQueueForWCName: undefined,
      },
    ],
  },
];

const mockWcTypes = [
  { id: 'wct-1', name: 'Rolls' },
  { id: 'wct-2', name: 'Inspection' },
];

describe('WorkCenterConfigScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue(mockGroups);
    vi.mocked(adminWorkCenterApi.getTypes).mockResolvedValue(mockWcTypes);
  });

  it('renders loading state initially', async () => {
    let resolveGetGrouped!: (v: typeof mockGroups) => void;
    vi.mocked(adminWorkCenterApi.getGrouped).mockImplementation(
      () => new Promise((r) => { resolveGetGrouped = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetGrouped(mockGroups);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders work center group cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getAllByText('Rolls 1').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Work Centers')).toBeInTheDocument();
  });

  it('renders without error when no groups', async () => {
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue([]);
    renderScreen();
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
    expect(screen.getByText('Work Centers')).toBeInTheDocument();
  });

  it('shows action buttons for admin user', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getAllByText('Rolls 1').length).toBeGreaterThanOrEqual(1);
    });
    const allButtons = screen.getAllByRole('button');
    expect(allButtons.length).toBeGreaterThanOrEqual(3);
  });

  it('opens create modal when Add button is clicked', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => {
      expect(screen.getAllByText('Rolls 1').length).toBeGreaterThanOrEqual(1);
    });

    const addButton = screen.getByRole('button', { name: /^Add$/i });
    await user.click(addButton);

    await waitFor(() => {
      expect(screen.getByText('Add Work Center')).toBeInTheDocument();
      expect(screen.getByText('Work Center Type')).toBeInTheDocument();
    });
  });
});
