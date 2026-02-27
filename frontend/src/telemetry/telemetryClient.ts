import {
  buildAuthHeaders,
  getApiBaseUrl,
  setApiErrorObserver,
  setApiRequestObserver,
} from '../api/apiClient.ts';
import type { FrontendTelemetryIngestRequest } from '../types/api.ts';
import { getTabletCache } from '../hooks/useLocalStorage.ts';
import { generateSessionId } from './sessionId.ts';

type TelemetryEvent = FrontendTelemetryIngestRequest;

const QUEUE_LIMIT = 200;
const FLUSH_INTERVAL_MS = 3000;
const DEDUPE_WINDOW_MS = 5000;
const RATE_WINDOW_MS = 60_000;
const RATE_LIMIT_PER_WINDOW = 120;

const queue: TelemetryEvent[] = [];
const lastSeenByFingerprint = new Map<string, number>();
let flushTimer: number | null = null;
let flushInProgress = false;
let initialized = false;
let windowStart = Date.now();
let sentInWindow = 0;
let lastNetworkState: 'online' | 'offline' = navigator.onLine ? 'online' : 'offline';
const SESSION_ID_KEY = 'mes_session_id';
const QUEUE_SEQ_PREFIX = 'mes_queue_seq_';

function trim(value: string | undefined, max: number): string | undefined {
  if (!value) return value;
  const normalized = value.trim();
  return normalized.length <= max ? normalized : normalized.slice(0, max);
}

function getSessionUser() {
  try {
    const raw = sessionStorage.getItem('mes_auth');
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { user?: { id?: string; defaultSiteId?: string } };
    return parsed.user ?? null;
  } catch {
    return null;
  }
}

function computeFingerprint(event: TelemetryEvent): string {
  return [
    event.category,
    event.source,
    event.message,
    event.route ?? '',
    event.screen ?? '',
    event.apiPath ?? '',
    event.httpStatus ?? '',
  ].join('|');
}

function getSessionId(): string | undefined {
  try {
    return sessionStorage.getItem(SESSION_ID_KEY) ?? undefined;
  } catch {
    return undefined;
  }
}

function ensureSessionId(): void {
  if (getSessionId()) return;
  try {
    sessionStorage.setItem(SESSION_ID_KEY, generateSessionId());
  } catch {
    // Some managed browsers can block Storage access.
  }
}

function nextQueueSequence(scope: string, workCenterId: string): number {
  const key = `${QUEUE_SEQ_PREFIX}${scope}_${workCenterId}`;
  try {
    const current = Number.parseInt(sessionStorage.getItem(key) ?? '0', 10);
    const next = Number.isFinite(current) ? current + 1 : 1;
    sessionStorage.setItem(key, String(next));
    return next;
  } catch {
    return Math.floor(Date.now() % 1_000_000_000);
  }
}

function passRateLimit(): boolean {
  const now = Date.now();
  if (now - windowStart >= RATE_WINDOW_MS) {
    windowStart = now;
    sentInWindow = 0;
  }
  if (sentInWindow >= RATE_LIMIT_PER_WINDOW) {
    return false;
  }
  sentInWindow += 1;
  return true;
}

function enrichEvent(input: TelemetryEvent): TelemetryEvent {
  const cache = getTabletCache();
  const user = getSessionUser();

  return {
    ...input,
    occurredAtUtc: input.occurredAtUtc ?? new Date().toISOString(),
    category: trim(input.category, 64) ?? 'unknown',
    source: trim(input.source, 64) ?? 'unknown',
    severity: trim(input.severity, 32) ?? 'error',
    message: trim(input.message, 2048) ?? 'No message',
    stack: trim(input.stack, 8000),
    metadataJson: trim(input.metadataJson, 8000),
    route: trim(input.route ?? window.location.pathname, 256),
    screen: trim(input.screen ?? document.title, 128),
    sessionId: trim(input.sessionId ?? getSessionId(), 128),
    apiPath: trim(input.apiPath, 256),
    httpMethod: trim(input.httpMethod, 16),
    userId: input.userId ?? user?.id,
    plantId: input.plantId ?? user?.defaultSiteId,
    workCenterId: input.workCenterId ?? cache?.cachedWorkCenterId,
    productionLineId: input.productionLineId ?? cache?.cachedProductionLineId,
  };
}

function queueEvent(event: TelemetryEvent) {
  if (queue.length >= QUEUE_LIMIT) {
    // Per requirement: when queue is full, drop the oldest event first.
    queue.shift();
  }
  queue.push(event);
}

async function flushQueue() {
  if (flushInProgress || queue.length === 0) return;
  flushInProgress = true;
  const startedAt = performance.now();
  const initialCount = queue.length;
  let sentCount = 0;
  let failedCount = 0;
  try {
    while (queue.length > 0) {
      const event = queue[0];
      const response = await fetch(`${getApiBaseUrl()}/frontend-telemetry`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...buildAuthHeaders(),
        },
        body: JSON.stringify(event),
        keepalive: true,
      });

      if (!response.ok) {
        failedCount = queue.length;
        break;
      }
      queue.shift();
      sentCount += 1;
    }
  } catch {
    failedCount = queue.length;
    // Best effort only. Keep queue for next retry tick.
  } finally {
    if (sentCount > 0 || failedCount > 0) {
      const totalAttempted = sentCount + failedCount;
      reportTelemetry({
        category: 'queue_resync',
        source: 'telemetry_client',
        severity: failedCount > 0 ? 'warning' : 'info',
        isReactRuntimeOverlayCandidate: false,
        message: failedCount > 0 ? 'telemetry_queue_replay_partial' : 'telemetry_queue_replay_completed',
        metadataJson: JSON.stringify({
          initialCount,
          sentCount,
          failedCount,
          totalAttempted,
          failureRatePct: totalAttempted > 0 ? Math.round((failedCount / totalAttempted) * 10_000) / 100 : 0,
          remainingCount: queue.length,
          flushDurationMs: Math.round(performance.now() - startedAt),
        }),
      });
    }
    flushInProgress = false;
  }
}

export function reportTelemetry(event: TelemetryEvent) {
  const enriched = enrichEvent(event);
  const fingerprint = trim(event.fingerprint, 512) ?? computeFingerprint(enriched);
  const now = Date.now();
  const seenAt = lastSeenByFingerprint.get(fingerprint);
  if (seenAt != null && now - seenAt < DEDUPE_WINDOW_MS) {
    return;
  }
  lastSeenByFingerprint.set(fingerprint, now);

  if (!passRateLimit()) {
    return;
  }

  queueEvent(enriched);
  void flushQueue();
}

export function reportException(error: unknown, context: Partial<TelemetryEvent>) {
  const err = error instanceof Error ? error : new Error(String(error));
  reportTelemetry({
    category: context.category ?? 'runtime_error',
    source: context.source ?? 'exception',
    severity: context.severity ?? 'error',
    isReactRuntimeOverlayCandidate: context.isReactRuntimeOverlayCandidate ?? false,
    message: context.message ?? err.message,
    stack: context.stack ?? err.stack,
    metadataJson: context.metadataJson,
    route: context.route,
    screen: context.screen,
    apiPath: context.apiPath,
    httpMethod: context.httpMethod,
    httpStatus: context.httpStatus,
    userId: context.userId,
    workCenterId: context.workCenterId,
    productionLineId: context.productionLineId,
    plantId: context.plantId,
    fingerprint: context.fingerprint,
  });
}

export function reportQueueFlowTelemetry(
  eventName: string,
  metadata: Record<string, unknown> & { workCenterId?: string; screen?: string; sequenceId?: number },
): void {
  const scope = String(metadata.screen ?? 'queue');
  const workCenterId = String(metadata.workCenterId ?? 'unknown');
  const sequenceId = typeof metadata.sequenceId === 'number'
    ? metadata.sequenceId
    : nextQueueSequence(scope, workCenterId);

  reportTelemetry({
    category: 'queue_resync',
    source: 'operator_queue',
    severity: 'info',
    isReactRuntimeOverlayCandidate: false,
    message: eventName,
    metadataJson: JSON.stringify({
      ...metadata,
      sequenceId,
      requestId: `${scope}:${workCenterId}:${sequenceId}`,
    }),
  });
}

export function reportActionTimingTelemetry(
  actionName: string,
  metadata: Record<string, unknown>,
  durationMs: number,
  ackLatencyMs: number,
): void {
  reportTelemetry({
    category: 'ux_timing',
    source: 'operator_action',
    severity: 'info',
    isReactRuntimeOverlayCandidate: false,
    message: actionName,
    metadataJson: JSON.stringify({
      ...metadata,
      durationMs: Math.round(durationMs),
      ackLatencyMs: Math.round(ackLatencyMs),
    }),
  });
}

function handleNetworkState(nextState: 'online' | 'offline'): void {
  if (lastNetworkState === nextState) {
    return;
  }

  lastNetworkState = nextState;
  reportTelemetry({
    category: 'network_state',
    source: 'telemetry_client',
    severity: nextState === 'offline' ? 'warning' : 'info',
    isReactRuntimeOverlayCandidate: false,
    message: nextState === 'offline' ? 'Browser went offline' : 'Browser reconnected',
    metadataJson: JSON.stringify({ state: nextState }),
  });

  if (nextState === 'online') {
    void flushQueue();
  }
}

function onOnline(): void {
  handleNetworkState('online');
}

function onOffline(): void {
  handleNetworkState('offline');
}

export function initializeTelemetry() {
  if (initialized) return;
  initialized = true;

  ensureSessionId();

  setApiErrorObserver((payload) => {
    if (payload.path.includes('/frontend-telemetry')) return;
    reportTelemetry({
      category: 'api_error',
      source: 'api_client',
      severity: 'error',
      isReactRuntimeOverlayCandidate: false,
      message: payload.message,
      apiPath: payload.path,
      httpMethod: payload.method,
      httpStatus: payload.status,
      metadataJson: JSON.stringify({ code: payload.code, networkError: payload.networkError === true }),
    });
  });
  setApiRequestObserver((payload) => {
    if (payload.path.includes('/frontend-telemetry')) return;
    reportTelemetry({
      category: 'api_timing',
      source: 'api_client',
      severity: payload.ok ? 'info' : 'warning',
      isReactRuntimeOverlayCandidate: false,
      message: `${payload.method} ${payload.path}`,
      apiPath: payload.path,
      httpMethod: payload.method,
      httpStatus: payload.status,
      metadataJson: JSON.stringify({
        elapsedMs: payload.elapsedMs,
        ok: payload.ok,
        networkError: payload.networkError === true,
      }),
    });
  });

  window.addEventListener('error', (event) => {
    reportTelemetry({
      category: 'runtime_error',
      source: 'window_error',
      severity: 'error',
      isReactRuntimeOverlayCandidate: true,
      message: event.message || 'Unhandled window error',
      stack: event.error?.stack,
      metadataJson: JSON.stringify({
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
      }),
    });
  });

  window.addEventListener('unhandledrejection', (event) => {
    const reason = event.reason instanceof Error ? event.reason : new Error(String(event.reason));
    reportTelemetry({
      category: 'unhandled_promise',
      source: 'window_unhandledrejection',
      severity: 'error',
      isReactRuntimeOverlayCandidate: true,
      message: reason.message,
      stack: reason.stack,
    });
  });

  window.addEventListener('online', onOnline);
  window.addEventListener('offline', onOffline);

  flushTimer = window.setInterval(() => {
    void flushQueue();
  }, FLUSH_INTERVAL_MS);
}

export function shutdownTelemetry() {
  if (flushTimer != null) {
    window.clearInterval(flushTimer);
    flushTimer = null;
  }
  window.removeEventListener('online', onOnline);
  window.removeEventListener('offline', onOffline);
  setApiErrorObserver(null);
  setApiRequestObserver(null);
  initialized = false;
}
