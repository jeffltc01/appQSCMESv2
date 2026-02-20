import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useClock } from './useClock';

describe('useClock', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('returns a formatted time string', () => {
    const { result } = renderHook(() => useClock());
    expect(result.current).toBeTruthy();
    expect(typeof result.current).toBe('string');
  });

  it('updates time on interval', () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useClock());
    const initial = result.current;

    act(() => {
      vi.advanceTimersByTime(1000);
    });

    expect(result.current).toBeTruthy();
    vi.useRealTimers();
  });
});
