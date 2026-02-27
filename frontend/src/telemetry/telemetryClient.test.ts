import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { initializeTelemetry, reportQueueFlowTelemetry, shutdownTelemetry } from './telemetryClient.ts';

describe('telemetryClient queue and reconnect behavior', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    sessionStorage.clear();
  });

  afterEach(() => {
    shutdownTelemetry();
  });

  it('emits queue_resync event via reportQueueFlowTelemetry', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 200, json: vi.fn(), text: vi.fn() });
    vi.stubGlobal('fetch', fetchMock);

    initializeTelemetry();
    reportQueueFlowTelemetry('queue_submit_success', { screen: 'RollsMaterial' });
    await Promise.resolve();

    expect(fetchMock).toHaveBeenCalled();
    const requestBody = JSON.parse(String(fetchMock.mock.calls[0][1].body));
    expect(requestBody.category).toBe('queue_resync');
    expect(requestBody.message).toBe('queue_submit_success');
  });

  it('emits network_state events when offline and online', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 200, json: vi.fn(), text: vi.fn() });
    vi.stubGlobal('fetch', fetchMock);

    initializeTelemetry();
    window.dispatchEvent(new Event('offline'));
    await Promise.resolve();

    window.dispatchEvent(new Event('online'));
    await Promise.resolve();

    const payloads = fetchMock.mock.calls
      .map((call) => JSON.parse(String(call[1].body)))
      .filter((payload) => payload.category === 'network_state');

    expect(payloads.length).toBeGreaterThanOrEqual(1);
    expect(payloads.some((p) => p.message.includes('offline'))).toBe(true);
  });
});
