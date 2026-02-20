import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkCenterConfigScreen } from './WorkCenterConfigScreen.tsx';
import { adminWorkCenterApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminWorkCenterApi: {
    getGrouped: vi.fn(),
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
        plantId: 'p1',
        plantName: 'Cleveland',
        siteName: 'Rolls 1',
        numberOfWelders: 2,
        productionLineId: 'pl1',
        materialQueueForWCId: null,
        materialQueueForWCName: null,
      },
    ],
  },
];

describe('WorkCenterConfigScreen', () => {
  beforeEach(() => {
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue(mockGroups);
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
    expect(screen.getByText('Cleveland')).toBeInTheDocument();
  });

  it('does not show Add button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getAllByText('Rolls 1').length).toBeGreaterThanOrEqual(1);
    });
    expect(screen.queryByRole('button', { name: /Add/i })).not.toBeInTheDocument();
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Work Center Config')).toBeInTheDocument();
  });

  it('renders without error when no groups', async () => {
    vi.mocked(adminWorkCenterApi.getGrouped).mockResolvedValue([]);
    renderScreen();
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
    expect(screen.getByText('Work Center Config')).toBeInTheDocument();
  });
});
