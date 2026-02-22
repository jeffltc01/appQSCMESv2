import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useInactivityTracker } from './useInactivityTracker';

describe('useInactivityTracker', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not fire callback when disabled', () => {
    const onInactivity = vi.fn();
    renderHook(() => useInactivityTracker({
      enabled: false,
      thresholdMinutes: 1,
      onInactivityDetected: onInactivity,
    }));

    vi.advanceTimersByTime(120_000);
    expect(onInactivity).not.toHaveBeenCalled();
  });

  it('fires callback after threshold when enabled', () => {
    const onInactivity = vi.fn();
    renderHook(() => useInactivityTracker({
      enabled: true,
      thresholdMinutes: 1,
      onInactivityDetected: onInactivity,
    }));

    vi.advanceTimersByTime(59_000);
    expect(onInactivity).not.toHaveBeenCalled();

    vi.advanceTimersByTime(2_000);
    expect(onInactivity).toHaveBeenCalledTimes(1);
    expect(typeof onInactivity.mock.calls[0][0]).toBe('number');
  });

  it('resets timer on user interaction', () => {
    const onInactivity = vi.fn();
    renderHook(() => useInactivityTracker({
      enabled: true,
      thresholdMinutes: 1,
      onInactivityDetected: onInactivity,
    }));

    vi.advanceTimersByTime(50_000);
    act(() => { document.dispatchEvent(new Event('click')); });

    vi.advanceTimersByTime(50_000);
    expect(onInactivity).not.toHaveBeenCalled();

    vi.advanceTimersByTime(11_000);
    expect(onInactivity).toHaveBeenCalledTimes(1);
  });

  it('provides resetTimer function', () => {
    const onInactivity = vi.fn();
    const { result } = renderHook(() => useInactivityTracker({
      enabled: true,
      thresholdMinutes: 1,
      onInactivityDetected: onInactivity,
    }));

    vi.advanceTimersByTime(50_000);
    act(() => { result.current.resetTimer(); });

    vi.advanceTimersByTime(50_000);
    expect(onInactivity).not.toHaveBeenCalled();
  });

  it('cleans up on unmount', () => {
    const onInactivity = vi.fn();
    const { unmount } = renderHook(() => useInactivityTracker({
      enabled: true,
      thresholdMinutes: 1,
      onInactivityDetected: onInactivity,
    }));

    unmount();
    vi.advanceTimersByTime(120_000);
    expect(onInactivity).not.toHaveBeenCalled();
  });
});
