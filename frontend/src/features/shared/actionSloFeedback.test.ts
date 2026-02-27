import { describe, expect, it, vi } from 'vitest';
import { idleActionFeedbackState, runActionWithSloFeedback } from './actionSloFeedback.ts';

vi.mock('../../telemetry/telemetryClient.ts', () => ({
  reportActionTimingTelemetry: vi.fn(),
}));

describe('runActionWithSloFeedback', () => {
  it('shows processing and retry guidance thresholds', async () => {
    vi.useFakeTimers();
    const states: Array<{ isPending: boolean; showProcessing: boolean; showRetryGuidance: boolean }> = [];
    let resolveDeferred: () => void = () => {};
    const deferred = new Promise<void>((resolve) => {
      resolveDeferred = resolve;
    });

    const runPromise = runActionWithSloFeedback(
      'test_action',
      { screen: 'Test' },
      (state) => { states.push(state); },
      () => deferred,
    );

    expect(states.at(-1)).toEqual({ isPending: true, showProcessing: false, showRetryGuidance: false });
    vi.advanceTimersByTime(1000);
    expect(states.at(-1)).toEqual({ isPending: true, showProcessing: true, showRetryGuidance: false });
    vi.advanceTimersByTime(2000);
    expect(states.at(-1)).toEqual({ isPending: true, showProcessing: true, showRetryGuidance: true });

    resolveDeferred();
    await runPromise;
    expect(states.at(-1)).toEqual(idleActionFeedbackState);
    vi.useRealTimers();
  });
});
