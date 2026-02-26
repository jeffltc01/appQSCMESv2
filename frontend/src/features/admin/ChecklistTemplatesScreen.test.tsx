import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ChecklistTemplatesScreen } from './ChecklistTemplatesScreen.tsx';
import { checklistApi, siteApi, workCenterApi } from '../../api/endpoints.ts';

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
    getTemplates: vi.fn(),
  },
  siteApi: {
    getSites: vi.fn(),
  },
  workCenterApi: {
    getWorkCenters: vi.fn(),
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
    mockNavigate.mockReset();
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
        ownerUserId: 'u1',
        items: [{ id: 'i1', sortOrder: 1, prompt: 'Guard in place?', isRequired: true, requireFailNote: true, responseType: 'Checkbox', responseOptions: [] }],
      },
    ] as any);
  });

  it('navigates to full-screen editor route when adding template', async () => {
    renderScreen();
    await waitFor(() => expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Add Template' }));
    expect(mockNavigate).toHaveBeenCalledWith('/menu/checklists/new');
  });

  it('navigates to full-screen editor route when editing template', async () => {
    renderScreen();
    await waitFor(() => expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument());

    fireEvent.click(screen.getByRole('button', { name: 'Edit Safety Pre-shift' }));

    expect(mockNavigate).toHaveBeenCalledWith('/menu/checklists/ct1');
  });

  it('renders checklist templates from API', async () => {
    renderScreen();
    await waitFor(() => {
      expect(screen.getByText('Safety Pre-shift')).toBeInTheDocument();
    });
    expect(screen.getByText('SAFE-1 v1')).toBeInTheDocument();
    expect(screen.getByText('Fail Note Required')).toBeInTheDocument();
  });
});
