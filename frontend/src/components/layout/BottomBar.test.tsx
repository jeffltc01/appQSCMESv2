import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { BottomBar } from './BottomBar';

function renderBottomBar(overrides = {}) {
  const defaults = {
    plantCode: 'PLT1',
    externalInput: false,
    onToggleExternalInput: vi.fn(),
    ...overrides,
  };
  return render(
    <FluentProvider theme={webLightTheme}>
      <BottomBar {...defaults} />
    </FluentProvider>,
  );
}

describe('BottomBar', () => {
  it('displays plant code and time', () => {
    renderBottomBar();
    expect(screen.getByText(/PLT1 -/)).toBeInTheDocument();
  });

  it('shows online status', () => {
    renderBottomBar();
    expect(screen.getByText('Online')).toBeInTheDocument();
  });

  it('displays External Input toggle', () => {
    renderBottomBar();
    expect(screen.getByText('External Input')).toBeInTheDocument();
  });

  it('calls toggle handler when switch is clicked', async () => {
    const user = userEvent.setup();
    const onToggle = vi.fn();
    renderBottomBar({ onToggleExternalInput: onToggle });

    const toggle = screen.getByRole('switch');
    await user.click(toggle);
    expect(onToggle).toHaveBeenCalledTimes(1);
  });
});
