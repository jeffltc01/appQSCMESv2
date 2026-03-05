import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { MemoryRouter } from 'react-router-dom';
import { HeijunkaPlannerScreen } from './HeijunkaPlannerScreen';

vi.mock('../../api/endpoints');
const mockUseAuth = vi.fn();
vi.mock('../../auth/AuthContext', () => ({ useAuth: () => mockUseAuth() }));

const { heijunkaApi, productionLineApi, shiftScheduleApi, workCenterApi } = await import('../../api/endpoints');

function renderScreen() {
  return render(
    <FluentProvider theme={webLightTheme}>
      <MemoryRouter>
        <HeijunkaPlannerScreen />
      </MemoryRouter>
    </FluentProvider>,
  );
}

describe('HeijunkaPlannerScreen', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    mockUseAuth.mockReturnValue({
      user: {
        id: 'u1',
        displayName: 'Planner User',
        roleTier: 5,
        roleName: 'Team Lead',
        defaultSiteId: 'site-1',
        employeeNumber: 'EMP001',
        plantCode: '000',
        plantName: 'Cleveland',
        plantTimeZoneId: 'America/Chicago',
        isCertifiedWelder: false,
        userType: 0,
      },
    });
    vi.mocked(heijunkaApi.getExceptions).mockResolvedValue([]);
    vi.mocked(heijunkaApi.getRiskSummary).mockResolvedValue({
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09',
      openUnmappedExceptions: 0,
      loadGroupsDue: 2,
      loadGroupsPlanned: 2,
      hasDispatchRisk: false,
    });
    vi.mocked(heijunkaApi.getPhase1Kpis).mockResolvedValue({
      siteCode: '000',
      productionLineId: 'line-1',
      fromDateLocal: '2026-03-09',
      toDateLocal: '2026-03-15',
      isEligible: true,
      scheduleAdherencePercent: { value: 95 },
      planAttainmentPercent: { value: 96 },
      loadReadinessPercent: { value: 100 },
      supermarketStockoutDurationMinutes: { value: 0 },
    });
    vi.mocked(heijunkaApi.getDispatchWeekOrders).mockResolvedValue([]);
    vi.mocked(heijunkaApi.getSupermarketQuantities).mockResolvedValue([]);
    vi.mocked(heijunkaApi.getWorkCenterBreakdownConfigs).mockResolvedValue([]);
    vi.mocked(heijunkaApi.getWorkCenterBreakdown).mockResolvedValue({
      scheduleId: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      workCenterId: 'wc-1',
      workCenterName: 'Hydro',
      weekStartDateLocal: '2026-03-09',
      groupingDimensions: [],
      rows: [],
    });
    vi.mocked(heijunkaApi.getChangeHistory).mockResolvedValue([]);
    vi.mocked(shiftScheduleApi.getAll).mockResolvedValue([]);
    vi.mocked(workCenterApi.getWorkCenters).mockResolvedValue([]);
    vi.mocked(heijunkaApi.moveLine).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [],
    });
    vi.mocked(productionLineApi.getProductionLines).mockResolvedValue([
      { id: 'line-1', name: 'Line 1', plantId: 'site-1' },
    ]);
  });

  it('generates draft and renders schedule lines', async () => {
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [
        {
          id: 'line-1',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 1,
          planningClass: 'Wheel',
          plannedQty: 4,
          loadGroupId: 'LOAD-100',
          mesPlanningGroupId: 'PG-100',
        },
      ],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());

    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    await waitFor(() => {
      expect(heijunkaApi.generateDraft).toHaveBeenCalled();
      expect(screen.getAllByText('PG-100').length).toBeGreaterThan(0);
      expect(screen.getByText('Draft')).toBeInTheDocument();
      expect(screen.getByText('Weekly Calendar')).toBeInTheDocument();
      expect(screen.getByText('Qty 4')).toBeInTheDocument();
    });
  });

  it('calendar drag and drop moves line to target day and priority', async () => {
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [
        {
          id: 'line-a',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 1,
          planningClass: 'Wheel',
          plannedQty: 4,
          loadGroupId: 'LOAD-A',
          mesPlanningGroupId: 'PG-A',
        },
        {
          id: 'line-b',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 2,
          planningClass: 'Wheel',
          plannedQty: 5,
          loadGroupId: 'LOAD-B',
          mesPlanningGroupId: 'PG-B',
        },
      ],
    });
    vi.mocked(heijunkaApi.moveLine).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    await waitFor(() => {
      expect(screen.getByTestId('calendar-item-line-a')).toBeInTheDocument();
      expect(screen.getByTestId('calendar-item-line-b')).toBeInTheDocument();
    });

    const topItem = screen.getByTestId('calendar-item-line-a');
    const bottomItem = screen.getByTestId('calendar-item-line-b');

    fireEvent.dragStart(bottomItem);
    fireEvent.dragOver(topItem);
    fireEvent.drop(topItem);

    await waitFor(() => {
      expect(heijunkaApi.moveLine).toHaveBeenCalledWith({
        scheduleId: 'sch-1',
        scheduleLineId: 'line-b',
        newPlannedDateLocal: '2026-03-10',
        newSequenceIndex: 1,
        changeReasonCode: 'CalendarDragDrop',
      });
    });
  });

  it('opens freeze override modal from weekly calendar card icon', async () => {
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Published',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [
        {
          id: 'line-a',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 1,
          planningClass: 'Wheel',
          plannedQty: 4,
          loadGroupId: 'LOAD-A',
          mesPlanningGroupId: 'PG-A',
        },
      ],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    const freezeButton = await screen.findByLabelText('Freeze override PG-A');
    await waitFor(() => expect(freezeButton).toBeEnabled());
    fireEvent.click(freezeButton);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Apply Override' })).toBeInTheDocument();
    });
  });

  it('calendar drag and drop to empty day moves line date', async () => {
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [
        {
          id: 'line-a',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 1,
          planningClass: 'Wheel',
          plannedQty: 4,
          loadGroupId: 'LOAD-A',
          mesPlanningGroupId: 'PG-A',
        },
      ],
    });
    vi.mocked(heijunkaApi.moveLine).mockResolvedValue({
      id: 'sch-1',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    await waitFor(() => {
      expect(screen.getByTestId('calendar-item-line-a')).toBeInTheDocument();
      expect(screen.getByTestId('calendar-day-2026-03-12')).toBeInTheDocument();
    });

    const item = screen.getByTestId('calendar-item-line-a');
    const emptyDay = screen.getByTestId('calendar-day-2026-03-12');

    fireEvent.dragStart(item);
    fireEvent.dragOver(emptyDay);
    fireEvent.drop(emptyDay);

    await waitFor(() => {
      expect(heijunkaApi.moveLine).toHaveBeenCalledWith(
        expect.objectContaining({
          scheduleId: 'sch-1',
          scheduleLineId: 'line-a',
          newPlannedDateLocal: '2026-03-12',
          changeReasonCode: 'CalendarDragDrop',
        }),
      );
    });
  });

  it('groups calendar cards by plan group per day', async () => {
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-grouped',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [
        {
          id: 'line-a',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 1,
          planningClass: 'Wheel',
          plannedQty: 3,
          loadGroupId: 'LOAD-A',
          mesPlanningGroupId: 'PG-500',
        },
        {
          id: 'line-b',
          plannedDateLocal: '2026-03-10T00:00:00',
          sequenceIndex: 2,
          planningClass: 'Wheel',
          plannedQty: 4,
          loadGroupId: 'LOAD-B',
          mesPlanningGroupId: 'PG-500',
        },
      ],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    await waitFor(() => {
      expect(screen.getByTestId('calendar-item-line-a')).toBeInTheDocument();
      expect(screen.queryByTestId('calendar-item-line-b')).not.toBeInTheDocument();
      expect(screen.getByText('Qty 7')).toBeInTheDocument();
      expect(screen.getByText((content) => content.includes('LOAD-A') && content.includes('LOAD-B'))).toBeInTheDocument();
    });
  });

  it('loads line dropdown from default plant and defaults selection', async () => {
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-ctx',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    await waitFor(() => {
      expect(productionLineApi.getProductionLines).toHaveBeenCalledWith('site-1');
      expect(heijunkaApi.generateDraft).toHaveBeenCalledWith(
        expect.objectContaining({ productionLineId: 'line-1' }),
      );
    });
  });

  it('hides non-working weekend days by default but force-shows demand days and supports toggle', async () => {
    vi.mocked(heijunkaApi.getDispatchWeekOrders).mockResolvedValue([
      {
        siteCode: '000',
        productionLineId: 'line-1',
        weekStartDateLocal: '2026-03-09',
        loadGroupId: 'LOAD-SUN',
        dispatchDateLocal: '2026-03-15T00:00:00',
        erpSalesOrderId: 'SO-SUN',
        erpSalesOrderLineId: '1',
        erpSkuCode: 'SKU-SUN',
        mesPlanningGroupId: 'PG-SUN',
        requiredQty: 1,
        loadGroupRequiredQty: 1,
        loadGroupPlannedQty: 0,
        isMapped: true,
        loadGroupCovered: false,
      },
    ]);
    vi.mocked(heijunkaApi.generateDraft).mockResolvedValue({
      id: 'sch-week',
      siteCode: '000',
      productionLineId: 'line-1',
      weekStartDateLocal: '2026-03-09T00:00:00',
      status: 'Draft',
      freezeHours: 24,
      revisionNumber: 1,
      lines: [],
    });

    const user = userEvent.setup();
    renderScreen();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Generate Draft' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Generate Draft' }));

    await waitFor(() => {
      expect(screen.queryByTestId('calendar-day-2026-03-14')).not.toBeInTheDocument();
      expect(screen.getByTestId('calendar-day-2026-03-15')).toBeInTheDocument();
    });

    await user.click(screen.getByLabelText('Show Non-Working Days'));

    await waitFor(() => {
      expect(screen.getByTestId('calendar-day-2026-03-14')).toBeInTheDocument();
      expect(screen.getByTestId('calendar-day-2026-03-15')).toBeInTheDocument();
    });
  });
});
