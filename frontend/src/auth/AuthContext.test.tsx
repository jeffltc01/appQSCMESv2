import { act, render, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { useEffect } from 'react';
import { AuthProvider, useAuth, type AuthUser } from './AuthContext.tsx';
import { buildAuthHeaders, setAuthToken, setRoleTier, setSiteId, setUserId } from '../api/apiClient.ts';

function AuthProbe({ onReady }: { onReady: (value: ReturnType<typeof useAuth>) => void }) {
  const auth = useAuth();
  useEffect(() => {
    onReady(auth);
  }, [auth, onReady]);
  return null;
}

describe('AuthContext site session behavior', () => {
  beforeEach(() => {
    sessionStorage.clear();
    setAuthToken(null);
    setRoleTier(null);
    setSiteId(null);
    setUserId(null);
  });

  it('uses session site id for API headers after login', async () => {
    let ctx: ReturnType<typeof useAuth> | null = null;
    render(
      <AuthProvider>
        <AuthProbe onReady={(value) => { ctx = value; }} />
      </AuthProvider>,
    );

    await waitFor(() => expect(ctx).not.toBeNull());

    const sessionSiteId = '22222222-2222-2222-2222-222222222222';
    const user: AuthUser = {
      id: 'user-1',
      employeeNumber: 'EMP001',
      displayName: 'Director User',
      roleTier: 2,
      roleName: 'Quality Director',
      defaultSiteId: sessionSiteId,
      isCertifiedWelder: false,
      userType: 0,
      plantCode: '600',
      plantName: 'Fremont',
      plantTimeZoneId: 'America/New_York',
    };

    act(() => {
      ctx!.login('token-123', user, false);
    });

    const headers = buildAuthHeaders();
    expect(headers['Authorization']).toBe('Bearer token-123');
    expect(headers['X-User-Site-Id']).toBe(sessionSiteId);

    const persisted = sessionStorage.getItem('mes_auth');
    expect(persisted).not.toBeNull();
    const parsed = JSON.parse(persisted!) as { user: { defaultSiteId: string } };
    expect(parsed.user.defaultSiteId).toBe(sessionSiteId);
  });

  it('hydrates site header from persisted session site id', async () => {
    const persistedSiteId = '33333333-3333-3333-3333-333333333333';
    sessionStorage.setItem(
      'mes_auth',
      JSON.stringify({
        token: 'persisted-token',
        isWelder: false,
        user: {
          id: 'user-2',
          employeeNumber: 'EMP777',
          displayName: 'Persisted User',
          roleTier: 3,
          roleName: 'Quality Manager',
          defaultSiteId: persistedSiteId,
          isCertifiedWelder: false,
          userType: 0,
          plantCode: '700',
          plantName: 'West Jordan',
          plantTimeZoneId: 'America/Denver',
        },
      }),
    );

    render(
      <AuthProvider>
        <div>session</div>
      </AuthProvider>,
    );

    await waitFor(() => {
      const headers = buildAuthHeaders();
      expect(headers['Authorization']).toBe('Bearer persisted-token');
      expect(headers['X-User-Site-Id']).toBe(persistedSiteId);
    });
  });
});
