import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { HelpDialog } from './HelpDialog';

vi.mock('react-markdown', () => ({
  default: ({ children }: { children: string }) => <div data-testid="markdown">{children}</div>,
}));

vi.mock('remark-gfm', () => ({
  default: () => {},
}));

const mockArticleContent = '# Test Article\n\nThis is test help content.';

vi.mock('/src/help/articles/*.md?raw', () => ({}));

beforeEach(() => {
  vi.restoreAllMocks();
});

function renderDialog(props: Partial<Parameters<typeof HelpDialog>[0]> = {}) {
  const defaults = {
    open: true,
    onClose: vi.fn(),
    initialSlug: 'overview',
    ...props,
  };
  return render(
    <FluentProvider theme={webLightTheme}>
      <HelpDialog {...defaults} />
    </FluentProvider>,
  );
}

describe('HelpDialog', () => {
  it('renders the dialog title', () => {
    renderDialog();
    expect(screen.getByText('MES v2 Help')).toBeInTheDocument();
  });

  it('renders table of contents with category labels', () => {
    renderDialog();
    expect(screen.getByText('General')).toBeInTheDocument();
    expect(screen.getByText('Operator Screens')).toBeInTheDocument();
    expect(screen.getByText('Admin & Supervisor')).toBeInTheDocument();
  });

  it('renders TOC items as buttons', () => {
    renderDialog();
    expect(screen.getByRole('button', { name: 'Login' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rolls' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Product Maintenance' })).toBeInTheDocument();
  });

  it('highlights the initial slug in the TOC', () => {
    renderDialog({ initialSlug: 'login' });
    const loginBtn = screen.getByRole('button', { name: 'Login' });
    expect(loginBtn.className).toContain('tocItemActive');
  });

  it('renders the download PDF link', () => {
    renderDialog();
    const link = screen.getByText('Download PDF Manual');
    expect(link).toBeInTheDocument();
    expect(link.closest('a')).toHaveAttribute('href', '/help/MESv2-Help-Manual.pdf');
  });

  it('calls onClose when close button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderDialog({ onClose });
    const closeBtn = screen.getByLabelText('Close');
    await user.click(closeBtn);
    expect(onClose).toHaveBeenCalled();
  });

  it('changes active article when TOC item is clicked', async () => {
    const user = userEvent.setup();
    renderDialog({ initialSlug: 'overview' });

    const loginBtn = screen.getByRole('button', { name: 'Login' });
    await user.click(loginBtn);

    expect(loginBtn.className).toContain('tocItemActive');
  });

  it('does not render when open is false', () => {
    renderDialog({ open: false });
    expect(screen.queryByText('MES v2 Help')).not.toBeInTheDocument();
  });
});
