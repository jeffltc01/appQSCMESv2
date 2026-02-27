import { describe, it, expect, vi, beforeEach } from 'vitest';
import { api, setApiRequestObserver, setAuthToken, setRoleTier, setSiteId } from './apiClient.ts';

function mockFetch(status: number, body?: unknown, json = true) {
  return vi.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: json ? vi.fn().mockResolvedValue(body) : vi.fn().mockRejectedValue(new Error('no json')),
    text: vi.fn().mockResolvedValue(typeof body === 'string' ? body : JSON.stringify(body)),
  });
}

beforeEach(() => {
  setAuthToken(null);
  setRoleTier(null);
  setSiteId(null);
  setApiRequestObserver(null);
  vi.restoreAllMocks();
});

describe('header injection', () => {
  it('sends Content-Type by default', async () => {
    const fetch = mockFetch(200, []);
    vi.stubGlobal('fetch', fetch);
    await api.get('/test');
    const headers = fetch.mock.calls[0][1].headers;
    expect(headers['Content-Type']).toBe('application/json');
  });

  it('includes Authorization header when token is set', async () => {
    setAuthToken('my-token');
    const fetch = mockFetch(200, {});
    vi.stubGlobal('fetch', fetch);
    await api.get('/test');
    expect(fetch.mock.calls[0][1].headers['Authorization']).toBe('Bearer my-token');
  });

  it('includes X-User-Role-Tier header when role tier is set', async () => {
    setRoleTier(3);
    const fetch = mockFetch(200, {});
    vi.stubGlobal('fetch', fetch);
    await api.get('/test');
    expect(fetch.mock.calls[0][1].headers['X-User-Role-Tier']).toBe('3');
  });

  it('includes X-User-Site-Id header when site id is set', async () => {
    setSiteId('site-1');
    const fetch = mockFetch(200, {});
    vi.stubGlobal('fetch', fetch);
    await api.get('/test');
    expect(fetch.mock.calls[0][1].headers['X-User-Site-Id']).toBe('site-1');
  });

  it('omits optional headers when values are null', async () => {
    const fetch = mockFetch(200, {});
    vi.stubGlobal('fetch', fetch);
    await api.get('/test');
    const headers = fetch.mock.calls[0][1].headers;
    expect(headers['Authorization']).toBeUndefined();
    expect(headers['X-User-Role-Tier']).toBeUndefined();
    expect(headers['X-User-Site-Id']).toBeUndefined();
  });
});

describe('HTTP methods', () => {
  it('GET sends method and no body', async () => {
    const fetch = mockFetch(200, { data: 1 });
    vi.stubGlobal('fetch', fetch);
    const result = await api.get('/items');
    expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/items'), expect.objectContaining({ method: 'GET', body: undefined }));
    expect(result).toEqual({ data: 1 });
  });

  it('POST sends method and JSON body', async () => {
    const fetch = mockFetch(200, { id: 'new' });
    vi.stubGlobal('fetch', fetch);
    const result = await api.post('/items', { name: 'test' });
    expect(fetch.mock.calls[0][1].method).toBe('POST');
    expect(fetch.mock.calls[0][1].body).toBe(JSON.stringify({ name: 'test' }));
    expect(result).toEqual({ id: 'new' });
  });

  it('PUT sends method and JSON body', async () => {
    const fetch = mockFetch(200, { updated: true });
    vi.stubGlobal('fetch', fetch);
    await api.put('/items/1', { name: 'updated' });
    expect(fetch.mock.calls[0][1].method).toBe('PUT');
    expect(fetch.mock.calls[0][1].body).toBe(JSON.stringify({ name: 'updated' }));
  });

  it('DELETE sends method and no body', async () => {
    const fetch = mockFetch(204, undefined);
    vi.stubGlobal('fetch', fetch);
    await api.delete('/items/1');
    expect(fetch.mock.calls[0][1].method).toBe('DELETE');
    expect(fetch.mock.calls[0][1].body).toBeUndefined();
  });
});

describe('request observer', () => {
  it('emits request timing for successful calls', async () => {
    const observer = vi.fn();
    setApiRequestObserver(observer);
    vi.stubGlobal('fetch', mockFetch(200, { ok: true }));

    await api.get('/observer-success');

    expect(observer).toHaveBeenCalledWith(expect.objectContaining({
      method: 'GET',
      path: '/observer-success',
      status: 200,
      ok: true,
    }));
  });

  it('emits request timing for network failures', async () => {
    const observer = vi.fn();
    setApiRequestObserver(observer);
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network')));

    await expect(api.get('/observer-fail')).rejects.toEqual({ message: 'Network error' });
    expect(observer).toHaveBeenCalledWith(expect.objectContaining({
      method: 'GET',
      path: '/observer-fail',
      status: 0,
      ok: false,
      networkError: true,
    }));
  });
});

describe('204 No Content', () => {
  it('returns undefined on 204', async () => {
    vi.stubGlobal('fetch', mockFetch(204));
    const result = await api.delete('/items/1');
    expect(result).toBeUndefined();
  });
});

describe('error handling', () => {
  it('throws ApiError with message from JSON body', async () => {
    vi.stubGlobal('fetch', mockFetch(400, { message: 'Validation failed' }));
    await expect(api.post('/items', {})).rejects.toEqual({ message: 'Validation failed', code: undefined });
  });

  it('throws ApiError with title from JSON body when message absent', async () => {
    vi.stubGlobal('fetch', mockFetch(404, { title: 'Not Found', status: 404 }));
    await expect(api.get('/nope')).rejects.toEqual({ message: 'Not Found', code: '404' });
  });

  it('falls back to status text when JSON body is a plain string', async () => {
    vi.stubGlobal('fetch', mockFetch(500, 'Internal server error'));
    await expect(api.get('/fail')).rejects.toEqual({ message: 'Internal server error' });
  });

  it('falls back to generic message when response is not JSON', async () => {
    vi.stubGlobal('fetch', mockFetch(502, undefined, false));
    await expect(api.get('/fail')).rejects.toEqual({ message: 'Request failed with status 502' });
  });
});
