import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useHeartbeat } from './useHeartbeat';
import type { CreateActiveSessionRequest } from '../types/api.ts';

vi.mock('../api/endpoints.ts', () => ({
  activeSessionApi: {
    upsert: vi.fn().mockResolvedValue(undefined),
    heartbeat: vi.fn().mockResolvedValue(undefined),
  },
}));

const { activeSessionApi } = await import('../api/endpoints.ts');

const mockSession: CreateActiveSessionRequest = {
  workCenterId: 'wc-1',
  productionLineId: 'pl-1',
  assetId: undefined,
  plantId: '11111111-1111-1111-1111-111111111111',
};

describe('useHeartbeat', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('sends an immediate heartbeat when active with session', async () => {
    renderHook(() => useHeartbeat(true, mockSession));
    await waitFor(() => {
      expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);
    });
    expect(activeSessionApi.upsert).toHaveBeenCalledTimes(1);
  });

  it('does not send heartbeat when inactive', () => {
    renderHook(() => useHeartbeat(false));
    expect(activeSessionApi.heartbeat).not.toHaveBeenCalled();
    expect(activeSessionApi.upsert).not.toHaveBeenCalled();
  });

  it('does not send heartbeat when session is undefined', () => {
    renderHook(() => useHeartbeat(true, undefined));
    expect(activeSessionApi.heartbeat).not.toHaveBeenCalled();
    expect(activeSessionApi.upsert).not.toHaveBeenCalled();
  });

  it('sends heartbeat on interval', async () => {
    vi.useFakeTimers();
    renderHook(() => useHeartbeat(true, mockSession));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(10);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(2);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(3);
  });

  it('clears interval on unmount', async () => {
    vi.useFakeTimers();
    const { unmount } = renderHook(() => useHeartbeat(true, mockSession));

    await act(async () => {
      await vi.advanceTimersByTimeAsync(10);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);

    unmount();
    await act(async () => {
      await vi.advanceTimersByTimeAsync(120_000);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);
  });

  it('stops heartbeat when toggled from active to inactive', async () => {
    vi.useFakeTimers();
    const { rerender } = renderHook(
      ({ active, session }) => useHeartbeat(active, session),
      { initialProps: { active: true, session: mockSession as CreateActiveSessionRequest | undefined } },
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(10);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);

    rerender({ active: false, session: undefined });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(120_000);
    });
    expect(activeSessionApi.heartbeat).toHaveBeenCalledTimes(1);
  });
});
