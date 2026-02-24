import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';

const mockUseAuth = vi.fn();

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../features/maintenance/MaintenanceRequestDialog', () => ({
  MaintenanceRequestDialog: () => null,
}));

vi.mock('../../features/issueRequest/IssueRequestDialog', () => ({
  IssueRequestDialog: () => null,
}));

import { LeftPanel } from './LeftPanel';

function setUser(roleTier: number) {
  mockUseAuth.mockReturnValue({
    user: { id: 'u1', displayName: 'Test', employeeNumber: 'EMP001', roleTier },
    logout: vi.fn(),
  });
}

function renderLeftPanel(overrides: Partial<Parameters<typeof LeftPanel>[0]> = {}) {
  const defaults = { externalInput: false, currentGearLevel: 3 as number | null, ...overrides };
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <LeftPanel {...defaults} />
      </MemoryRouter>
    </FluentProvider>,
  );
}

const EXPECTED_LABELS = [
  'Maintenance Request',
  'Tablet Setup',
  'Schedule',
  'Report Issue',
  'Settings',
  'Logout',
];

describe('LeftPanel', () => {
  it('renders gear level display', () => {
    setUser(5);
    renderLeftPanel({ currentGearLevel: 3 });
    expect(screen.getByText('Gear 3')).toBeInTheDocument();
  });

  it('shows "--" when currentGearLevel is null', () => {
    setUser(5);
    renderLeftPanel({ currentGearLevel: null });
    expect(screen.getByText('Gear --')).toBeInTheDocument();
  });

  it('renders all expected buttons', () => {
    setUser(5);
    renderLeftPanel();
    for (const label of EXPECTED_LABELS) {
      expect(screen.getByRole('button', { name: label })).toBeInTheDocument();
    }
  });

  it('disables buttons when externalInput is true', () => {
    setUser(5);
    renderLeftPanel({ externalInput: true });
    for (const label of EXPECTED_LABELS) {
      expect(screen.getByRole('button', { name: label })).toBeDisabled();
    }
  });
});
