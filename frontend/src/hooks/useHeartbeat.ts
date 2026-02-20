import { useEffect, useRef } from 'react';
import { activeSessionApi } from '../api/endpoints.ts';

const HEARTBEAT_INTERVAL_MS = 60_000;

export function useHeartbeat(active: boolean) {
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    if (!active) {
      if (intervalRef.current) clearInterval(intervalRef.current);
      return;
    }

    const beat = () => {
      activeSessionApi.heartbeat().catch(() => {});
    };

    beat();
    intervalRef.current = setInterval(beat, HEARTBEAT_INTERVAL_MS);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [active]);
}
