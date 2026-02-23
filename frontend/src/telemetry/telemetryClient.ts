import { buildAuthHeaders, getApiBaseUrl, setApiErrorObserver } from '../api/apiClient.ts';
import type { FrontendTelemetryIngestRequest } from '../types/api.ts';
import { getTabletCache } from '../hooks/useLocalStorage.ts';

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
    sessionId: trim(input.sessionId ?? sessionStorage.getItem('mes_session_id') ?? undefined, 128),
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
        break;
      }
      queue.shift();
    }
  } catch {
    // Best effort only. Keep queue for next retry tick.
  } finally {
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

export function initializeTelemetry() {
  if (initialized) return;
  initialized = true;

  if (!sessionStorage.getItem('mes_session_id')) {
    sessionStorage.setItem('mes_session_id', crypto.randomUUID());
  }

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

  flushTimer = window.setInterval(() => {
    void flushQueue();
  }, FLUSH_INTERVAL_MS);
}

export function shutdownTelemetry() {
  if (flushTimer != null) {
    window.clearInterval(flushTimer);
    flushTimer = null;
  }
  setApiErrorObserver(null);
  initialized = false;
}
