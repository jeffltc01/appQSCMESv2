import { useEffect, useState } from 'react';
import { Button, Dropdown, Input, Option, Spinner } from '@fluentui/react-components';
import { LockOpenRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { heijunkaApi, productionLineApi, shiftScheduleApi } from '../../api/endpoints.ts';
import type {
  DispatchWeekOrderCoverage,
  DispatchRiskSummary,
  HeijunkaPhase1Kpis,
  HeijunkaScheduleChangeLog,
  HeijunkaSchedule,
  ProductionLine,
  ShiftSchedule,
  SupermarketQuantityStatus,
  UnmappedDemandException,
} from '../../types/domain.ts';
import styles from './HeijunkaPlannerScreen.module.css';

function getWeekStart(input: Date): string {
  const date = new Date(input);
  const day = date.getDay();
  const diff = date.getDate() - day + (day === 0 ? -6 : 1);
  date.setDate(diff);
  return date.toISOString().slice(0, 10);
}

function getWeekDates(weekStartIso: string): string[] {
  const start = new Date(`${weekStartIso}T00:00:00`);
  return Array.from({ length: 7 }, (_, offset) => {
    const day = new Date(start);
    day.setDate(start.getDate() + offset);
    return day.toISOString().slice(0, 10);
  });
}

export function HeijunkaPlannerScreen() {
  const { user } = useAuth();
  const [weekStartDateLocal, setWeekStartDateLocal] = useState(getWeekStart(new Date()));
  const [planningResourceId, setPlanningResourceId] = useState('paint-final-scan');
  const [freezeHours, setFreezeHours] = useState(24);
  const [loading, setLoading] = useState(false);
  const [schedule, setSchedule] = useState<HeijunkaSchedule | null>(null);
  const [productionLines, setProductionLines] = useState<ProductionLine[]>([]);
  const [selectedProductionLineId, setSelectedProductionLineId] = useState('');
  const [exceptions, setExceptions] = useState<UnmappedDemandException[]>([]);
  const [risk, setRisk] = useState<DispatchRiskSummary | null>(null);
  const [kpis, setKpis] = useState<HeijunkaPhase1Kpis | null>(null);
  const [dispatchWeekOrders, setDispatchWeekOrders] = useState<DispatchWeekOrderCoverage[]>([]);
  const [supermarketQuantities, setSupermarketQuantities] = useState<SupermarketQuantityStatus[]>([]);
  const [shiftSchedules, setShiftSchedules] = useState<ShiftSchedule[]>([]);
  const [showNonWorkingDays, setShowNonWorkingDays] = useState(false);
  const [changeHistory, setChangeHistory] = useState<HeijunkaScheduleChangeLog[]>([]);
  const [error, setError] = useState('');
  const [selectedLineId, setSelectedLineId] = useState('');
  const [overrideModalOpen, setOverrideModalOpen] = useState(false);
  const [overrideDate, setOverrideDate] = useState('');
  const [overrideQty, setOverrideQty] = useState('');
  const [overrideReason, setOverrideReason] = useState('DemandShock');
  const [dragLineId, setDragLineId] = useState<string | null>(null);
  const [dragOverLineId, setDragOverLineId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'plan' | 'analytics'>('plan');

  const canEdit = (user?.roleTier ?? 99) <= 5;
  const canFreezeOverride = !!schedule && canEdit && (schedule.status === 'Published' || schedule.status === 'InExecution');
  const siteCode = user?.plantCode ?? '';
  const loadExceptions = async () => {
    if (!siteCode) return;
    const data = await heijunkaApi.getExceptions(siteCode);
    setExceptions(data);
  };

  const refreshRiskAndKpis = async (scheduleData: HeijunkaSchedule) => {
    const weekStart = scheduleData.weekStartDateLocal.slice(0, 10);
    const [riskData, kpiData, dispatchOrdersData, supermarketData] = await Promise.all([
      heijunkaApi.getRiskSummary(scheduleData.siteCode, scheduleData.productionLineId, weekStart),
      heijunkaApi.getPhase1Kpis(
        scheduleData.siteCode,
        scheduleData.productionLineId,
        weekStart,
        new Date(new Date(weekStart).getTime() + 6 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
      ),
      heijunkaApi.getDispatchWeekOrders(scheduleData.siteCode, scheduleData.productionLineId, weekStart, scheduleData.id),
      heijunkaApi.getSupermarketQuantities(scheduleData.siteCode, scheduleData.productionLineId, weekStart),
    ]);
    setRisk(riskData);
    setKpis(kpiData);
    setDispatchWeekOrders(dispatchOrdersData);
    setSupermarketQuantities(supermarketData);
  };

  const refreshChangeHistory = async (scheduleData: HeijunkaSchedule) => {
    const history = await heijunkaApi.getChangeHistory(scheduleData.id);
    setChangeHistory(history);
  };

  useEffect(() => {
    loadExceptions().catch(() => setExceptions([]));
  }, [siteCode]);

  useEffect(() => {
    if (!user?.defaultSiteId) {
      setProductionLines([]);
      setSelectedProductionLineId('');
      setShiftSchedules([]);
      return;
    }

    let isCancelled = false;
    const loadPlannerContext = async () => {
      try {
        const lines = await productionLineApi.getProductionLines(user.defaultSiteId);
        if (isCancelled) return;
        setProductionLines(lines);
        setSelectedProductionLineId((current) => {
          if (current && lines.some((line) => line.id === current)) return current;
          return lines[0]?.id ?? '';
        });
      } catch {
        if (isCancelled) return;
        setProductionLines([]);
        setSelectedProductionLineId('');
      }

      try {
        const schedules = await shiftScheduleApi.getAll(user.defaultSiteId);
        if (isCancelled) return;
        setShiftSchedules(schedules);
      } catch {
        if (isCancelled) return;
        setShiftSchedules([]);
      }
    };

    loadPlannerContext().catch(() => undefined);
    return () => {
      isCancelled = true;
    };
  }, [user?.defaultSiteId]);

  const handleGenerateDraft = async () => {
    if (!canEdit || !siteCode || !selectedProductionLineId) {
      setError('Planner context missing. Configure plant and production line first.');
      return;
    }
    setError('');
    setLoading(true);
    try {
      const created = await heijunkaApi.generateDraft({
        siteCode,
        productionLineId: selectedProductionLineId,
        weekStartDateLocal,
        freezeHours,
        planningResourceId,
      });
      setSchedule(created);
      await loadExceptions();
      await Promise.all([refreshRiskAndKpis(created), refreshChangeHistory(created)]);
    } catch (err: any) {
      setError(err?.message ?? 'Failed to generate schedule draft.');
    } finally {
      setLoading(false);
    }
  };

  const handlePublish = async () => {
    if (!schedule) return;
    setError('');
    setLoading(true);
    try {
      const published = await heijunkaApi.publish(schedule.id);
      setSchedule(published);
      await loadExceptions();
      await Promise.all([refreshRiskAndKpis(published), refreshChangeHistory(published)]);
    } catch (err: any) {
      setError(err?.message ?? 'Publish failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleResolveException = async (exceptionId: string) => {
    if (!canEdit) return;
    setLoading(true);
    setError('');
    try {
      await heijunkaApi.resolveException({
        exceptionId,
        action: 'Resolve',
        resolutionNotes: 'Resolved by planner from Heijunka board.',
      });
      await loadExceptions();
    } catch (err: any) {
      setError(err?.message ?? 'Unable to resolve exception.');
    } finally {
      setLoading(false);
    }
  };

  const openOverrideModal = (lineId: string, plannedDateLocal: string, plannedQty: number) => {
    setSelectedLineId(lineId);
    setOverrideDate(plannedDateLocal.slice(0, 10));
    setOverrideQty(String(plannedQty));
    setOverrideReason('DemandShock');
    setOverrideModalOpen(true);
  };

  const handleFreezeOverride = async () => {
    if (!schedule || !selectedLineId || !overrideDate || !overrideReason) return;
    setLoading(true);
    setError('');
    try {
      const updated = await heijunkaApi.freezeOverride({
        scheduleId: schedule.id,
        scheduleLineId: selectedLineId,
        newPlannedDateLocal: overrideDate,
        newPlannedQty: overrideQty ? Number(overrideQty) : undefined,
        changeReasonCode: overrideReason,
      });
      setSchedule(updated);
      setOverrideModalOpen(false);
      await Promise.all([refreshRiskAndKpis(updated), refreshChangeHistory(updated)]);
    } catch (err: any) {
      setError(err?.message ?? 'Freeze override failed.');
    } finally {
      setLoading(false);
    }
  };

  const orderedLines = schedule?.lines
    .slice()
    .sort((a, b) => (a.sequenceIndex ?? 9999) - (b.sequenceIndex ?? 9999)) ?? [];
  const calendarWeekStart = schedule?.weekStartDateLocal.slice(0, 10) ?? weekStartDateLocal;
  const weekDates = getWeekDates(calendarWeekStart);
  const effectiveShiftSchedule = shiftSchedules
    .slice()
    .sort((a, b) => b.effectiveDate.localeCompare(a.effectiveDate))
    .find((item) => item.effectiveDate <= calendarWeekStart);

  const getScheduledHoursForDay = (dayDate: Date): number | null => {
    if (!effectiveShiftSchedule) return null;
    switch (dayDate.getDay()) {
      case 0:
        return effectiveShiftSchedule.sundayHours;
      case 1:
        return effectiveShiftSchedule.mondayHours;
      case 2:
        return effectiveShiftSchedule.tuesdayHours;
      case 3:
        return effectiveShiftSchedule.wednesdayHours;
      case 4:
        return effectiveShiftSchedule.thursdayHours;
      case 5:
        return effectiveShiftSchedule.fridayHours;
      case 6:
        return effectiveShiftSchedule.saturdayHours;
      default:
        return 0;
    }
  };

  const isWorkingDay = (dateKey: string): boolean => {
    const dayDate = new Date(`${dateKey}T00:00:00`);
    const scheduledHours = getScheduledHoursForDay(dayDate);
    if (scheduledHours != null) {
      return scheduledHours > 0;
    }

    const dayOfWeek = dayDate.getDay();
    return dayOfWeek >= 1 && dayOfWeek <= 5;
  };

  const linesByDate = orderedLines.reduce<Record<string, typeof orderedLines>>((acc, line) => {
    const key = line.plannedDateLocal.slice(0, 10);
    if (!acc[key]) acc[key] = [];
    acc[key].push(line);
    return acc;
  }, {});

  const dispatchOrdersByDate = dispatchWeekOrders.reduce<Record<string, number>>((acc, item) => {
    const key = item.dispatchDateLocal.slice(0, 10);
    acc[key] = (acc[key] ?? 0) + 1;
    return acc;
  }, {});

  const exceptionsByDate = exceptions.reduce<Record<string, number>>((acc, item) => {
    const key = item.dispatchDateLocal.slice(0, 10);
    acc[key] = (acc[key] ?? 0) + 1;
    return acc;
  }, {});

  const visibleWeekDates = weekDates.filter((dateKey) => {
    if (showNonWorkingDays) return true;
    const hasPlannedLines = (linesByDate[dateKey]?.length ?? 0) > 0;
    const hasDispatchDemand = (dispatchOrdersByDate[dateKey] ?? 0) > 0;
    const hasOpenExceptions = (exceptionsByDate[dateKey] ?? 0) > 0;
    return isWorkingDay(dateKey) || hasPlannedLines || hasDispatchDemand || hasOpenExceptions;
  });

  const calendarDatesToRender = visibleWeekDates.length > 0 ? visibleWeekDates : weekDates;

  const handleCalendarDropMove = async (targetLineId: string) => {
    if (!dragLineId || !schedule || dragLineId === targetLineId) return;
    const targetLine = orderedLines.find((line) => line.id === targetLineId);
    if (!targetLine) return;
    setLoading(true);
    setError('');
    try {
      const updated = await heijunkaApi.moveLine({
        scheduleId: schedule.id,
        scheduleLineId: dragLineId,
        newPlannedDateLocal: targetLine.plannedDateLocal.slice(0, 10),
        newSequenceIndex: targetLine.sequenceIndex ?? 1,
        changeReasonCode: 'CalendarDragDrop',
      });
      setSchedule(updated);
      await refreshChangeHistory(updated);
    } catch (err: any) {
      setError(err?.message ?? 'Unable to move schedule line.');
    } finally {
      setLoading(false);
      setDragLineId(null);
      setDragOverLineId(null);
    }
  };

  const handleCalendarDropToDay = async (targetDateLocal: string) => {
    if (!dragLineId || !schedule) return;
    setLoading(true);
    setError('');
    try {
      const updated = await heijunkaApi.moveLine({
        scheduleId: schedule.id,
        scheduleLineId: dragLineId,
        newPlannedDateLocal: targetDateLocal,
        changeReasonCode: 'CalendarDragDrop',
      });
      setSchedule(updated);
      await refreshChangeHistory(updated);
    } catch (err: any) {
      setError(err?.message ?? 'Unable to move schedule line.');
    } finally {
      setLoading(false);
      setDragLineId(null);
      setDragOverLineId(null);
    }
  };

  return (
    <AdminLayout title="Heijunka Planner" showAskMes={false}>
      <div className={styles.container}>
        <section className={styles.controls}>
          <label>
            Week Start
            <Input type="date" value={weekStartDateLocal} onChange={(_, d) => setWeekStartDateLocal(d.value)} />
          </label>
          <label>
            Production Line
            <Dropdown
              value={productionLines.find((line) => line.id === selectedProductionLineId)?.name ?? ''}
              selectedOptions={selectedProductionLineId ? [selectedProductionLineId] : []}
              onOptionSelect={(_, data) => setSelectedProductionLineId(data.optionValue ?? '')}
            >
              {productionLines.map((line) => (
                <Option key={line.id} value={line.id}>
                  {line.name}
                </Option>
              ))}
            </Dropdown>
          </label>
          <label>
            Freeze Hours
            <Input type="number" value={String(freezeHours)} onChange={(_, d) => setFreezeHours(Number(d.value || 24))} />
          </label>
          <label>
            Planning Resource
            <Input value={planningResourceId} onChange={(_, d) => setPlanningResourceId(d.value)} />
          </label>
          <Button appearance="primary" onClick={handleGenerateDraft} disabled={loading || !canEdit || !selectedProductionLineId}>
            Generate Draft
          </Button>
          {schedule && (
            <Button appearance="secondary" onClick={handlePublish} disabled={loading || schedule.status !== 'Draft' || !canEdit}>
              Publish
            </Button>
          )}
        </section>
        <div className={styles.tabBar}>
          <Button appearance={activeTab === 'plan' ? 'primary' : 'secondary'} onClick={() => setActiveTab('plan')}>
            Plan Board
          </Button>
          <Button appearance={activeTab === 'analytics' ? 'primary' : 'secondary'} onClick={() => setActiveTab('analytics')}>
            Analytics &amp; Audit
          </Button>
        </div>

        {loading && <Spinner label="Loading..." />}
        {error && <div className={styles.error}>{error}</div>}

        {activeTab === 'plan' ? (
          <div className={styles.columns}>
            <div className={styles.leftColumn}>
              <section className={styles.card}>
                <h3>Unmapped Demand Exceptions</h3>
                {exceptions.length === 0 ? (
                  <p>No open exceptions for this site.</p>
                ) : (
                  <table className={styles.table}>
                    <thead>
                      <tr>
                        <th>SKU</th>
                        <th>Load Group</th>
                        <th>Dispatch Date</th>
                        <th>Qty</th>
                        <th>Status</th>
                        <th />
                      </tr>
                    </thead>
                    <tbody>
                      {exceptions.map((item) => (
                        <tr key={item.id}>
                          <td>{item.erpSkuCode}</td>
                          <td>{item.loadGroupId}</td>
                          <td>{item.dispatchDateLocal.slice(0, 10)}</td>
                          <td>{item.requiredQty}</td>
                          <td>{item.exceptionStatus}</td>
                          <td>
                            {item.exceptionStatus === 'Open' && (
                              <Button size="small" onClick={() => handleResolveException(item.id)} disabled={!canEdit || loading}>
                                Resolve
                              </Button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </section>

              <section className={styles.card}>
                <h3>Weekly Calendar</h3>
                {schedule && (
                  <p>
                    Status: <strong>{schedule.status}</strong> | Revision: <strong>{schedule.revisionNumber}</strong> | Lines:{' '}
                    <strong>{schedule.lines.length}</strong>
                  </p>
                )}
                <div className={styles.calendarToolbar}>
                  <label className={styles.checkboxLabel}>
                    <input
                      type="checkbox"
                      checked={showNonWorkingDays}
                      onChange={(event) => setShowNonWorkingDays(event.currentTarget.checked)}
                    />
                    Show Non-Working Days
                  </label>
                </div>
                {!schedule ? (
                  <p>Generate a schedule to view the weekly calendar.</p>
                ) : (
                  <div className={styles.calendarGrid}>
                    {calendarDatesToRender.map((dateKey) => {
                      const dayLines = linesByDate[dateKey] ?? [];
                      const dayLabel = new Date(`${dateKey}T00:00:00`).toLocaleDateString(undefined, { weekday: 'short' });
                      return (
                        <div
                          key={dateKey}
                          data-testid={`calendar-day-${dateKey}`}
                          className={styles.calendarDay}
                          onDragOver={(event) => {
                            event.preventDefault();
                          }}
                          onDrop={() => handleCalendarDropToDay(dateKey)}
                        >
                          <div className={styles.calendarDayHeader}>
                            <strong>{dayLabel}</strong>
                            <span>{dateKey}</span>
                          </div>
                          {dayLines.length === 0 ? (
                            <div className={styles.calendarEmpty}>No planned lines</div>
                          ) : (
                            dayLines.map((line) => (
                              <div
                                key={line.id}
                                data-testid={`calendar-item-${line.id}`}
                                className={`${styles.calendarItem} ${dragOverLineId === line.id ? styles.calendarDragOverItem : ''}`}
                                draggable={canEdit && schedule.status === 'Draft'}
                                onDragStart={() => setDragLineId(line.id)}
                                onDragOver={(event) => {
                                  event.preventDefault();
                                  setDragOverLineId(line.id);
                                }}
                                onDrop={(event) => {
                                  event.preventDefault();
                                  event.stopPropagation();
                                  handleCalendarDropMove(line.id);
                                }}
                                onDragEnd={() => {
                                  setDragLineId(null);
                                  setDragOverLineId(null);
                                }}
                              >
                                <div className={styles.calendarItemTopRow}>
                                  <div>{line.mesPlanningGroupId ?? '-'}</div>
                                  {canFreezeOverride && (
                                    <Button
                                      size="small"
                                      appearance="subtle"
                                      icon={<LockOpenRegular />}
                                      className={styles.calendarFreezeOverrideButton}
                                      aria-label={`Freeze override ${line.mesPlanningGroupId ?? line.id}`}
                                      title="Freeze Override"
                                      disabled={loading}
                                      onPointerDown={(event) => {
                                        event.stopPropagation();
                                      }}
                                      onClick={(event) => {
                                        event.preventDefault();
                                        event.stopPropagation();
                                        openOverrideModal(line.id, line.plannedDateLocal, line.plannedQty);
                                      }}
                                    />
                                  )}
                                </div>
                                <div>Seq {line.sequenceIndex ?? '-'} • Qty {line.plannedQty}</div>
                                <div>{line.loadGroupId ?? 'No load group'}</div>
                              </div>
                            ))
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </section>
            </div>

            <div className={styles.rightColumn}>
              <section className={styles.card}>
                <h3>Dispatch Week Orders</h3>
                {!schedule ? (
                  <p>Generate a schedule to view dispatch-week demand orders.</p>
                ) : dispatchWeekOrders.length === 0 ? (
                  <p>No dispatch-week orders found for this week.</p>
                ) : (
                  <table className={styles.table}>
                    <thead>
                      <tr>
                        <th>Dispatch Date</th>
                        <th>Load Group</th>
                        <th>Order</th>
                        <th>SKU</th>
                        <th>Mapped Group</th>
                        <th>Order Qty</th>
                        <th>Load Group Req</th>
                        <th>Load Group Plan</th>
                        <th>Coverage</th>
                      </tr>
                    </thead>
                    <tbody>
                      {dispatchWeekOrders.map((item, idx) => (
                        <tr key={`${item.erpSalesOrderId}-${item.erpSalesOrderLineId}-${item.loadGroupId}-${idx}`}>
                          <td>{item.dispatchDateLocal.slice(0, 10)}</td>
                          <td>{item.loadGroupId}</td>
                          <td>{item.erpSalesOrderId}-{item.erpSalesOrderLineId}</td>
                          <td>{item.erpSkuCode}</td>
                          <td>{item.mesPlanningGroupId ?? 'UNMAPPED'}</td>
                          <td>{item.requiredQty}</td>
                          <td>{item.loadGroupRequiredQty}</td>
                          <td>{item.loadGroupPlannedQty}</td>
                          <td className={item.loadGroupCovered ? styles.readinessPass : styles.readinessFail}>
                            {item.loadGroupCovered ? 'Covered' : 'Gap'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </section>

              <section className={styles.card}>
                <h3>Supermarket Quantities</h3>
                {!schedule ? (
                  <p>Generate a schedule to view supermarket quantities.</p>
                ) : supermarketQuantities.length === 0 ? (
                  <p>No supermarket quantity snapshots captured for this week.</p>
                ) : (
                  <table className={styles.table}>
                    <thead>
                      <tr>
                        <th>Product</th>
                        <th>On Hand</th>
                        <th>In Transit</th>
                        <th>Demand</th>
                        <th>Net Available</th>
                        <th>Stockout Minutes</th>
                        <th>Open Stockout</th>
                        <th>Last Captured (UTC)</th>
                      </tr>
                    </thead>
                    <tbody>
                      {supermarketQuantities.map((item, idx) => (
                        <tr key={`${item.productId ?? 'none'}-${idx}`}>
                          <td>{item.productId ?? 'Unspecified'}</td>
                          <td>{item.onHandQty}</td>
                          <td>{item.inTransitQty}</td>
                          <td>{item.demandQty}</td>
                          <td>{item.netAvailableQty}</td>
                          <td>{item.stockoutDurationMinutes}</td>
                          <td className={item.hasOpenStockout ? styles.readinessFail : styles.readinessPass}>
                            {item.hasOpenStockout ? 'Yes' : 'No'}
                          </td>
                          <td>{item.lastCapturedAtUtc.slice(0, 19).replace('T', ' ')}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </section>
            </div>
          </div>
        ) : (
          <div className={styles.analyticsColumn}>
            <section className={styles.card}>
              <h3>Schedule Change History</h3>
              {!schedule ? (
                <p>Generate a schedule to see audit history.</p>
              ) : changeHistory.length === 0 ? (
                <p>No changes captured yet.</p>
              ) : (
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>Changed At (UTC)</th>
                      <th>Reason</th>
                      <th>Field</th>
                      <th>From</th>
                      <th>To</th>
                    </tr>
                  </thead>
                  <tbody>
                    {changeHistory.map((entry) => (
                      <tr key={entry.id}>
                        <td>{entry.changedAtUtc.slice(0, 19).replace('T', ' ')}</td>
                        <td>{entry.changeReasonCode}</td>
                        <td>{entry.fieldName}</td>
                        <td>{entry.fromValue ?? '-'}</td>
                        <td>{entry.toValue ?? '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>

            <section className={styles.card}>
              <h3>Performance KPIs</h3>
              {!risk || !kpis ? (
                <p>Generate or refresh a schedule to view KPI profile.</p>
              ) : (
                <div className={styles.kpiGrid}>
                  <div>Dispatch Risk: {risk.hasDispatchRisk ? 'At Risk' : 'On Track'}</div>
                  <div>Open Exceptions: {risk.openUnmappedExceptions}</div>
                  <div>Load Readiness %: {kpis.loadReadinessPercent.value ?? 0}</div>
                  <div>Plan Attainment %: {kpis.planAttainmentPercent.value ?? 0}</div>
                  <div>Schedule Adherence %: {kpis.scheduleAdherencePercent.value ?? 0}</div>
                  <div>Stockout Minutes: {kpis.supermarketStockoutDurationMinutes.value ?? 0}</div>
                </div>
              )}
            </section>
          </div>
        )}
      </div>
      <AdminModal
        open={overrideModalOpen}
        title="Freeze Override"
        onCancel={() => setOverrideModalOpen(false)}
        onConfirm={handleFreezeOverride}
        confirmLabel="Apply Override"
        loading={loading}
        confirmDisabled={!overrideDate || !overrideReason || !selectedLineId}
      >
        <div className={styles.overrideForm}>
          <label>
            New Planned Date
            <Input type="date" value={overrideDate} onChange={(_, d) => setOverrideDate(d.value)} />
          </label>
          <label>
            New Planned Qty
            <Input type="number" value={overrideQty} onChange={(_, d) => setOverrideQty(d.value)} />
          </label>
          <label>
            Reason Code
            <Input value={overrideReason} onChange={(_, d) => setOverrideReason(d.value)} />
          </label>
        </div>
      </AdminModal>
    </AdminLayout>
  );
}
