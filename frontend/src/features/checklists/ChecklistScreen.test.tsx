import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ChecklistScreen } from './ChecklistScreen.tsx';
import { checklistApi } from '../../api/endpoints.ts';

vi.mock('../../api/endpoints.ts', () => ({
  checklistApi: {
    resolveTemplate: vi.fn(),
    createEntry: vi.fn(),
    submitResponses: vi.fn(),
    completeEntry: vi.fn(),
  },
}));

describe('ChecklistScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(checklistApi.resolveTemplate).mockResolvedValue({
      id: 't1',
      templateCode: 'SAFE-1',
      title: 'Safety Checklist',
      checklistType: 'SafetyPreShift',
      scopeLevel: 'PlantWorkCenter',
      versionNo: 1,
      effectiveFromUtc: '2026-02-24T00:00:00Z',
      isActive: true,
      responseMode: 'PF',
      requireFailNote: false,
      isSafetyProfile: false,
      ownerUserId: 'u1',
      items: [
        { id: 'i1', sortOrder: 1, prompt: 'No section item', isRequired: true, responseType: 'Checkbox', requireFailNote: false },
        { id: 'i2', sortOrder: 2, prompt: 'Zeta item', isRequired: true, section: 'Zeta', responseType: 'Checkbox', requireFailNote: false },
        { id: 'i3', sortOrder: 3, prompt: 'Alpha item', isRequired: true, section: 'Alpha', responseType: 'Checkbox', requireFailNote: false },
      ],
    } as any);
    vi.mocked(checklistApi.createEntry).mockResolvedValue({
      id: 'e1',
      checklistTemplateId: 't1',
      checklistType: 'SafetyPreShift',
      siteId: 's1',
      workCenterId: 'wc1',
      operatorUserId: 'u1',
      status: 'InProgress',
      startedAtUtc: '2026-02-24T00:00:00Z',
      resolvedFromScope: 'PlantWorkCenter',
      resolvedTemplateCode: 'SAFE-1',
      resolvedTemplateVersionNo: 1,
      responses: [],
    } as any);
  });

  it('renders section groups in alphabetical order with unsectioned first', async () => {
    render(
      <FluentProvider theme={webLightTheme}>
        <ChecklistScreen
          workCenterId="wc1"
          assetId="a1"
          productionLineId="pl1"
          operatorId="u1"
          plantId="s1"
          welders={[]}
          numberOfWelders={0}
          welderCountLoaded
          externalInput={false}
          setExternalInput={() => undefined}
          showScanResult={() => undefined}
          refreshHistory={() => undefined}
          registerBarcodeHandler={() => undefined}
        />
      </FluentProvider>,
    );

    await waitFor(() => expect(screen.getByText('Safety Checklist')).toBeInTheDocument());
    const headings = screen.getAllByRole('heading', { level: 3 }).map((h) => h.textContent);
    expect(headings).toEqual(['Unsectioned', 'Alpha', 'Zeta']);
  });

  it('shows actionable guidance when no template resolves for selected checklist type', async () => {
    vi.mocked(checklistApi.resolveTemplate).mockRejectedValue(new Error('404 Not Found'));

    render(
      <FluentProvider theme={webLightTheme}>
        <ChecklistScreen
          workCenterId="wc1"
          assetId="a1"
          productionLineId="pl1"
          operatorId="u1"
          plantId="s1"
          welders={[]}
          numberOfWelders={0}
          welderCountLoaded
          externalInput={false}
          setExternalInput={() => undefined}
          showScanResult={() => undefined}
          refreshHistory={() => undefined}
          registerBarcodeHandler={() => undefined}
        />
      </FluentProvider>,
    );

    expect(await screen.findByText(/No active SafetyPreShift template is effective/i)).toBeInTheDocument();
    expect(checklistApi.createEntry).not.toHaveBeenCalled();
  });

  it('renders score options as buttons instead of dropdown', async () => {
    vi.mocked(checklistApi.resolveTemplate).mockResolvedValueOnce({
      id: 't2',
      templateCode: 'SAFE-SCORE',
      title: 'Safety Score Checklist',
      checklistType: 'SafetyPreShift',
      scopeLevel: 'GlobalDefault',
      versionNo: 1,
      effectiveFromUtc: '2026-02-24T00:00:00Z',
      isActive: true,
      responseMode: 'PF',
      requireFailNote: false,
      isSafetyProfile: false,
      ownerUserId: 'u1',
      items: [
        {
          id: 's1',
          sortOrder: 1,
          prompt: 'Are all safety devices operational?',
          isRequired: true,
          responseType: 'Score',
          requireFailNote: false,
          scoreOptions: [
            { id: 'sv1', score: 1, description: 'No' },
            { id: 'sv2', score: 2, description: 'Yes' },
          ],
        },
      ],
    } as any);

    render(
      <FluentProvider theme={webLightTheme}>
        <ChecklistScreen
          workCenterId="wc1"
          assetId="a1"
          productionLineId="pl1"
          operatorId="u1"
          plantId="s1"
          welders={[]}
          numberOfWelders={0}
          welderCountLoaded
          externalInput={false}
          setExternalInput={() => undefined}
          showScanResult={() => undefined}
          refreshHistory={() => undefined}
          registerBarcodeHandler={() => undefined}
        />
      </FluentProvider>,
    );

    await waitFor(() => expect(screen.getByText('Safety Score Checklist')).toBeInTheDocument());
    expect(screen.getByRole('button', { name: 'No' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Yes' })).toBeInTheDocument();
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Yes' }));
    fireEvent.click(screen.getByRole('button', { name: 'Submit Checklist' }));
    await waitFor(() => expect(checklistApi.submitResponses).toHaveBeenCalled());
  });
});
