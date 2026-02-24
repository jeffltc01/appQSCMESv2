import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { render, screen, waitFor } from '@testing-library/react';
import { MobileRoutes } from './MobileRoutes.tsx';

const mockUseAuth = vi.fn();

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminPlantGearApi: {
    getAll: vi.fn().mockResolvedValue([]),
  },
  productionLineApi: {
    getProductionLines: vi.fn().mockResolvedValue([]),
  },
  activeSessionApi: {
    getBySite: vi.fn().mockResolvedValue([]),
  },
  digitalTwinApi: {
    getSnapshot: vi.fn().mockResolvedValue({
      stations: [],
      materialFeeds: [],
      throughput: { unitsToday: 0, unitsDelta: 0, unitsPerHour: 0 },
      avgCycleTimeMinutes: 0,
      lineEfficiencyPercent: 0,
      unitTracker: [],
    }),
  },
}));

function renderRoutes(path = '/mobile') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/mobile/*" element={<MobileRoutes />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('MobileRoutes', () => {
  it('defaults quality director to quality portfolio', () => {
    mockUseAuth.mockReturnValue({ user: { roleTier: 2, roleName: 'Quality Director' } });
    renderRoutes('/mobile');
    expect(screen.getByText('Quality Portfolio')).toBeInTheDocument();
  });

  it('defaults ops director to operations portfolio', async () => {
    mockUseAuth.mockReturnValue({ user: { roleTier: 2, roleName: 'Ops Director' } });
    renderRoutes('/mobile');
    expect(screen.getByText('Operations Portfolio')).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText(/Updated|Live update unavailable/i)).toBeInTheDocument();
    });
  });

  it('defaults non-director roles to operator quick actions', () => {
    mockUseAuth.mockReturnValue({ user: { roleTier: 5, roleName: 'Team Lead' } });
    renderRoutes('/mobile');
    expect(screen.getByText('Operator Quick Actions')).toBeInTheDocument();
  });

  it('defaults kiosk operators to operator quick actions', () => {
    mockUseAuth.mockReturnValue({ user: { roleTier: 6, roleName: 'Operator' } });
    renderRoutes('/mobile');
    expect(screen.getByText('Operator Quick Actions')).toBeInTheDocument();
  });
});
