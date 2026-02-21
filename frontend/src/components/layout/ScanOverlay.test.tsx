import { describe, it, expect, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ScanOverlay } from './ScanOverlay';

describe('ScanOverlay', () => {
  it('renders success overlay with green background', () => {
    const onDismiss = vi.fn();
    render(<ScanOverlay result={{ type: 'success', message: 'Shell recorded' }} onDismiss={onDismiss} />);

    const overlay = screen.getByTestId('scan-overlay');
    expect(overlay).toBeInTheDocument();
    expect(overlay.style.backgroundColor).toContain('40, 167, 69');
    expect(screen.getByText('Shell recorded')).toBeInTheDocument();
  });

  it('renders error overlay with red background', () => {
    const onDismiss = vi.fn();
    render(<ScanOverlay result={{ type: 'error', message: 'Labels do not match' }} onDismiss={onDismiss} />);

    const overlay = screen.getByTestId('scan-overlay');
    expect(overlay.style.backgroundColor).toContain('220, 53, 69');
    expect(screen.getByText('Labels do not match')).toBeInTheDocument();
  });

  it('calls onDismiss when clicked', async () => {
    const user = userEvent.setup();
    const onDismiss = vi.fn();
    render(<ScanOverlay result={{ type: 'success' }} onDismiss={onDismiss} />);

    await user.click(screen.getByTestId('scan-overlay'));
    expect(onDismiss).toHaveBeenCalledTimes(1);
  });

  it('renders without message text', () => {
    render(<ScanOverlay result={{ type: 'success' }} onDismiss={() => {}} />);
    const overlay = screen.getByTestId('scan-overlay');
    expect(overlay).toBeInTheDocument();
  });

  it('shows "Tap to dismiss" hint on error overlay', () => {
    render(<ScanOverlay result={{ type: 'error', message: 'Something failed' }} onDismiss={() => {}} />);
    expect(screen.getByText('Tap to dismiss')).toBeInTheDocument();
  });

  it('does not show "Tap to dismiss" on success overlay', () => {
    render(<ScanOverlay result={{ type: 'success', message: 'Done' }} onDismiss={() => {}} />);
    expect(screen.queryByText('Tap to dismiss')).not.toBeInTheDocument();
  });

  it('shows countdown on error overlay when autoCloseMs is set', () => {
    vi.useFakeTimers();
    render(
      <ScanOverlay result={{ type: 'error', message: 'Failed' }} onDismiss={() => {}} autoCloseMs={10000} />,
    );

    expect(screen.getByText('Tap to dismiss (10s)')).toBeInTheDocument();

    act(() => { vi.advanceTimersByTime(1000); });
    expect(screen.getByText('Tap to dismiss (9s)')).toBeInTheDocument();

    act(() => { vi.advanceTimersByTime(4000); });
    expect(screen.getByText('Tap to dismiss (5s)')).toBeInTheDocument();

    vi.useRealTimers();
  });

  it('does not show countdown on success overlay even with autoCloseMs', () => {
    render(
      <ScanOverlay result={{ type: 'success', message: 'OK' }} onDismiss={() => {}} autoCloseMs={5000} />,
    );
    expect(screen.queryByText(/\(\d+s\)/)).not.toBeInTheDocument();
  });

  it('shows plain "Tap to dismiss" when no autoCloseMs', () => {
    render(
      <ScanOverlay result={{ type: 'error', message: 'Fail' }} onDismiss={() => {}} />,
    );
    expect(screen.getByText('Tap to dismiss')).toBeInTheDocument();
    expect(screen.queryByText(/\(\d+s\)/)).not.toBeInTheDocument();
  });
});
