import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
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
      children: [
        {
          id: 'assy-1',
          label: 'AC-001 (Assembled)',
          nodeType: 'assembled',
          children: [
            { id: 'child-1', label: 'SN-002 (Shell)', nodeType: 'shell' },
            { id: 'child-2', label: 'Heat H123 (leftHead)', nodeType: 'leftHead' },
          ],
        },
      ],
    },
  ],
  events: [
    {
      timestamp: '2026-02-15T14:30:00Z',
      workCenterName: 'Long Seam 1',
      type: 'Long Seam',
      completedBy: 'John Doe',
      assetName: 'Welder A',
      inspectionResult: null,
    },
    {
      timestamp: '2026-02-14T10:00:00Z',
      workCenterName: 'Inspection 1',
      type: 'Inspection',
      completedBy: 'Jane Smith',
      assetName: null,
      inspectionResult: 'pass',
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
    expect(screen.getByRole('button', { name: /Go/i })).toBeInTheDocument();
  });

  it('Go button is disabled when input is empty', () => {
    renderScreen();
    expect(screen.getByRole('button', { name: /Go/i })).toBeDisabled();
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

  it('performs lookup and shows results', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByRole('button', { name: /Go/i }));

    await waitFor(() => {
      expect(screen.getByText('SN-001 (Sellable)')).toBeInTheDocument();
    });
    expect(screen.getByText('AC-001 (Assembled)')).toBeInTheDocument();
    expect(screen.getByText('Long Seam 1')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
  });

  it('shows error when serial not found', async () => {
    vi.mocked(serialNumberApi.getLookup).mockRejectedValue(new Error('Not found'));
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'UNKNOWN');
    await user.click(screen.getByRole('button', { name: /Go/i }));

    await waitFor(() => {
      expect(screen.getByText('Serial number not found.')).toBeInTheDocument();
    });
  });

  it('hides events table when details is set to Hide', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByRole('button', { name: /Go/i }));

    await waitFor(() => {
      expect(screen.getByText('Long Seam 1')).toBeInTheDocument();
    });

    await user.click(screen.getByLabelText('Hide'));

    expect(screen.queryByText('Long Seam 1')).not.toBeInTheDocument();
  });

  it('can collapse and expand tree nodes', async () => {
    const user = userEvent.setup();
    renderScreen();

    const input = screen.getByPlaceholderText('Enter serial number...');
    await user.type(input, 'SN-001');
    await user.click(screen.getByRole('button', { name: /Go/i }));

    await waitFor(() => {
      expect(screen.getByText('SN-002 (Shell)')).toBeInTheDocument();
    });

    const sellableNode = screen.getByText('SN-001 (Sellable)');
    const chevron = sellableNode.previousElementSibling;
    if (chevron) fireEvent.click(chevron);

    await waitFor(() => {
      expect(screen.queryByText('AC-001 (Assembled)')).not.toBeInTheDocument();
    });
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
});
