import { useState, useEffect } from 'react';
import { formatClockTime } from '../utils/dateFormat.ts';

export function useClock(timeZoneId?: string): string {
  const [time, setTime] = useState(() => formatClockTime(new Date(), timeZoneId));

  useEffect(() => {
    const interval = setInterval(() => {
      setTime(formatClockTime(new Date(), timeZoneId));
    }, 1000);
    return () => clearInterval(interval);
  }, [timeZoneId]);

  return time;
}
