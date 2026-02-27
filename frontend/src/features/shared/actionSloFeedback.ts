import { reportActionTimingTelemetry } from '../../telemetry/telemetryClient.ts';

const PROCESSING_THRESHOLD_MS = 1000;
const RETRY_GUIDANCE_THRESHOLD_MS = 3000;
const ACK_LATENCY_TARGET_MS = 100;

export type ActionFeedbackState = {
  isPending: boolean;
  showProcessing: boolean;
  showRetryGuidance: boolean;
};

export const idleActionFeedbackState: ActionFeedbackState = {
  isPending: false,
  showProcessing: false,
  showRetryGuidance: false,
};

export async function runActionWithSloFeedback<T>(
  actionName: string,
  metadata: Record<string, unknown>,
  setState: (state: ActionFeedbackState) => void,
  action: () => Promise<T>,
): Promise<T> {
  const startedAt = performance.now();
  let ackLatencyMs = 0;
  let processingTimeout: number | null = null;
  let retryGuidanceTimeout: number | null = null;

  setState({ isPending: true, showProcessing: false, showRetryGuidance: false });
  ackLatencyMs = Math.round(performance.now() - startedAt);

  processingTimeout = window.setTimeout(() => {
    setState({ isPending: true, showProcessing: true, showRetryGuidance: false });
  }, PROCESSING_THRESHOLD_MS);
  retryGuidanceTimeout = window.setTimeout(() => {
    setState({ isPending: true, showProcessing: true, showRetryGuidance: true });
  }, RETRY_GUIDANCE_THRESHOLD_MS);

  try {
    return await action();
  } finally {
    if (processingTimeout != null) {
      window.clearTimeout(processingTimeout);
    }
    if (retryGuidanceTimeout != null) {
      window.clearTimeout(retryGuidanceTimeout);
    }
    const durationMs = performance.now() - startedAt;
    reportActionTimingTelemetry(actionName, {
      ...metadata,
      ackTargetMs: ACK_LATENCY_TARGET_MS,
      processingThresholdMs: PROCESSING_THRESHOLD_MS,
      retryGuidanceThresholdMs: RETRY_GUIDANCE_THRESHOLD_MS,
    }, durationMs, ackLatencyMs);
    setState(idleActionFeedbackState);
  }
}
