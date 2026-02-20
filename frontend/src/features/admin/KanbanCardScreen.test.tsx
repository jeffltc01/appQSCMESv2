import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { KanbanCardScreen } from './KanbanCardScreen.tsx';
import { adminKanbanCardApi } from '../../api/endpoints.ts';

vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => ({
    user: { plantCode: 'PLT1', plantName: 'Cleveland', displayName: 'Test Admin' },
    logout: vi.fn(),
  }),
}));

vi.mock('../../api/endpoints.ts', () => ({
  adminKanbanCardApi: {
    getAll: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <KanbanCardScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

const mockBarcodeCards = [
  {
    id: '1',
    cardValue: '01',
    color: 'Red',
    description: 'Card 1',
  },
];

describe('KanbanCardScreen', () => {
  beforeEach(() => {
    vi.mocked(adminKanbanCardApi.getAll).mockResolvedValue(mockBarcodeCards);
  });

  it('renders loading state initially', async () => {
    let resolveGetAll!: (v: typeof mockBarcodeCards) => void;
    vi.mocked(adminKanbanCardApi.getAll).mockImplementation(
      () => new Promise((r) => { resolveGetAll = r; }),
    );
    renderScreen();
    expect(screen.getByText('Loading...')).toBeInTheDocument();
    resolveGetAll(mockBarcodeCards);
    await waitFor(() =>
      expect(screen.queryByText('Loading...')).not.toBeInTheDocument(),
    );
  });

  it('renders cards after API resolves', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText(/Card 01/)).toBeInTheDocument();
    });
    expect(screen.getByText('Red')).toBeInTheDocument();
    expect(screen.getByText('Card 1')).toBeInTheDocument();
  });

  it('shows empty state when no items', async () => {
    vi.mocked(adminKanbanCardApi.getAll).mockResolvedValue([]);
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('No kanban cards found.')).toBeInTheDocument();
    });
  });

  it('shows Add Card button', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Add Card/i })).toBeInTheDocument();
    });
  });

  it('displays correct title', async () => {
    renderScreen();
    expect(screen.getByText('Kanban Card Management')).toBeInTheDocument();
  });
});
