const OPERATOR_ROLE_TIER = 6;

export function isOperatorKioskRole(roleTier?: number | null): boolean {
  if (typeof roleTier !== 'number') return false;
  return roleTier >= OPERATOR_ROLE_TIER;
}

export function hasCachedWorkCenter(): boolean {
  try {
    return Boolean(localStorage.getItem('cachedWorkCenterId'));
  } catch {
    // Some managed/locked-down browsers can throw on Storage access.
    return false;
  }
}
