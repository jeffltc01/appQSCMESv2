import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AuditLogScreen } from './AuditLogScreen.tsx';
import { auditLogApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  auditLogApi: {
    getLogs: vi.fn(),
    getEntityNames: vi.fn(),
  },
}));

const adminUser = { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin', roleTier: 1 };

const mockPage = {
  items: [
    {
      id: 1,
      action: 'Created',
      entityName: 'Vendor',
      entityId: '11111111-1111-1111-1111-111111111111',
      changes: '{"Name":{"old":null,"new":"Acme"}}',
      changedByUserName: 'Test Admin',
      changedByUserId: '99999999-9999-9999-9999-999999999999',
      changedAtUtc: '2026-02-23T10:00:00Z',
    },
    {
      id: 2,
      action: 'Updated',
      entityName: 'Product',
      entityId: '22222222-2222-2222-2222-222222222222',
      changes: '{"TankSize":{"old":"100","new":"200"}}',
      changedByUserName: 'Test Admin',
      changedByUserId: '99999999-9999-9999-9999-999999999999',
      changedAtUtc: '2026-02-23T11:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 50,
};

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <AuditLogScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('AuditLogScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: adminUser, logout: vi.fn() });
    vi.mocked(auditLogApi.getLogs).mockResolvedValue(mockPage);
    vi.mocked(auditLogApi.getEntityNames).mockResolvedValue(['Product', 'Vendor']);
  });

  it('renders the page title', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Audit Log')).toBeInTheDocument();
    });
  });

  it('displays audit log entries after load', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getAllByText('Vendor').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Product').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows action badges', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Created')).toBeInTheDocument();
      expect(screen.getByText('Updated')).toBeInTheDocument();
    });
  });

  it('shows pagination info', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/Page 1 of 1/)).toBeInTheDocument();
      expect(screen.getByText(/2 total/)).toBeInTheDocument();
    });
  });

  it('shows empty state when no results', async () => {
    vi.mocked(auditLogApi.getLogs).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });

    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No audit log entries found.')).toBeInTheDocument();
    });
  });

  it('expands change detail when View is clicked', async () => {
    renderScreen();

    await waitFor(() => {
      expect(screen.getAllByText('View')).toHaveLength(2);
    });

    const viewButtons = screen.getAllByText('View');
    await userEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('Name')).toBeInTheDocument();
      expect(screen.getByText('Acme')).toBeInTheDocument();
    });
  });
});
