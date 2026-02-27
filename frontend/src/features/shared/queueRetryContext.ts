type RetryContextPayload = {
  form: Record<string, string>;
  workCenterId: string;
  savedAtUtc: string;
};

const STORAGE_PREFIX = 'mes_retry_context_';
const MAX_AGE_MS = 24 * 60 * 60 * 1000;

function getStorageKey(scope: string): string {
  return `${STORAGE_PREFIX}${scope}`;
}

export function saveRetryContext(scope: string, workCenterId: string, form: Record<string, string>): void {
  const payload: RetryContextPayload = {
    form,
    workCenterId,
    savedAtUtc: new Date().toISOString(),
  };

  try {
    sessionStorage.setItem(getStorageKey(scope), JSON.stringify(payload));
  } catch {
    // Best effort for managed browser policies.
  }
}

export function loadRetryContext(scope: string, workCenterId: string): Record<string, string> | null {
  try {
    const raw = sessionStorage.getItem(getStorageKey(scope));
    if (!raw) {
      return null;
    }

    const payload = JSON.parse(raw) as RetryContextPayload;
    if (payload.workCenterId !== workCenterId) {
      return null;
    }

    const savedAtMs = Date.parse(payload.savedAtUtc);
    if (Number.isNaN(savedAtMs) || Date.now() - savedAtMs > MAX_AGE_MS) {
      clearRetryContext(scope);
      return null;
    }

    return payload.form ?? null;
  } catch {
    return null;
  }
}

export function clearRetryContext(scope: string): void {
  try {
    sessionStorage.removeItem(getStorageKey(scope));
  } catch {
    // Best effort for managed browser policies.
  }
}
