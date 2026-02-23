import { describe, it, expect } from 'vitest';
import {
  formatDateTime,
  formatShortDateTime,
  formatTimeOnly,
  formatDateOnly,
  formatDateForInput,
  todayISOString,
  formatUnixDate,
  formatClockTime,
} from './dateFormat.ts';

const ISO = '2026-06-15T14:30:45.000Z';

describe('formatDateTime', () => {
  it('formats a valid ISO string', () => {
    const result = formatDateTime(ISO);
    expect(result).toContain('2026');
    expect(result).toMatch(/\d{1,2}:\d{2}:\d{2}/);
  });

  it('accepts a timezone parameter', () => {
    const result = formatDateTime(ISO, 'America/Denver');
    expect(result).toContain('2026');
  });

  it('does not throw on invalid date', () => {
    const result = formatDateTime('not-a-date');
    expect(typeof result).toBe('string');
  });
});

describe('formatShortDateTime', () => {
  it('formats without year or seconds', () => {
    const result = formatShortDateTime(ISO);
    expect(result).not.toContain('2026');
    expect(result).toMatch(/\d{1,2}\/\d{1,2}/);
  });

  it('does not throw on invalid date', () => {
    const result = formatShortDateTime('bad');
    expect(typeof result).toBe('string');
  });
});

describe('formatTimeOnly', () => {
  it('returns time without date', () => {
    const result = formatTimeOnly(ISO);
    expect(result).toMatch(/\d{1,2}:\d{2}\s?(AM|PM)/);
    expect(result).not.toContain('/');
  });

  it('does not throw on invalid date', () => {
    const result = formatTimeOnly('bad');
    expect(typeof result).toBe('string');
  });
});

describe('formatDateOnly', () => {
  it('returns date without time', () => {
    const result = formatDateOnly(ISO);
    expect(result).toContain('2026');
    expect(result).not.toMatch(/:\d{2}:\d{2}/);
  });

  it('does not throw on invalid date', () => {
    const result = formatDateOnly('nope');
    expect(typeof result).toBe('string');
  });
});

describe('formatDateForInput', () => {
  it('returns YYYY-MM-DD format', () => {
    const d = new Date(2026, 0, 5);
    expect(formatDateForInput(d)).toBe('2026-01-05');
  });

  it('pads single-digit months and days', () => {
    const d = new Date(2026, 2, 3);
    expect(formatDateForInput(d)).toBe('2026-03-03');
  });
});

describe('todayISOString', () => {
  it('returns today in YYYY-MM-DD format', () => {
    const result = todayISOString();
    expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    const now = new Date();
    expect(result).toBe(formatDateForInput(now));
  });
});

describe('formatUnixDate', () => {
  it('returns "--" for zero', () => {
    expect(formatUnixDate(0)).toBe('--');
  });

  it('returns "--" for undefined', () => {
    expect(formatUnixDate(undefined)).toBe('--');
  });

  it('converts a Unix timestamp (seconds) to a formatted date', () => {
    const ts = Math.floor(new Date('2026-06-15T00:00:00Z').getTime() / 1000);
    const result = formatUnixDate(ts);
    expect(result).toContain('2026');
  });

  it('accepts a timezone parameter', () => {
    const ts = Math.floor(new Date('2026-06-15T00:00:00Z').getTime() / 1000);
    const result = formatUnixDate(ts, 'America/Denver');
    expect(result).toContain('2026');
  });
});

describe('formatClockTime', () => {
  it('formats a Date object for display', () => {
    const d = new Date('2026-06-15T14:30:45Z');
    const result = formatClockTime(d);
    expect(result).toContain('2026');
    expect(result).toMatch(/\d{1,2}:\d{2}:\d{2}/);
  });

  it('accepts a timezone parameter', () => {
    const d = new Date('2026-06-15T14:30:45Z');
    const result = formatClockTime(d, 'America/Denver');
    expect(result).toContain('2026');
  });
});
