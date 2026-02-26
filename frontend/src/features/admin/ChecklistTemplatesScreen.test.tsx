import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ChecklistTemplatesScreen } from './ChecklistTemplatesScreen.tsx';
import { checklistApi, siteApi, workCenterApi } from '../../api/endpoints.ts';

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  checklistApi: {
    getTemplates: vi.fn(),
    upsertTemplate: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
  workCenterApi: {
    getWorkCenters: vi.fn(),
  },
  productionLineApi: {
    getProductionLines: vi.fn().mockResolvedValue([]),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <ChecklistTemplatesScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

describe('ChecklistTemplatesScreen', () => {
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
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([
      { id: 'wc1', name: 'Rolls', workCenterTypeId: 't1', workCenterTypeName: 'Weld', numberOfWelders: 1 },
    ]);
    vi.mocked(checklistApi.getTemplates).mockResolvedValue([
      {
        id: 'ct1',
        templateCode: 'SAFE-1',
        title: 'Safety Pre-shift',
        checklistType: 'SafetyPreShift',
        scopeLevel: 'PlantWorkCenter',
        siteId: 's1',
        workCenterId: 'wc1',
        versionNo: 1,
        effectiveFromUtc: '2026-02-24T00:00:00Z',
        isActive: true,
        responseMode: 'PF',
        requireFailNote: true,
        isSafetyProfile: true,
        items: [{ id: 'i1', sortOrder: 1, prompt: 'Guard in place?', isRequired: true, requireFailNote: true, responseType: 'PassFail', responseOptions: [] }],
      },
    ] as any);
    vi.mocked(checklistApi.upsertTemplate).mockResolvedValue({} as any);
  });

  it('renders checklist templates from API', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument();
    });
    expect(screen.getByText('SAFE-1 v1')).toBeInTheDocument();
    expect(screen.getByText('Fail Note Required')).toBeInTheDocument();
  });

  it('adds a question and immediately opens the question editor', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Add Template' }));
    expect(screen.queryByText('Question 1')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Add Question' }));

    await waitFor(() => {
      expect(screen.getByText('Question 1')).toBeInTheDocument();
      expect(screen.getByText('Edit Question 1')).toBeInTheDocument();
    });
  });

  it('shows imported questions in the list immediately', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Add Template' }));
    const allTextboxes = screen.getAllByRole('textbox');
    const topTextInputs = allTextboxes.filter((el) => el.tagName === 'INPUT');
    fireEvent.change(topTextInputs[0], { target: { value: 'Ops checks' } });
    fireEvent.change(topTextInputs[1], { target: { value: 'OPS-100' } });

    fireEvent.click(screen.getByRole('button', { name: 'Import as PassFail Questions' }));
    await waitFor(() => {
      expect(screen.getByText('Import PassFail Questions')).toBeInTheDocument();
    });
    const textareasAfterOpen = screen.getAllByRole('textbox').filter((el) => el.tagName === 'TEXTAREA');
    fireEvent.change(textareasAfterOpen[textareasAfterOpen.length - 1], { target: { value: 'First check\nSecond check' } });
    fireEvent.click(screen.getByRole('button', { name: 'Import Questions' }));

    await waitFor(() => {
      expect(screen.getByText('Imported 2 questions.')).toBeInTheDocument();
      expect(screen.getByText('First check')).toBeInTheDocument();
      expect(screen.getByText('Second check')).toBeInTheDocument();
    });
  });

  it('imports prompt lines as pass/fail items and sends typed payload', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Add Template' }));
    const allTextboxes = screen.getAllByRole('textbox');
    const topTextInputs = allTextboxes.filter((el) => el.tagName === 'INPUT');
    fireEvent.change(topTextInputs[0], { target: { value: 'Ops checks' } });
    fireEvent.change(topTextInputs[1], { target: { value: 'OPS-100' } });

    fireEvent.click(screen.getByRole('button', { name: 'Import as PassFail Questions' }));
    await waitFor(() => {
      expect(screen.getByText('Import PassFail Questions')).toBeInTheDocument();
    });
    const textareasAfterOpen = screen.getAllByRole('textbox').filter((el) => el.tagName === 'TEXTAREA');
    fireEvent.change(textareasAfterOpen[textareasAfterOpen.length - 1], { target: { value: 'First check\nSecond check' } });
    fireEvent.click(screen.getByRole('button', { name: 'Import Questions' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => {
      expect(checklistApi.upsertTemplate).toHaveBeenCalled();
    });

    const payload = vi.mocked(checklistApi.upsertTemplate).mock.calls[0][0];
    expect(payload.items).toHaveLength(2);
    expect(payload.items[0]).toMatchObject({
      prompt: 'First check',
      responseType: 'PassFail',
      responseOptions: [],
    });
    expect(payload.items[1]).toMatchObject({
      prompt: 'Second check',
      responseType: 'PassFail',
      responseOptions: [],
    });
  }, 15000);
});
