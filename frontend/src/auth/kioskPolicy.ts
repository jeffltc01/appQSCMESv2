const OPERATOR_ROLE_TIER = 6;

export function isOperatorKioskRole(roleTier?: number | null): boolean {
  if (typeof roleTier !== 'number') return false;
  return roleTier >= OPERATOR_ROLE_TIER;
}

export function hasCachedWorkCenter(): boolean {
  return Boolean(localStorage.getItem('cachedWorkCenterId'));
}
