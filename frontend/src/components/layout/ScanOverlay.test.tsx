import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
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
});
