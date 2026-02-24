import type { AuthUser } from './AuthContext.tsx';

const PHONE_MAX_WIDTH = 768;

function normalizeRoleName(roleName?: string): string {
  return (roleName ?? '').trim().toLowerCase();
}

export function isPhoneViewport(width: number): boolean {
  return width <= PHONE_MAX_WIDTH;
}

export function isQualityDirector(user: AuthUser | null | undefined): boolean {
  const role = normalizeRoleName(user?.roleName);
  return role.includes('quality director');
}

export function isOpsDirector(user: AuthUser | null | undefined): boolean {
  const role = normalizeRoleName(user?.roleName);
  return role.includes('ops director') || role.includes('operations director');
}

export function isDirectorRole(user: AuthUser | null | undefined): boolean {
  return isQualityDirector(user) || isOpsDirector(user);
}

export function canUseMobileSupervisorHub(user: AuthUser | null | undefined): boolean {
  const roleTier = user?.roleTier ?? 99;
  return roleTier <= 5.5;
}
