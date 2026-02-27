import type { ApiError } from '../types/api.ts';

function normalizeApiBaseUrl(rawBaseUrl?: string): string {
  const value = rawBaseUrl?.trim();
  if (!value) {
    return '/api';
  }

  // Keep local proxy-style paths exactly as configured.
  if (value.startsWith('/')) {
    return value.replace(/\/+$/, '') || '/api';
  }

  try {
    const url = new URL(value);
    const normalizedPath = url.pathname.replace(/\/+$/, '');
    const pathWithApi = normalizedPath.endsWith('/api') ? normalizedPath : `${normalizedPath}/api`;
    url.pathname = pathWithApi;
    return url.toString().replace(/\/+$/, '');
  } catch {
    return value.replace(/\/+$/, '');
  }
}

const BASE_URL = normalizeApiBaseUrl(import.meta.env.VITE_API_URL);

let authToken: string | null = null;
let roleTierHeader: string | null = null;
let siteIdHeader: string | null = null;
let userIdHeader: string | null = null;
type ApiErrorObserverPayload = {
  method: string;
  path: string;
  status?: number;
  message: string;
  code?: string;
  networkError?: boolean;
};
type ApiRequestObserverPayload = {
  method: string;
  path: string;
  status: number;
  elapsedMs: number;
  ok: boolean;
  networkError?: boolean;
};
let apiErrorObserver: ((payload: ApiErrorObserverPayload) => void) | null = null;
let apiRequestObserver: ((payload: ApiRequestObserverPayload) => void) | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}

export function setRoleTier(tier: number | null) {
  roleTierHeader = tier != null ? String(tier) : null;
}

export function setSiteId(siteId: string | null) {
  siteIdHeader = siteId ?? null;
}

export function setUserId(userId: string | null) {
  userIdHeader = userId ?? null;
}

export function getAuthToken() {
  return authToken;
}

export function getApiBaseUrl() {
  return BASE_URL;
}

export function buildAuthHeaders(): Record<string, string> {
  const headers: Record<string, string> = {};
  if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`;
  }
  if (roleTierHeader) {
    headers['X-User-Role-Tier'] = roleTierHeader;
  }
  if (siteIdHeader) {
    headers['X-User-Site-Id'] = siteIdHeader;
  }
  if (userIdHeader) {
    headers['X-User-Id'] = userIdHeader;
  }
  return headers;
}

export function setApiErrorObserver(observer: ((payload: ApiErrorObserverPayload) => void) | null) {
  apiErrorObserver = observer;
}

export function setApiRequestObserver(observer: ((payload: ApiRequestObserverPayload) => void) | null) {
  apiRequestObserver = observer;
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  const startedAt = performance.now();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`;
  }
  if (roleTierHeader) {
    headers['X-User-Role-Tier'] = roleTierHeader;
  }
  if (siteIdHeader) {
    headers['X-User-Site-Id'] = siteIdHeader;
  }
  if (userIdHeader) {
    headers['X-User-Id'] = userIdHeader;
  }

  let response: Response;
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    const elapsedMs = Math.round(performance.now() - startedAt);
    apiRequestObserver?.({
      method,
      path,
      status: 0,
      elapsedMs,
      ok: false,
      networkError: true,
    });
    apiErrorObserver?.({
      method,
      path,
      message: 'Network error',
      networkError: true,
    });
    throw { message: 'Network error' } as ApiError;
  }

  if (!response.ok) {
    const elapsedMs = Math.round(performance.now() - startedAt);
    apiRequestObserver?.({
      method,
      path,
      status: response.status,
      elapsedMs,
      ok: false,
    });
    let error: ApiError;
    try {
      const body = await response.json();
      error = typeof body === 'string'
        ? { message: body }
        : {
            message: body.message ?? body.title ?? `Request failed with status ${response.status}`,
            code: body.code ?? body.status?.toString(),
          };
    } catch {
      error = { message: `Request failed with status ${response.status}` };
    }
    apiErrorObserver?.({
      method,
      path,
      status: response.status,
      message: error.message,
      code: error.code,
    });
    throw error;
  }

  const elapsedMs = Math.round(performance.now() - startedAt);
  apiRequestObserver?.({
    method,
    path,
    status: response.status,
    elapsedMs,
    ok: true,
  });
  if (response.status === 204) return undefined as T;
  return response.json();
}

async function requestText(path: string): Promise<string> {
  const startedAt = performance.now();
  const headers: Record<string, string> = {};
  if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`;
  }

  const response = await fetch(`${BASE_URL}${path}`, { method: 'GET', headers });

  if (!response.ok) {
    const elapsedMs = Math.round(performance.now() - startedAt);
    apiRequestObserver?.({
      method: 'GET',
      path,
      status: response.status,
      elapsedMs,
      ok: false,
    });
    apiErrorObserver?.({
      method: 'GET',
      path,
      status: response.status,
      message: `Request failed with status ${response.status}`,
    });
    throw { message: `Request failed with status ${response.status}` } as ApiError;
  }

  const elapsedMs = Math.round(performance.now() - startedAt);
  apiRequestObserver?.({
    method: 'GET',
    path,
    status: response.status,
    elapsedMs,
    ok: true,
  });
  return response.text();
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T>(path: string) => request<T>('DELETE', path),
  getText: (path: string) => requestText(path),
};
