import { useState, useEffect, useRef } from 'react';
import { usePageVisible } from './usePageVisible.ts';

export type HealthStatus = 'online' | 'offline' | 'checking';

const POLL_INTERVAL_MS = 15_000;
const FAILURES_BEFORE_OFFLINE = 2;

export function useHealthCheck(): HealthStatus {
  const [status, setStatus] = useState<HealthStatus>('checking');
  const consecutiveFailures = useRef(0);
  const visible = usePageVisible();

  useEffect(() => {
    if (!visible) return;

    let cancelled = false;

    async function check() {
      try {
        const res = await fetch('/healthz', { method: 'GET', cache: 'no-store' });
        if (!cancelled) {
          if (res.ok) {
            consecutiveFailures.current = 0;
            setStatus('online');
          } else {
            consecutiveFailures.current += 1;
            if (consecutiveFailures.current >= FAILURES_BEFORE_OFFLINE) {
              setStatus('offline');
            }
          }
        }
      } catch {
        if (!cancelled) {
          consecutiveFailures.current += 1;
          if (consecutiveFailures.current >= FAILURES_BEFORE_OFFLINE) {
            setStatus('offline');
          }
        }
      }
    }

    check();
    const interval = setInterval(check, POLL_INTERVAL_MS);

    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, [visible]);

  return status;
}
