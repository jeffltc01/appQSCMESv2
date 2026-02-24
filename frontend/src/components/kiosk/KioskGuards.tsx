import { useAuth } from '../../auth/AuthContext.tsx';
import { isOperatorKioskRole } from '../../auth/kioskPolicy.ts';
import { useKioskGuards } from '../../hooks/useKioskGuards.ts';

export function KioskGuards() {
  const { user, isAuthenticated } = useAuth();
  const enabled = isAuthenticated && isOperatorKioskRole(user?.roleTier);
  useKioskGuards(enabled);
  return null;
}
