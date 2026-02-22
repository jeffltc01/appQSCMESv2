import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MaintenanceRequestDialog } from './MaintenanceRequestDialog';

const mockGetStatuses = vi.fn();
const mockGetMyRequests = vi.fn();
const mockCreateWorkRequest = vi.fn();

vi.mock('../../api/endpoints', () => ({
  limbleApi: {
    getStatuses: (...args: unknown[]) => mockGetStatuses(...args),
    getMyRequests: (...args: unknown[]) => mockGetMyRequests(...args),
    createWorkRequest: (...args: unknown[]) => mockCreateWorkRequest(...args),
  },
}));

function renderDialog(open = true) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MaintenanceRequestDialog
        open={open}
        onClose={vi.fn()}
        employeeNumber="EMP001"
        displayName="John Smith"
      />
    </FluentProvider>
  );
}

describe('MaintenanceRequestDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetStatuses.mockResolvedValue([
      { id: 1, name: 'Open' },
      { id: 2, name: 'Closed' },
    ]);
    mockGetMyRequests.mockResolvedValue([
      { id: 100, name: 'Fix valve', description: 'Leaking', priority: 3, statusId: 1, dueDate: 1700000000, createdDate: 1699000000 },
      { id: 101, name: 'Replace belt', description: null, priority: 1, statusId: 2, dueDate: null, createdDate: 1699100000 },
    ]);
  });

  it('loads and displays request list on open', async () => {
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText('Fix valve')).toBeInTheDocument();
    });

    expect(screen.getByText('Replace belt')).toBeInTheDocument();
    expect(mockGetMyRequests).toHaveBeenCalledWith('EMP001');
    expect(mockGetStatuses).toHaveBeenCalled();
  });

  it('shows empty state when no requests', async () => {
    mockGetMyRequests.mockResolvedValue([]);
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText(/no maintenance requests found/i)).toBeInTheDocument();
    });
  });

  it('shows error when load fails', async () => {
    mockGetMyRequests.mockRejectedValue(new Error('Network error'));
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText(/failed to load/i)).toBeInTheDocument();
    });
  });

  it('switches to form view when Add Request clicked', async () => {
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText('Fix valve')).toBeInTheDocument();
    });

    const addBtn = screen.getByText('Add Request');
    await userEvent.click(addBtn);

    expect(screen.getByText('New Maintenance Request')).toBeInTheDocument();
    expect(screen.getByText('Subject')).toBeInTheDocument();
    expect(screen.getByText('Description')).toBeInTheDocument();
    expect(screen.getByText('Priority')).toBeInTheDocument();
  });

  it('validates required fields on submit', async () => {
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText('Add Request')).toBeInTheDocument();
    });

    await userEvent.click(screen.getByText('Add Request'));
    await userEvent.click(screen.getByText('Submit Request'));

    await waitFor(() => {
      expect(screen.getByText('Subject is required.')).toBeInTheDocument();
    });
  });

  it('submits form and returns to list', async () => {
    mockCreateWorkRequest.mockResolvedValue({ id: 200, name: 'New request' });
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText('Add Request')).toBeInTheDocument();
    });

    await userEvent.click(screen.getByText('Add Request'));

    const subjectInput = screen.getByPlaceholderText(/brief description/i);
    await userEvent.type(subjectInput, 'Broken conveyor');

    const descInput = screen.getByPlaceholderText(/detailed description/i);
    await userEvent.type(descInput, 'Conveyor belt is torn');

    await userEvent.click(screen.getByText('Submit Request'));

    await waitFor(() => {
      expect(mockCreateWorkRequest).toHaveBeenCalledWith(
        expect.objectContaining({
          subject: 'Broken conveyor',
          description: 'Conveyor belt is torn',
          priority: 2,
        })
      );
    });
  });

  it('navigates back from form to list', async () => {
    renderDialog();

    await waitFor(() => {
      expect(screen.getByText('Add Request')).toBeInTheDocument();
    });

    await userEvent.click(screen.getByText('Add Request'));
    expect(screen.getByText('New Maintenance Request')).toBeInTheDocument();

    await userEvent.click(screen.getByText('Back'));

    await waitFor(() => {
      expect(screen.getByText('Maintenance Requests')).toBeInTheDocument();
    });
  });

  it('does not load data when dialog is closed', () => {
    renderDialog(false);
    expect(mockGetMyRequests).not.toHaveBeenCalled();
    expect(mockGetStatuses).not.toHaveBeenCalled();
  });
});
