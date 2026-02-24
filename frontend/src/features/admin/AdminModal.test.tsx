import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AdminModal } from './AdminModal';

function renderModal(props: Partial<Parameters<typeof AdminModal>[0]> = {}) {
  const defaultProps: Parameters<typeof AdminModal>[0] = {
    open: true,
    title: 'Edit Item',
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    children: <p>Modal body content</p>,
  };
  const merged = { ...defaultProps, ...props };
  render(
    <FluentProvider theme={webLightTheme}>
      <AdminModal {...merged} />
    </FluentProvider>,
  );
  return merged;
}

describe('AdminModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders title and children', () => {
    renderModal({ title: 'Create Widget', children: <span>Widget form</span> });
    expect(screen.getByText('Create Widget')).toBeInTheDocument();
    expect(screen.getByText('Widget form')).toBeInTheDocument();
  });

  it('renders error message when error prop is set', () => {
    renderModal({ error: 'Something went wrong' });
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('Confirm button shows confirmLabel', () => {
    renderModal({ confirmLabel: 'Submit' });
    expect(screen.getByRole('button', { name: 'Submit' })).toBeInTheDocument();
  });

  it('Confirm button defaults to "OK" when no confirmLabel provided', () => {
    renderModal();
    expect(screen.getByRole('button', { name: 'OK' })).toBeInTheDocument();
  });

  it('Confirm button is disabled when confirmDisabled is true', () => {
    renderModal({ confirmDisabled: true });
    expect(screen.getByRole('button', { name: 'OK' })).toBeDisabled();
  });

  it('Confirm button is disabled when loading is true', () => {
    renderModal({ loading: true });
    const buttons = screen.getAllByRole('button');
    const confirmBtn = buttons.find(b => b.textContent !== 'Cancel' && b.getAttribute('aria-label') !== 'close');
    expect(confirmBtn).toBeDisabled();
  });

  it('Cancel button calls onCancel', async () => {
    const user = userEvent.setup();
    const props = renderModal();
    await user.click(screen.getByRole('button', { name: /cancel/i }));
    expect(props.onCancel).toHaveBeenCalledOnce();
  });

  it('Confirm button calls onConfirm', async () => {
    const user = userEvent.setup();
    const props = renderModal();
    await user.click(screen.getByRole('button', { name: 'OK' }));
    expect(props.onConfirm).toHaveBeenCalledOnce();
  });

  it('shows spinner when loading', () => {
    renderModal({ loading: true });
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('applies layout classes that keep footer actions visible', () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <AdminModal
          open
          title="Edit Item"
          onConfirm={vi.fn()}
          onCancel={vi.fn()}
        >
          <p>Modal body content</p>
        </AdminModal>
      </FluentProvider>,
    );

    const dialogBody = document.querySelector('.fui-DialogBody');
    const dialogActions = document.querySelector('.fui-DialogActions');

    expect(dialogBody).toBeTruthy();
    expect(dialogActions).toBeTruthy();
    expect(dialogBody?.className).toContain('body');
    expect(dialogActions?.className).toContain('actions');
  });
});
