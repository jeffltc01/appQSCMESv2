import type { ApiError } from '../types/api.ts';

const BASE_URL = import.meta.env.VITE_API_URL ?? '/api';

let authToken: string | null = null;
let roleTierHeader: string | null = null;
let siteIdHeader: string | null = null;

export function setAuthToken(token: string | null) {
  authToken = token;
}

export function setRoleTier(tier: number | null) {
  roleTierHeader = tier != null ? String(tier) : null;
}

export function setSiteId(siteId: string | null) {
  siteIdHeader = siteId ?? null;
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
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

  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
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
    throw error;
  }

  if (response.status === 204) return undefined as T;
  return response.json();
}

async function requestText(path: string): Promise<string> {
  const headers: Record<string, string> = {};
  if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`;
  }

  const response = await fetch(`${BASE_URL}${path}`, { method: 'GET', headers });

  if (!response.ok) {
    throw { message: `Request failed with status ${response.status}` } as ApiError;
  }

  return response.text();
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T>(path: string) => request<T>('DELETE', path),
  getText: (path: string) => requestText(path),
};
