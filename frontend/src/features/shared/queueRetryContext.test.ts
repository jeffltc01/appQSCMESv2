import { beforeEach, describe, expect, it, vi } from 'vitest';
import { clearRetryContext, loadRetryContext, saveRetryContext } from './queueRetryContext.ts';

describe('queueRetryContext', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.useRealTimers();
  });

  it('persists and restores form state for matching work center', () => {
    saveRetryContext('rolls_material', 'wc-1', {
      productId: 'p1',
      heatNumber: 'H100',
    });

    const restored = loadRetryContext('rolls_material', 'wc-1');

    expect(restored).toEqual({
      productId: 'p1',
      heatNumber: 'H100',
    });
  });

  it('does not restore context for a different work center', () => {
    saveRetryContext('rolls_material', 'wc-1', { productId: 'p1' });

    const restored = loadRetryContext('rolls_material', 'wc-2');

    expect(restored).toBeNull();
  });

  it('expires context older than 24 hours', () => {
    vi.useFakeTimers();
    saveRetryContext('fitup_queue', 'wc-9', { cardCode: '03' });
    vi.advanceTimersByTime(24 * 60 * 60 * 1000 + 1);

    const restored = loadRetryContext('fitup_queue', 'wc-9');

    expect(restored).toBeNull();
  });

  it('clears context explicitly', () => {
    saveRetryContext('fitup_queue', 'wc-9', { cardCode: '03' });
    clearRetryContext('fitup_queue');

    const restored = loadRetryContext('fitup_queue', 'wc-9');
    expect(restored).toBeNull();
  });
});
