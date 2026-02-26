import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { ScoreTypesScreen } from './ScoreTypesScreen.tsx';
import { checklistApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  checklistApi: {
    getScoreTypes: vi.fn(),
    upsertScoreType: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <ScoreTypesScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('ScoreTypesScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: { roleTier: 1 } });
    vi.mocked(checklistApi.getScoreTypes).mockResolvedValue([
      {
        id: 'st1',
        name: 'Quality',
        isActive: true,
        values: [
          { id: 'v1', score: 1, description: 'Poor', sortOrder: 1 },
        ],
      },
    ] as any);
    vi.mocked(checklistApi.upsertScoreType).mockResolvedValue({} as any);
  });

  it('renders existing score types and archives an active type', async () => {
    renderScreen();
    await waitFor(() => expect(screen.getByText('Quality')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Archive' }));
    await waitFor(() => expect(checklistApi.upsertScoreType).toHaveBeenCalled());
  });

  it('creates a score type from modal form', async () => {
    renderScreen();
    await waitFor(() => expect(screen.getByText('Quality')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Add Score Type' }));
    const inputs = screen.getAllByRole('textbox');
    fireEvent.change(inputs[0], { target: { value: 'Weld Score' } });
    fireEvent.change(inputs[1], { target: { value: 'Good' } });
    fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '5' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add', hidden: true }));

    await waitFor(() => expect(checklistApi.upsertScoreType).toHaveBeenCalled());
  });
});
