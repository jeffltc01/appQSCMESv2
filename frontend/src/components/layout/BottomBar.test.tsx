import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { BottomBar } from './BottomBar';

vi.mock('../../hooks/useHealthCheck.ts', () => ({
  useHealthCheck: vi.fn(() => 'online'),
}));

import { useHealthCheck } from '../../hooks/useHealthCheck.ts';
const mockUseHealthCheck = vi.mocked(useHealthCheck);

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

  it('shows Online when health check succeeds', () => {
    mockUseHealthCheck.mockReturnValue('online');
    renderBottomBar();
    expect(screen.getByText('Online')).toBeInTheDocument();
  });

  it('shows Offline when health check fails', () => {
    mockUseHealthCheck.mockReturnValue('offline');
    renderBottomBar();
    expect(screen.getByText('Offline')).toBeInTheDocument();
  });

  it('shows Checking… on initial load', () => {
    mockUseHealthCheck.mockReturnValue('checking');
    renderBottomBar();
    expect(screen.getByText('Checking…')).toBeInTheDocument();
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

  it('shows green scanner dot when externalInput is on and scannerReady is true', () => {
    renderBottomBar({ externalInput: true, scannerReady: true });
    const dot = document.querySelector('[title="Scanner ready"]');
    expect(dot).toBeInTheDocument();
  });

  it('shows red scanner dot when externalInput is on and scannerReady is false', () => {
    renderBottomBar({ externalInput: true, scannerReady: false });
    const dot = document.querySelector('[title="Scanner inactive"]');
    expect(dot).toBeInTheDocument();
  });

  it('does not show scanner dot when externalInput is off', () => {
    renderBottomBar({ externalInput: false, scannerReady: false });
    const readyDot = document.querySelector('[title="Scanner ready"]');
    const lostDot = document.querySelector('[title="Scanner inactive"]');
    expect(readyDot).not.toBeInTheDocument();
    expect(lostDot).not.toBeInTheDocument();
  });
});
