import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';

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

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({
    user: null,
    token: null,
    isWelder: false,
  });

  const login = useCallback((token: string, user: AuthUser, isWelder: boolean) => {
    setState({ user, token, isWelder });
  }, []);

  const logout = useCallback(() => {
    setState({ user: null, token: null, isWelder: false });
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
