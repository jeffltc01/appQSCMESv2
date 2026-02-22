const LOCALE = 'en-US' as const;

interface TzOption {
  timeZone?: string;
}

function tzOpts(tz?: string): TzOption {
  return tz ? { timeZone: tz } : {};
}

/** Full date + time: "2/22/2026, 3:45:12 PM" */
export function formatDateTime(iso: string, tz?: string): string {
  try {
    return new Date(iso).toLocaleString(LOCALE, {
      ...tzOpts(tz),
      month: 'numeric',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      second: '2-digit',
      hour12: true,
    });
  } catch {
    return iso;
  }
}

/** Compact date + time without seconds or year: "2/22 3:45 PM" */
export function formatShortDateTime(iso: string, tz?: string): string {
  try {
    const d = new Date(iso);
    const dateStr = d.toLocaleDateString(LOCALE, {
      ...tzOpts(tz),
      month: 'numeric',
      day: 'numeric',
    });
    const timeStr = d.toLocaleTimeString(LOCALE, {
      ...tzOpts(tz),
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
    return `${dateStr} ${timeStr}`;
  } catch {
    return iso;
  }
}

/** Time only: "3:45 PM" */
export function formatTimeOnly(iso: string, tz?: string): string {
  try {
    return new Date(iso).toLocaleTimeString(LOCALE, {
      ...tzOpts(tz),
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  } catch {
    return iso;
  }
}

/** Date only: "2/22/2026" */
export function formatDateOnly(iso: string, tz?: string): string {
  try {
    return new Date(iso).toLocaleDateString(LOCALE, {
      ...tzOpts(tz),
      month: 'numeric',
      day: 'numeric',
      year: 'numeric',
    });
  } catch {
    return iso;
  }
}

/** For <input type="date"> values: "2026-02-22". Uses local date parts to avoid UTC date-shift. */
export function formatDateForInput(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Today as "YYYY-MM-DD" for API query params. */
export function todayISOString(): string {
  return formatDateForInput(new Date());
}

/** Converts a Unix timestamp (seconds) to a formatted date string. Returns "--" if falsy. */
export function formatUnixDate(ts?: number, tz?: string): string {
  if (!ts) return '--';
  return formatDateOnly(new Date(ts * 1000).toISOString(), tz);
}

/** Formats a Date object for clock display: "2/22/2026, 3:45:12 PM" */
export function formatClockTime(date: Date, tz?: string): string {
  return date.toLocaleString(LOCALE, {
    ...tzOpts(tz),
    month: 'numeric',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
    hour12: true,
  });
}
