import { useEffect, useRef } from 'react';
import { activeSessionApi } from '../api/endpoints.ts';
import type { CreateActiveSessionRequest } from '../types/api.ts';

const HEARTBEAT_INTERVAL_MS = 60_000;

export function useHeartbeat(active: boolean, session?: CreateActiveSessionRequest) {
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const upsertedRef = useRef(false);

  useEffect(() => {
    if (!active || !session) {
      if (intervalRef.current) clearInterval(intervalRef.current);
      upsertedRef.current = false;
      return;
    }

    let cancelled = false;

    const start = async () => {
      try {
        await activeSessionApi.upsert(session);
        upsertedRef.current = true;
      } catch {
        // upsert failed — still attempt heartbeats in case session already exists
      }

      if (cancelled) return;

      const beat = () => {
        activeSessionApi.heartbeat().catch(() => {});
      };

      beat();
      intervalRef.current = setInterval(beat, HEARTBEAT_INTERVAL_MS);
    };

    start();

    return () => {
      cancelled = true;
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [active, session?.workCenterId, session?.productionLineId, session?.assetId, session?.siteCode]);
}
