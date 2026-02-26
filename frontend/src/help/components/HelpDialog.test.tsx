import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { HelpDialog } from './HelpDialog';

vi.mock('react-markdown', () => ({
  default: ({ children }: { children: string }) => <div data-testid="markdown">{children}</div>,
}));

vi.mock('remark-gfm', () => ({
  default: () => {},
}));

vi.mock('/src/help/articles/*.md?raw', () => ({}));

beforeEach(() => {
  vi.restoreAllMocks();
  document.documentElement.classList.remove('help-scroll-open');
  document.body.classList.remove('help-scroll-open');
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
  it('renders explicit TOC and article scroll regions', () => {
    renderDialog();
    const tocRegion = screen.getByRole('navigation', { name: 'Help table of contents' });
    const articleRegion = screen.getByRole('region', { name: 'Help article content' });
    expect(tocRegion).toBeInTheDocument();
    expect(articleRegion).toBeInTheDocument();
    expect(tocRegion.className).toContain('toc');
    expect(articleRegion.className).toContain('article');
  });

  it('renders an article content region for scrollable help text', () => {
    renderDialog();
    const articleRegion = screen.getByRole('region', { name: 'Help article content' });
    expect(articleRegion).toBeInTheDocument();
    expect(articleRegion.className).toContain('article');
  });

  it('applies and removes help scroll override classes through open-close lifecycle', () => {
    vi.useFakeTimers();
    const onClose = vi.fn();
    const { rerender } = renderDialog({ open: true, onClose });
    expect(document.documentElement.classList.contains('help-scroll-open')).toBe(true);
    expect(document.body.classList.contains('help-scroll-open')).toBe(true);

    rerender(
      <FluentProvider theme={webLightTheme}>
        <HelpDialog open={false} onClose={onClose} initialSlug="overview" />
      </FluentProvider>,
    );

    // During close animation the dialog remains mounted, so the class stays active.
    expect(document.documentElement.classList.contains('help-scroll-open')).toBe(true);
    expect(document.body.classList.contains('help-scroll-open')).toBe(true);

    act(() => {
      vi.advanceTimersByTime(260);
    });

    expect(document.documentElement.classList.contains('help-scroll-open')).toBe(false);
    expect(document.body.classList.contains('help-scroll-open')).toBe(false);
    vi.useRealTimers();
  });

  it('keeps the dialog mounted while closing animation runs', () => {
    vi.useFakeTimers();
    const onClose = vi.fn();
    const { rerender } = renderDialog({ open: true, onClose });

    rerender(
      <FluentProvider theme={webLightTheme}>
        <HelpDialog open={false} onClose={onClose} initialSlug="overview" />
      </FluentProvider>,
    );

    const surface = screen.getByTestId('help-dialog-surface');
    expect(surface).toHaveAttribute('data-phase', 'closing');
    expect(screen.getByText('MES v2 Help')).toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(260);
    });
    expect(screen.queryByText('MES v2 Help')).not.toBeInTheDocument();
    vi.useRealTimers();
  });

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
