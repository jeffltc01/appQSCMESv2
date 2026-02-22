import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { SerialNumberLookupScreen } from './SerialNumberLookupScreen.tsx';
import { serialNumberApi, siteApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  serialNumberApi: {
    getLookup: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

const directorUser = {
  plantCode: 'PLT1',
  plantName: 'Cleveland',
  displayName: 'Director User',
  roleTier: 1,
  defaultSiteId: 's1',
};

const supervisorUser = {
  plantCode: 'PLT1',
  plantName: 'Cleveland',
  displayName: 'Supervisor User',
  roleTier: 3,
  defaultSiteId: 's1',
};

const mockLookupResult = {
  serialNumber: 'SN-001',
  treeNodes: [
    {
      id: 'root-1',
      label: 'SN-001 (Sellable)',
      nodeType: 'sellable',
      serial: 'SN-001',
      tankSize: 500,
      tankType: '500 gal UG',
      createdAt: '2026-02-14T10:00:00Z',
      events: [
        {
          serialNumberId: 'root-1',
          serialNumberSerial: 'SN-001',
          timestamp: '2026-02-14T10:00:00Z',
          workCenterName: 'Hydro 1',
          type: 'Hydro Test',
          completedBy: 'Jane Smith',
          assetName: undefined,
          inspectionResult: 'pass',
        },
      ],
      children: [
        {
          id: 'assy-1',
          label: 'AC-001 (Assembled)',
          nodeType: 'assembled',
          serial: 'AC-001',
          tankSize: 500,
          tankType: '500 gal UG',
          events: [
            {
              serialNumberId: 'assy-1',
              serialNumberSerial: 'AC-001',
              timestamp: '2026-02-13T14:00:00Z',
              workCenterName: 'Fitup 1',
              type: 'Fitup',
              completedBy: 'Bob Fitter',
              assetName: undefined,
              inspectionResult: undefined,
            },
          ],
          children: [
            {
              id: 'child-1',
              label: 'SN-002 (24in Shell)',
              nodeType: 'shell',
              serial: 'SN-002',
              tankSize: 500,
              tankType: '24in Shell',
              events: [
                {
                  serialNumberId: 'child-1',
                  serialNumberSerial: 'SN-002',
                  timestamp: '2026-02-15T14:30:00Z',
                  workCenterName: 'Long Seam 1',
                  type: 'Long Seam',
                  completedBy: 'John Doe',
                  assetName: 'Welder A',
                  inspectionResult: undefined,
                },
              ],
            },
            {
              id: 'child-2',
              label: 'Heat H123 (leftHead)',
              nodeType: 'leftHead',
              serial: 'LOT-H123',
              heatNumber: 'H123',
              events: [],
            },
          ],
        },
      ],
    },
  ],
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <SerialNumberLookupScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('SerialNumberLookupScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ user: supervisorUser, logout: vi.fn() });
    vi.mocked(siteApi.getSites).mockResolvedValue([]);
    vi.mocked(serialNumberApi.getLookup).mockResolvedValue(mockLookupResult);
  });

  it('renders the title', () => {
    renderScreen();
    expect(screen.getByText('Serial Number Lookup')).toBeInTheDocument();
  });

  it('renders serial input and Go button', () => {
    renderScreen();
    expect(screen.getByPlaceholderText('Enter serial number...')).toBeInTheDocument();
    expect(screen.getByTestId('lookup-go-btn')).toBeInTheDocument();
  });

  it('Go button is disabled when input is empty', () => {
    renderScreen();
    expect(screen.getByTestId('lookup-go-btn')).toBeDisabled();
  });

  it('does not show site dropdown for tier 3+ users', () => {
    renderScreen();
    expect(screen.queryByLabelText('Site')).not.toBeInTheDocument();
  });

  it('shows site dropdown for Director+ users', () => {
    mockUseAuth.mockReturnValue({ user: directorUser, logout: vi.fn() });
    vi.mocked(siteApi.getSites).mockResolvedValue([
      { id: 's1', code: 'PLT1', name: 'Cleveland', timeZoneId: 'EST' },
    ]);
    renderScreen();
    expect(screen.getByText('Site')).toBeInTheDocument();
  });

  it('performs lookup and shows hero cards', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('genealogy-flow')).toBeInTheDocument();
    });

    expect(screen.getByTestId('hero-card-child-1')).toBeInTheDocument();
    expect(screen.getByTestId('hero-card-assy-1')).toBeInTheDocument();
    expect(screen.getByTestId('hero-card-root-1')).toBeInTheDocument();
  });

  it('shows serial numbers on hero cards', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByText('SN-002')).toBeInTheDocument();
    });
    expect(screen.getByText('AC-001')).toBeInTheDocument();
    expect(screen.getByText('SN-001')).toBeInTheDocument();
  });

  it('shows error when serial not found', async () => {
    vi.mocked(serialNumberApi.getLookup).mockRejectedValue(new Error('Not found'));
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'UNKNOWN');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByText('Serial number not found.')).toBeInTheDocument();
    });
  });

  it('expands card in-place to show events when clicked', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('hero-card-child-1')).toBeInTheDocument();
    });

    await user.click(screen.getByTestId('hero-card-child-1'));

    await waitFor(() => {
      expect(screen.getByTestId('events-table-child-1')).toBeInTheDocument();
    });
    expect(screen.getByText('Long Seam 1')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
  });

  it('collapses expanded card when clicked again', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('hero-card-child-1')).toBeInTheDocument();
    });

    await user.click(screen.getByTestId('hero-card-child-1'));
    await waitFor(() => {
      expect(screen.getByTestId('events-table-child-1')).toBeInTheDocument();
    });

    await user.click(screen.getByTestId('hero-card-child-1'));
    await waitFor(() => {
      expect(screen.queryByTestId('events-table-child-1')).not.toBeVisible();
    });
  });

  it('only one card expanded at a time', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('hero-card-child-1')).toBeInTheDocument();
    });

    await user.click(screen.getByTestId('hero-card-child-1'));
    await waitFor(() => {
      expect(screen.getByTestId('events-table-child-1')).toBeInTheDocument();
    });

    await user.click(screen.getByTestId('hero-card-root-1'));
    await waitFor(() => {
      expect(screen.getByTestId('events-table-root-1')).toBeInTheDocument();
    });
    expect(screen.queryByTestId('events-table-child-1')).not.toBeVisible();
  });

  it('performs lookup on Enter key', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001{Enter}');

    await waitFor(() => {
      expect(serialNumberApi.getLookup).toHaveBeenCalledWith('SN-001');
    });
  });

  it('renders the diagram legend', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('tree-legend')).toBeInTheDocument();
    });
    const legend = screen.getByTestId('tree-legend');
    expect(legend).toHaveTextContent('Diagram Key');
    expect(legend).toHaveTextContent('Finished SN');
    expect(legend).toHaveTextContent('Shells');
    expect(legend).toHaveTextContent('Heads');
    expect(legend).toHaveTextContent('Plate');
  });

  it('shows sub-components beneath parent cards', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('hero-card-child-2')).toBeInTheDocument();
    });
  });

  it('shows tank size on hero cards', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByTestId('lookup-go-btn'));

    await waitFor(() => {
      expect(screen.getByTestId('genealogy-flow')).toBeInTheDocument();
    });
    expect(screen.getAllByText('500 gal').length).toBeGreaterThan(0);
  });
});
