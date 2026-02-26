import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ChecklistTemplateEditorScreen } from './ChecklistTemplateEditorScreen.tsx';
import { adminUserApi, checklistApi, productionLineApi, siteApi, workCenterApi } from '../../api/endpoints.ts';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext.tsx', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../../api/endpoints.ts', () => ({
  checklistApi: {
    getTemplate: vi.fn(),
    upsertTemplate: vi.fn(),
    getScoreTypes: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
  workCenterApi: {
    getWorkCenters: vi.fn(),
  },
  productionLineApi: {
    getProductionLines: vi.fn(),
  },
  adminUserApi: {
    getAll: vi.fn(),
  },
}));

function renderEditor(path: string) {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/menu/checklists/new" element={<ChecklistTemplateEditorScreen />} />
          <Route path="/menu/checklists/:templateId" element={<ChecklistTemplateEditorScreen />} />
        </Routes>
      </MemoryRouter>
    </FluentProvider>,
  );
}

function toLocalInputValue(isoLike: string): string {
  const date = new Date(isoLike);
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

describe('ChecklistTemplateEditorScreen', () => {
  beforeEach(() => {
    mockNavigate.mockReset();
    mockUseAuth.mockReturnValue({
      user: {
        id: 'u1',
        employeeNumber: 'EMP001',
        displayName: 'Admin User',
        roleTier: 1,
        defaultSiteId: 's1',
        plantCode: '000',
        plantName: 'Cleveland',
      },
    });
    vi.mocked(siteApi.getSites).mockResolvedValue([{ id: 's1', code: '000', name: 'Cleveland', timeZoneId: 'America/Chicago' }] as any);
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([{ id: 'wc1', name: 'Rolls' }] as any);
    vi.mocked(productionLineApi.getProductionLines).mockResolvedValue([{ id: 'pl1', name: 'Line 1', plantId: 's1' }] as any);
    vi.mocked(adminUserApi.getAll).mockResolvedValue([{ id: 'u1', employeeNumber: 'EMP001', displayName: 'Admin User' }] as any);
    vi.mocked(checklistApi.getScoreTypes).mockResolvedValue([{ id: 'st1', name: 'Quality', values: [] }] as any);
    vi.mocked(checklistApi.upsertTemplate).mockResolvedValue({} as any);
    vi.mocked(checklistApi.getTemplate).mockResolvedValue({
      id: 'ct1',
      templateCode: 'SAFE-1',
      title: 'Safety Template',
      checklistType: 'SafetyPreShift',
      scopeLevel: 'PlantWorkCenter',
      siteId: 's1',
      workCenterId: 'wc1',
      versionNo: 1,
      effectiveFromUtc: '2026-02-24T00:00:00Z',
      isActive: true,
      responseMode: 'PF',
      requireFailNote: true,
      isSafetyProfile: false,
      ownerUserId: 'u1',
      items: [
        { id: 'i1', sortOrder: 1, prompt: 'Question A', isRequired: true, section: 'Zeta', responseType: 'Checkbox', requireFailNote: false },
        { id: 'i2', sortOrder: 2, prompt: 'Question B', isRequired: true, section: 'Alpha', responseType: 'Checkbox', requireFailNote: false },
      ],
    } as any);
  });

  it('section selector shows existing sections and create-new option', async () => {
    renderEditor('/menu/checklists/new');
    await waitFor(() => expect(screen.getByText('Create template')).toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: 'Add Question' }));

    const sectionDropdown = document.querySelector('button[role="combobox"][value="Unsectioned"]') as HTMLButtonElement | null;
    expect(sectionDropdown).not.toBeNull();
    fireEvent.click(sectionDropdown as HTMLButtonElement);
    expect(screen.getByText('+ Create new section')).toBeInTheDocument();
  });

  it('shows score type selector for score response type', async () => {
    renderEditor('/menu/checklists/new');
    await waitFor(() => expect(screen.getByText('Create template')).toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: 'Add Question' }));

    const responseTypeDropdown = document.querySelector('button[role="combobox"][value="Checkbox"]') as HTMLButtonElement | null;
    if (!responseTypeDropdown) throw new Error('Response type dropdown not found');
    fireEvent.click(responseTypeDropdown);
    fireEvent.click(await screen.findByText('Score'));
    expect(await screen.findByText('Score Type')).toBeInTheDocument();
  });

  it('shows dimension metadata fields for dimension response type', async () => {
    renderEditor('/menu/checklists/new');
    await waitFor(() => expect(screen.getByText('Create template')).toBeInTheDocument());
    fireEvent.click(screen.getByRole('button', { name: 'Add Question' }));

    const responseTypeDropdown = document.querySelector('button[role="combobox"][value="Checkbox"]') as HTMLButtonElement | null;
    if (!responseTypeDropdown) throw new Error('Response type dropdown not found');
    fireEvent.click(responseTypeDropdown);
    fireEvent.click(await screen.findByText('Dimension'));
    expect(await screen.findByText('Target')).toBeInTheDocument();
    expect(screen.getByText('Upper Limit')).toBeInTheDocument();
    expect(screen.getByText('Lower Limit')).toBeInTheDocument();
    expect(screen.getByText('Unit of Measure')).toBeInTheDocument();
  });

  it('validates owner is required before save', async () => {
    mockUseAuth.mockReturnValue({
      user: {
        id: '',
        employeeNumber: 'EMP001',
        displayName: 'Admin User',
        roleTier: 1,
        defaultSiteId: 's1',
        plantCode: '000',
        plantName: 'Cleveland',
      },
    });
    renderEditor('/menu/checklists/new');
    await waitFor(() => expect(screen.getByText('Create template')).toBeInTheDocument());

    const textInputs = screen.getAllByRole('textbox');
    fireEvent.change(textInputs[0], { target: { value: 'Template A' } });
    fireEvent.change(textInputs[1], { target: { value: 'TEMP-A' } });

    fireEvent.click(screen.getByRole('button', { name: 'Save Template' }));
    expect(await screen.findByText('Template owner is required.')).toBeInTheDocument();
  });

  it('renders effective-from in local datetime format for existing templates', async () => {
    vi.mocked(checklistApi.getTemplate).mockResolvedValueOnce({
      id: 'ct2',
      templateCode: 'SAFE-2',
      title: 'Safety Offset',
      checklistType: 'SafetyPreShift',
      scopeLevel: 'GlobalDefault',
      versionNo: 1,
      effectiveFromUtc: '2026-02-24T00:00:00-05:00',
      isActive: true,
      responseMode: 'PF',
      requireFailNote: false,
      isSafetyProfile: false,
      ownerUserId: 'u1',
      items: [{ id: 'i1', sortOrder: 1, prompt: 'Question A', isRequired: true, responseType: 'Checkbox', requireFailNote: false }],
    } as any);

    renderEditor('/menu/checklists/ct2');
    await waitFor(() => expect(screen.getByText('Editing Safety Offset')).toBeInTheDocument());

    const dateInputs = document.querySelectorAll('input[type="datetime-local"]');
    expect(dateInputs.length).toBeGreaterThan(0);
    expect((dateInputs[0] as HTMLInputElement).value).toBe(toLocalInputValue('2026-02-24T00:00:00-05:00'));
  });

  it('converts local datetime input to UTC ISO when saving', async () => {
    renderEditor('/menu/checklists/new');
    await waitFor(() => expect(screen.getByText('Create template')).toBeInTheDocument());

    const textInputs = screen.getAllByRole('textbox');
    fireEvent.change(textInputs[0], { target: { value: 'Template Date Test' } });
    fireEvent.change(textInputs[1], { target: { value: 'TEMP-DATE' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Question' }));
    fireEvent.change(screen.getByPlaceholderText('New section name'), { target: { value: '' } });
    const prompts = document.querySelectorAll('textarea');
    fireEvent.change(prompts[0] as HTMLTextAreaElement, { target: { value: 'Prompt A' } });

    const expectedLocal = '2026-02-24T10:30';
    const dateInputs = document.querySelectorAll('input[type="datetime-local"]');
    fireEvent.change(dateInputs[0] as HTMLInputElement, { target: { value: expectedLocal } });

    fireEvent.click(screen.getByRole('button', { name: 'Save Template' }));

    await waitFor(() => expect(checklistApi.upsertTemplate).toHaveBeenCalled());
    const payload = vi.mocked(checklistApi.upsertTemplate).mock.calls.at(-1)?.[0] as { effectiveFromUtc: string };
    expect(payload.effectiveFromUtc).toBe(new Date(expectedLocal).toISOString());
  });
});
