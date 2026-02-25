import { describe, it, expect, vi } from 'vitest';
import { generateSessionId } from './sessionId.ts';

describe('generateSessionId', () => {
  it('uses crypto.randomUUID when available', () => {
    const originalCrypto = globalThis.crypto;
    Object.defineProperty(globalThis, 'crypto', {
      configurable: true,
      value: {
        randomUUID: () => 'uuid-from-randomuuid',
      },
    });

    expect(generateSessionId()).toBe('uuid-from-randomuuid');

    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: originalCrypto });
  });

  it('falls back when randomUUID is unavailable', () => {
    const originalCrypto = globalThis.crypto;
    Object.defineProperty(globalThis, 'crypto', {
      configurable: true,
      value: undefined,
    });

    const id = generateSessionId();
    expect(id).toBeTruthy();
    expect(typeof id).toBe('string');
    expect(id.length).toBeGreaterThan(8);

    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: originalCrypto });
  });

  it('uses getRandomValues when available', () => {
    const originalCrypto = globalThis.crypto;
    const getRandomValues = vi.fn((arr: Uint8Array) => {
      for (let i = 0; i < arr.length; i += 1) arr[i] = i;
      return arr;
    });

    Object.defineProperty(globalThis, 'crypto', {
      configurable: true,
      value: {
        getRandomValues,
      },
    });

    const id = generateSessionId();
    expect(getRandomValues).toHaveBeenCalled();
    expect(id).toMatch(/^[0-9a-f-]{36}$/);

    Object.defineProperty(globalThis, 'crypto', { configurable: true, value: originalCrypto });
  });
});
