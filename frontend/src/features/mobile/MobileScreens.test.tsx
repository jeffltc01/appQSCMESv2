import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MobileSupervisorHubScreen } from './MobileScreens.tsx';

const logoutMock = vi.fn();
const mockUseAuth = vi.fn();

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

describe('MobileScreens', () => {
  it('allows mobile users to logout from header', async () => {
    mockUseAuth.mockReturnValue({
      user: { plantName: 'Test Plant', plantCode: 'TP1' },
      logout: logoutMock,
    });

    const user = userEvent.setup();
    render(
      <FluentProvider theme={webLightTheme}>
        <MemoryRouter initialEntries={['/mobile/supervisor-hub']}>
          <Routes>
            <Route path="/mobile/supervisor-hub" element={<MobileSupervisorHubScreen />} />
            <Route path="/login" element={<div data-testid="login-route">Login</div>} />
          </Routes>
        </MemoryRouter>
      </FluentProvider>,
    );

    await user.click(screen.getByRole('button', { name: /logout/i }));

    expect(logoutMock).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('login-route')).toBeInTheDocument();
  });
});
