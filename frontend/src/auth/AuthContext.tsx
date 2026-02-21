import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { setAuthToken, setRoleTier, setSiteId } from '../api/apiClient.ts';

const SESSION_KEY = 'mes_auth';

export interface AuthUser {
  id: string;
  employeeNumber: string;
  displayName: string;
  roleTier: number;
  roleName: string;
  defaultSiteId: string;
  isCertifiedWelder: boolean;
  userType: number;
  plantCode: string;
  plantName: string;
  plantTimeZoneId: string;
}

interface AuthState {
  user: AuthUser | null;
  token: string | null;
  isWelder: boolean;
}

interface AuthContextValue extends AuthState {
  login: (token: string, user: AuthUser, isWelder: boolean) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

function loadPersistedState(): AuthState {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as AuthState;
      if (parsed.token) {
        setAuthToken(parsed.token);
        setRoleTier(parsed.user?.roleTier ?? null);
        setSiteId(parsed.user?.defaultSiteId ?? null);
        return parsed;
      }
    }
  } catch {
    sessionStorage.removeItem(SESSION_KEY);
  }
  return { user: null, token: null, isWelder: false };
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(loadPersistedState);

  const login = useCallback((token: string, user: AuthUser, isWelder: boolean) => {
    const next: AuthState = { user, token, isWelder };
    setState(next);
    setAuthToken(token);
    setRoleTier(user.roleTier);
    setSiteId(user.defaultSiteId);
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(next));
  }, []);

  const logout = useCallback(() => {
    setState({ user: null, token: null, isWelder: false });
    setAuthToken(null);
    setRoleTier(null);
    setSiteId(null);
    sessionStorage.removeItem(SESSION_KEY);
  }, []);

  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    isAuthenticated: state.token !== null,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
