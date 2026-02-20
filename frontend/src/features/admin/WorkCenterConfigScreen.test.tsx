import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkCenterConfigScreen } from './WorkCenterConfigScreen.tsx';
import { adminWorkCenterApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminWorkCenterApi: {
    getAll: vi.fn(),
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

const mockWorkCenters = [
  {
    id: 'wc1',
    name: 'Rolls 1',
    workCenterTypeName: 'Rolls',
    productionLineName: 'Line 1',
    plantName: 'Plant 1',
    numberOfWelders: 2,
    dataEntryType: 'standard',
  },
];

describe('WorkCenterConfigScreen', () => {
  beforeEach(() => {
    vi.mocked(adminWorkCenterApi.getAll).mockResolvedValue(mockWorkCenters);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockWorkCenters) => void;
    vi.mocked(adminWorkCenterApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockWorkCenters);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders work center cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    });
    expect(screen.getByText('Rolls')).toBeInTheDocument();
    expect(screen.getByText('Plant 1')).toBeInTheDocument();
  });

  it('does not show Add button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    });
    expect(screen.queryByRole('button', { name: /Add/i })).not.toBeInTheDocument();
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Work Center Config')).toBeInTheDocument();
  });

  it('renders without error when no work centers', async () => {
    vi.mocked(adminWorkCenterApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
    expect(screen.getByText('Work Center Config')).toBeInTheDocument();
  });
});
