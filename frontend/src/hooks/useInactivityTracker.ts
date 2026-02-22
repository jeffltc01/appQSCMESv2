import { useEffect, useRef, useCallback } from 'react';

interface UseInactivityTrackerOptions {
  enabled: boolean;
  thresholdMinutes: number;
  onInactivityDetected: (lastActivityTimestamp: number) => void;
}

export function useInactivityTracker({
  enabled,
  thresholdMinutes,
  onInactivityDetected,
}: UseInactivityTrackerOptions) {
  const lastActivityRef = useRef(Date.now());
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const callbackRef = useRef(onInactivityDetected);
  callbackRef.current = onInactivityDetected;

  const resetTimer = useCallback(() => {
    lastActivityRef.current = Date.now();

    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }

    if (!enabled || thresholdMinutes <= 0) return;

    timerRef.current = setTimeout(() => {
      callbackRef.current(lastActivityRef.current);
    }, thresholdMinutes * 60 * 1000);
  }, [enabled, thresholdMinutes]);

  useEffect(() => {
    if (!enabled) return;

    const handleActivity = () => resetTimer();

    const events: (keyof DocumentEventMap)[] = ['click', 'touchstart', 'keydown', 'pointerdown'];
    events.forEach(evt => document.addEventListener(evt, handleActivity, { passive: true }));

    resetTimer();

    return () => {
      events.forEach(evt => document.removeEventListener(evt, handleActivity));
      if (timerRef.current) {
        clearTimeout(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [enabled, resetTimer]);

  return { resetTimer, getLastActivity: () => lastActivityRef.current };
}
