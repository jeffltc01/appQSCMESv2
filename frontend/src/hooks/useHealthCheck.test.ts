import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useHealthCheck } from './useHealthCheck';

describe('useHealthCheck', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('starts in checking state', () => {
    (fetch as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    const { result } = renderHook(() => useHealthCheck());
    expect(result.current).toBe('checking');
  });

  it('transitions to online on successful response', async () => {
    (fetch as ReturnType<typeof vi.fn>).mockResolvedValue({ ok: true });
    const { result } = renderHook(() => useHealthCheck());
    await act(async () => {});
    expect(result.current).toBe('online');
  });

  it('stays online after a single failure (requires 2 consecutive)', async () => {
    const mockFetch = fetch as ReturnType<typeof vi.fn>;
    mockFetch.mockResolvedValueOnce({ ok: true });

    const { result } = renderHook(() => useHealthCheck());
    await act(async () => {});
    expect(result.current).toBe('online');

    mockFetch.mockRejectedValueOnce(new Error('network'));
    await act(async () => {
      vi.advanceTimersByTime(15_000);
    });
    expect(result.current).toBe('online');
  });

  it('transitions to offline after 2 consecutive failures', async () => {
    const mockFetch = fetch as ReturnType<typeof vi.fn>;
    mockFetch.mockRejectedValue(new Error('network'));

    const { result } = renderHook(() => useHealthCheck());
    await act(async () => {});
    // First failure — still checking (not yet 2)
    expect(result.current).toBe('checking');

    await act(async () => {
      vi.advanceTimersByTime(15_000);
    });
    expect(result.current).toBe('offline');
  });

  it('recovers to online after being offline', async () => {
    const mockFetch = fetch as ReturnType<typeof vi.fn>;
    mockFetch.mockRejectedValue(new Error('network'));

    const { result } = renderHook(() => useHealthCheck());
    await act(async () => {});
    await act(async () => {
      vi.advanceTimersByTime(15_000);
    });
    expect(result.current).toBe('offline');

    mockFetch.mockResolvedValue({ ok: true });
    await act(async () => {
      vi.advanceTimersByTime(15_000);
    });
    expect(result.current).toBe('online');
  });
});
