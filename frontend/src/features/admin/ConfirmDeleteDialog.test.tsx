import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog';

function renderDialog(props: Partial<Parameters<typeof ConfirmDeleteDialog>[0]> = {}) {
  const defaultProps = {
    open: true,
    itemName: 'Work Center Alpha',
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    loading: false,
  };
  const merged = { ...defaultProps, ...props };
  render(
    <FluentProvider theme={webLightTheme}>
      <ConfirmDeleteDialog {...merged} />
    </FluentProvider>,
  );
  return merged;
}

describe('ConfirmDeleteDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders item name in dialog content', () => {
    renderDialog({ itemName: 'Line Bravo' });
    expect(screen.getByText('Line Bravo')).toBeInTheDocument();
  });

  it('Cancel button calls onCancel', async () => {
    const user = userEvent.setup();
    const props = renderDialog();
    await user.click(screen.getByRole('button', { name: /cancel/i }));
    expect(props.onCancel).toHaveBeenCalledOnce();
  });

  it('Deactivate button calls onConfirm', async () => {
    const user = userEvent.setup();
    const props = renderDialog();
    await user.click(screen.getByRole('button', { name: /deactivate/i }));
    expect(props.onConfirm).toHaveBeenCalledOnce();
  });

  it('buttons are disabled when loading', () => {
    renderDialog({ loading: true });
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled();
    const allButtons = screen.getAllByRole('button');
    const deactivateBtn = allButtons.find(b => b.textContent !== 'Cancel');
    expect(deactivateBtn).toBeDisabled();
  });

  it('shows spinner when loading instead of "Deactivate" text', () => {
    renderDialog({ loading: true });
    expect(screen.queryByText('Deactivate')).not.toBeInTheDocument();
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('renders custom delete wording when props are provided', () => {
    renderDialog({
      title: 'Confirm Delete Printer',
      confirmLabel: 'Delete Printer',
      message: (
        <p>
          Are you sure you want to delete <strong>Printer A</strong>?
        </p>
      ),
      details: <p>This printer configuration will be permanently removed.</p>,
    });

    expect(screen.getByText('Confirm Delete Printer')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete Printer' })).toBeInTheDocument();
    expect(screen.getByText(/Are you sure you want to delete/i)).toBeInTheDocument();
    expect(screen.getByText('This printer configuration will be permanently removed.')).toBeInTheDocument();
  });
});
