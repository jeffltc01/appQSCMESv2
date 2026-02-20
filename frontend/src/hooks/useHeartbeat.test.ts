import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useHeartbeat } from './useHeartbeat';

vi.mock('../api/endpoints.ts', () => ({
  activeSessionApi: {
    heartbeat: vi.fn().mockResolvedValue(undefined),
  },
}));

const { activeSessionApi } = await import('../api/endpoints.ts');

describe('useHeartbeat', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('sends an immediate heartbeat when active', () => {
    renderHook(() => useHeartbeat(true));
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);
  });

  it('does not send heartbeat when inactive', () => {
    renderHook(() => useHeartbeat(false));
    expect(activeSessionApi.heartbeat).not.toHaveBeenCalled();
  });

  it('sends heartbeat on interval', () => {
    vi.useFakeTimers();
    renderHook(() => useHeartbeat(true));
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(60_000);
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(2);

    vi.advanceTimersByTime(60_000);
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(3);
  });

  it('clears interval on unmount', () => {
    vi.useFakeTimers();
    const { unmount } = renderHook(() => useHeartbeat(true));
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);

    unmount();
    vi.advanceTimersByTime(120_000);
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);
  });

  it('stops heartbeat when toggled from active to inactive', () => {
    vi.useFakeTimers();
    const { rerender } = renderHook(({ active }) => useHeartbeat(active), {
      initialProps: { active: true },
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);

    rerender({ active: false });
    vi.advanceTimersByTime(120_000);
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);
  });
});
