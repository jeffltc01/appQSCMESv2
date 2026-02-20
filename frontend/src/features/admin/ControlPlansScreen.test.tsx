import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ControlPlansScreen } from './ControlPlansScreen.tsx';
import { adminControlPlanApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminControlPlanApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <ControlPlansScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockControlPlans = [
  {
    id: '1',
    characteristicName: 'Long Seam',
    workCenterName: 'Rolls 1',
    isEnabled: true,
    resultType: 'PassFail',
    isGateCheck: false,
  },
];

describe('ControlPlansScreen', () => {
  beforeEach(() => {
    vi.mocked(adminControlPlanApi.getAll).mockResolvedValue(mockControlPlans);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockControlPlans) => void;
    vi.mocked(adminControlPlanApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockControlPlans);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.getByText('Rolls 1')).toBeInTheDocument();
    expect(screen.getByText('PassFail')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminControlPlanApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No control plans found.')).toBeInTheDocument();
    });
  });

  it('does not show Add button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Long Seam')).toBeInTheDocument();
    });
    expect(screen.queryByRole('button', { name: /Add/i })).not.toBeInTheDocument();
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Control Plans')).toBeInTheDocument();
  });
});
