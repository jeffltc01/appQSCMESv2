const LOCALE = 'en-US' as const;
const WINDOWS_TO_IANA_TZ: Record<string, string> = {
  UTC: 'UTC',
  'Eastern Standard Time': 'America/New_York',
  'Central Standard Time': 'America/Chicago',
  'Mountain Standard Time': 'America/Denver',
  'US Mountain Standard Time': 'America/Phoenix',
  'Pacific Standard Time': 'America/Los_Angeles',
};

export function normalizeTimeZoneId(tz?: string): string | undefined {
  if (!tz) return undefined;
  if (tz.includes('/')) return tz;
  return WINDOWS_TO_IANA_TZ[tz] ?? tz;
}

interface TzOption {
  timeZone?: string;
}

function tzOpts(tz?: string): TzOption {
  const normalized = normalizeTimeZoneId(tz);
  return normalized ? { timeZone: normalized } : {};
}

/** Timezone short code for a specific instant (e.g. "CST", "CDT", "UTC"). */
export function getTimeZoneCode(dateLike: Date | string, tz?: string): string {
  try {
    const date = typeof dateLike === 'string' ? new Date(dateLike) : dateLike;
    const parts = new Intl.DateTimeFormat(LOCALE, {
      ...tzOpts(tz),
      timeZoneName: 'short',
    }).formatToParts(date);
    const tzPart = parts.find((part) => part.type === 'timeZoneName')?.value?.trim();
    return tzPart || 'UTC';
  } catch {
    return 'UTC';
  }
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

/** Compact date only without year: "2/22" */
export function formatShortDateOnly(iso: string, tz?: string): string {
  try {
    return new Date(iso).toLocaleDateString(LOCALE, {
      ...tzOpts(tz),
      month: 'numeric',
      day: 'numeric',
    });
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
