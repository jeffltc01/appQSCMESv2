import { describe, expect, it } from 'vitest';
import { canShowCreateHoldTagButton } from './holdTagVisibility.ts';

describe('canShowCreateHoldTagButton', () => {
  it('requires line enabled when line-only context', () => {
    expect(canShowCreateHoldTagButton(true, false, false)).toBe(true);
    expect(canShowCreateHoldTagButton(false, false, true)).toBe(false);
  });

  it('requires both line and workcenter enabled when workcenter context exists', () => {
    expect(canShowCreateHoldTagButton(true, true, true)).toBe(true);
    expect(canShowCreateHoldTagButton(true, true, false)).toBe(false);
    expect(canShowCreateHoldTagButton(false, true, true)).toBe(false);
  });
});
