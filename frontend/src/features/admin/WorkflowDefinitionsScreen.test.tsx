import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkflowDefinitionsScreen } from './WorkflowDefinitionsScreen.tsx';
import { workflowApi } from '../../api/endpoints.ts';

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

describe('WorkflowDefinitionsScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
    vi.mocked(workflowApi.getDefinitions).mockResolvedValue([seededDefinition]);
    vi.mocked(workflowApi.upsertDefinition).mockResolvedValue(seededDefinition);
    vi.mocked(workflowApi.validateDefinition).mockResolvedValue({ isExecutable: true, errors: [] });
  });

  it('loads selected definition into editor for new version and clone draft', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => expect(screen.getByText('HoldTag v3')).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: 'Edit As New Version' }));
    expect(screen.getAllByDisplayValue('TagCreated').length).toBeGreaterThan(0);
    const activeSwitch = screen.getByLabelText('Active') as HTMLInputElement;
    expect(activeSwitch.checked).toBe(true);

    await user.click(screen.getByRole('button', { name: 'Clone As Draft' }));
    expect(activeSwitch.checked).toBe(false);
  });

  it('reorders steps and saves a new version payload', async () => {
    const user = userEvent.setup();
    renderScreen();

    await waitFor(() => expect(screen.getByText('HoldTag v3')).toBeInTheDocument());
    await user.click(screen.getByRole('button', { name: 'Edit As New Version' }));

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
});
