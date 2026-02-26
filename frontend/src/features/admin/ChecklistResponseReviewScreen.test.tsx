import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ChecklistResponseReviewScreen } from './ChecklistResponseReviewScreen.tsx';
import { checklistApi, siteApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  checklistApi: {
    getReviewSummary: vi.fn(),
    getQuestionResponses: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <ChecklistResponseReviewScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('ChecklistResponseReviewScreen', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({
      user: {
        id: 'u1',
        roleTier: 1,
        defaultSiteId: 's1',
        displayName: 'Admin',
        plantCode: '000',
        plantName: 'Cleveland',
      },
      logout: vi.fn(),
    });

    vi.mocked(siteApi.getSites).mockResolvedValue([
      { id: 's1', code: '000', name: 'Cleveland', timeZoneId: 'America/Chicago' },
    ]);

    vi.mocked(checklistApi.getReviewSummary).mockResolvedValue({
      siteId: 's1',
      fromUtc: '2026-02-01T00:00:00.000Z',
      toUtc: '2026-02-15T23:59:59.999Z',
      totalEntries: 2,
      totalResponses: 4,
      checklistTypesFound: ['SafetyPreShift', 'OpsPreShift'],
      checklistFiltersFound: [
        { checklistType: 'SafetyPreShift', checklistName: 'Safety Pre-Shift Checklist' },
        { checklistType: 'OpsPreShift', checklistName: 'Operations Pre-Shift Checklist' },
      ],
      questions: [
        {
          checklistTemplateItemId: 'q1',
          prompt: 'Guard in place?',
          responseType: 'Checkbox',
          responseCount: 2,
          responseBuckets: [
            { value: 'true', label: 'Pass', count: 1 },
            { value: 'false', label: 'Fail', count: 1 },
          ],
        },
        {
          checklistTemplateItemId: 'q2',
          prompt: 'PPE worn?',
          responseType: 'Checkbox',
          responseCount: 2,
          responseBuckets: [
            { value: 'true', label: 'Pass', count: 2 },
          ],
        },
      ],
    } as any);

    vi.mocked(checklistApi.getQuestionResponses).mockResolvedValue({
      checklistTemplateItemId: 'q1',
      prompt: 'Guard in place?',
      responseType: 'Checkbox',
      totalResponses: 2,
      responseBuckets: [
        { value: 'true', label: 'Pass', count: 1 },
        { value: 'false', label: 'Fail', count: 1 },
      ],
      rows: [
        {
          checklistEntryId: 'e1',
          checklistType: 'SafetyPreShift',
          operatorUserId: 'u1',
          operatorDisplayName: 'J. Smith',
          respondedAtUtc: '2026-02-10T10:00:00Z',
          responseValue: 'true',
          responseLabel: 'Pass',
          note: '',
        },
      ],
    } as any);
  });

  it('shows checklist chips when checklist dropdown is all', async () => {
    renderScreen();
    await waitFor(() => expect(screen.getByText('Question Results Summary')).toBeInTheDocument());
    expect(screen.getByText('Checklists Found:')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Safety Pre-Shift Checklist' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Operations Pre-Shift Checklist' })).toBeInTheDocument();
  });

  it('applies chip narrowing by requesting summary with selected checklist type', async () => {
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Operations Pre-Shift Checklist' })).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Operations Pre-Shift Checklist' }));

    await waitFor(() => {
      expect(checklistApi.getReviewSummary).toHaveBeenLastCalledWith(
        expect.objectContaining({ checklistType: 'OpsPreShift' }),
      );
    });
  });

  it('loads drill-down responses when selecting a question row', async () => {
    vi.mocked(checklistApi.getQuestionResponses).mockResolvedValueOnce({
      checklistTemplateItemId: 'q2',
      prompt: 'PPE worn?',
      responseType: 'Checkbox',
      totalResponses: 2,
      responseBuckets: [{ value: 'true', label: 'Pass', count: 2 }],
      rows: [],
    } as any);

    renderScreen();
    await waitFor(() => expect(screen.getByText('Guard in place?')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: /PPE worn\?/i }));

    await waitFor(() => {
      expect(checklistApi.getQuestionResponses).toHaveBeenLastCalledWith(
        expect.objectContaining({ checklistTemplateItemId: 'q2' }),
      );
    });
  });
});
