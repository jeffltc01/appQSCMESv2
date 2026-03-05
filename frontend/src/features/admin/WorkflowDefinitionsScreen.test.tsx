import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkflowDefinitionsScreen } from './WorkflowDefinitionsScreen.tsx';
import { workflowApi } from '../../api/endpoints.ts';

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
  workflowApi: {
    getDefinitions: vi.fn(),
    upsertDefinition: vi.fn(),
    validateDefinition: vi.fn(),
  },
  nlqApi: {
    ask: vi.fn(),
  },
}));

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <BrowserRouter>
        <WorkflowDefinitionsScreen />
      </BrowserRouter>
    </FluentProvider>,
  );
}

async function ensurePickerOpen(user: ReturnType<typeof userEvent.setup>) {
  if (screen.queryByRole('button', { name: 'Edit As New Version' })) {
    return;
  }
  await user.click(screen.getByRole('button', { name: 'Select Workflow' }));
  await waitFor(() => expect(screen.getByText('HoldTag v3')).toBeInTheDocument(), { timeout: 5000 });
}

async function clickPickerAction(
  user: ReturnType<typeof userEvent.setup>,
  actionName: 'Edit As New Version' | 'Clone As Draft',
) {
  for (let attempt = 0; attempt < 3; attempt += 1) {
    await ensurePickerOpen(user);
    const action = screen.queryByRole('button', { name: actionName });
    if (action) {
      await user.click(action);
      return;
    }
    await user.click(screen.getByRole('button', { name: 'Select Workflow' }));
  }
  throw new Error(`Unable to locate picker action: ${actionName}`);
}

const seededDefinition = {
  id: '11111111-1111-1111-1111-111111111111',
  workflowType: 'HoldTag',
  version: 3,
  isActive: true,
  startStepCode: 'TagCreated',
  steps: [
    {
      id: 'a',
      stepCode: 'TagCreated',
      stepName: 'Tag Created',
      sequence: 1,
      requiredFields: [],
      requiredChecklistTemplateIds: [],
      approvalMode: 'None' as const,
      approvalAssignments: [],
      allowReject: false,
    },
    {
      id: 'b',
      stepCode: 'QualityReview',
      stepName: 'Quality Review',
      sequence: 2,
      requiredFields: [],
      requiredChecklistTemplateIds: [],
      approvalMode: 'AnyOne' as const,
      approvalAssignments: ['role:2'],
      allowReject: true,
      onApproveNextStepCode: 'Complete',
    },
  ],
};

const seededNcrDefinition = {
  id: '22222222-2222-2222-2222-222222222222',
  workflowType: 'Ncr',
  version: 1,
  isActive: true,
  startStepCode: 'NcrOpen',
  steps: [
    {
      id: 'n1',
      stepCode: 'NcrOpen',
      stepName: 'NCR Opened',
      sequence: 1,
      requiredFields: [],
      requiredChecklistTemplateIds: [],
      approvalMode: 'None' as const,
      approvalAssignments: [],
      allowReject: false,
    },
  ],
};

describe('WorkflowDefinitionsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockReset();
    mockUseAuth.mockReturnValue({
      user: {
        id: 'user-1',
        displayName: 'Director',
        roleTier: 2,
        plantCode: 'PLT1',
        plantName: 'Plant 1',
      },
      logout: vi.fn(),
    });
    vi.mocked(workflowApi.getDefinitions).mockImplementation(async (type?: string) => (
      type === 'Ncr' ? [seededNcrDefinition] : [seededDefinition]
    ));
    vi.mocked(workflowApi.upsertDefinition).mockResolvedValue(seededDefinition);
    vi.mocked(workflowApi.validateDefinition).mockResolvedValue({ isExecutable: true, errors: [] });
  });

  it('loads selected definition into editor for new version and clone draft', async () => {
    const user = userEvent.setup();
    renderScreen();

    await ensurePickerOpen(user);
    await waitFor(() => expect(screen.getByText('HoldTag v3')).toBeInTheDocument());

    await clickPickerAction(user, 'Edit As New Version');
    expect(screen.getAllByDisplayValue('TagCreated').length).toBeGreaterThan(0);
    const activeSwitch = screen.getByLabelText('Active') as HTMLInputElement;
    expect(activeSwitch.checked).toBe(true);

    await ensurePickerOpen(user);
    await clickPickerAction(user, 'Clone As Draft');
    expect(activeSwitch.checked).toBe(false);
  });

  it('reorders steps and saves a new version payload', async () => {
    const user = userEvent.setup();
    renderScreen();

    await ensurePickerOpen(user);
    await waitFor(() => expect(screen.getByText('HoldTag v3')).toBeInTheDocument());
    await clickPickerAction(user, 'Edit As New Version');

    const moveDownButtons = screen.getAllByRole('button', { name: 'Move Down' });
    await user.click(moveDownButtons[0]);
    await user.click(screen.getByRole('button', { name: 'Save New Version' }));

    await waitFor(() => expect(workflowApi.upsertDefinition).toHaveBeenCalledTimes(1));
    const payload = vi.mocked(workflowApi.upsertDefinition).mock.calls[0][0];
    expect(payload.sourceDefinitionIdForNewVersion).toBe(seededDefinition.id);
    expect(payload.steps[0].stepCode).toBe('QualityReview');
    expect(payload.steps[0].sequence).toBe(1);
    expect(payload.steps[1].stepCode).toBe('TagCreated');
    expect(payload.steps[1].sequence).toBe(2);
  });

  it('close on picker exits workflow definitions screen', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Select Workflow Definition' })).toBeInTheDocument());
    await user.click(screen.getByRole('button', { name: 'Close' }));

    expect(mockNavigate).toHaveBeenCalledWith('/menu');
  });

});
