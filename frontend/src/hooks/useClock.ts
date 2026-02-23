import { useState, useEffect } from 'react';
import { formatClockTime } from '../utils/dateFormat.ts';
import { usePageVisible } from './usePageVisible.ts';

export function useClock(timeZoneId?: string): string {
  const [time, setTime] = useState(() => formatClockTime(new Date(), timeZoneId));
  const visible = usePageVisible();

  useEffect(() => {
    if (!visible) return;
    setTime(formatClockTime(new Date(), timeZoneId));
    const interval = setInterval(() => {
      setTime(formatClockTime(new Date(), timeZoneId));
    }, 1000);
    return () => clearInterval(interval);
  }, [timeZoneId, visible]);

  return time;
}
