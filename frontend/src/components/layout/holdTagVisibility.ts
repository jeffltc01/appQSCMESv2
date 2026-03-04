export function canShowCreateHoldTagButton(
  lineEnabled: boolean,
  hasWorkCenterContext: boolean,
  workCenterEnabled: boolean,
): boolean {
  if (!hasWorkCenterContext) {
    return lineEnabled;
  }

  return lineEnabled && workCenterEnabled;
}
