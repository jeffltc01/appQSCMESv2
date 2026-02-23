import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout';

vi.mock('../../auth/AuthContext', () => ({
  useAuth: () => ({
    user: {
      id: 'u-1',
      employeeNumber: 'EMP001',
      displayName: 'Team Lead',
      roleTier: 5,
      roleName: 'Team Lead',
      defaultSiteId: '11111111-1111-1111-1111-111111111111',
      isCertifiedWelder: false,
      userType: 0,
      plantCode: '000',
      plantName: 'Cleveland',
      plantTimeZoneId: 'America/Chicago',
    },
    logout: vi.fn(),
  }),
}));

vi.mock('../../help/components/HelpButton.tsx', () => ({
  HelpButton: () => <button type="button">Help</button>,
}));

vi.mock('../../help/useCurrentHelpArticle.ts', () => ({
  useCurrentHelpArticle: () => null,
}));

vi.mock('../../api/endpoints.ts', () => ({
  nlqApi: {
    ask: vi.fn().mockResolvedValue({
      answerText: 'Plant has produced 42 tanks today.',
      scopeUsed: 'plant-wide',
      confidence: 0.9,
      dataPoints: [{ label: 'Plant total today', value: '42', unit: 'tanks' }],
      followUps: [],
      trace: { intent: 'TanksProducedToday', usedModel: true, usedCache: false, durationMs: 12 },
    }),
  },
}));

const { nlqApi } = await import('../../api/endpoints.ts');

function renderLayout() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <AdminLayout title="Admin Test">
          <div>Child content</div>
        </AdminLayout>
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('AdminLayout Ask MES', () => {
  beforeEach(() => vi.clearAllMocks());

  it('submits question and displays answer', async () => {
    const user = userEvent.setup();
    renderLayout();

    const input = screen.getByLabelText('Ask MES');
    await user.type(input, 'how many tanks today?');
    await user.click(screen.getByRole('button', { name: 'Ask' }));

    await waitFor(() => {
      expect(nlqApi.ask).toHaveBeenCalledTimes(1);
      expect(screen.getByText('Plant has produced 42 tanks today.')).toBeInTheDocument();
      expect(screen.getByText(/Plant total today: 42/)).toBeInTheDocument();
    });
  });
});
