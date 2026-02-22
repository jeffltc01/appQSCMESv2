import { useState, useEffect, useCallback, useRef } from 'react';
import {
  Button,
  Spinner,
  Dropdown,
  Option,
  Textarea,
  Label,
  Checkbox,
} from '@fluentui/react-components';
import { NoteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { workCenterApi, supervisorDashboardApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type {
  WorkCenter,
  SupervisorDashboardMetrics,
  SupervisorRecord,
  PerformanceTableResponse,
} from '../../types/domain.ts';
import { todayISOString, formatTimeOnly } from '../../utils/dateFormat.ts';
import styles from './SupervisorDashboardScreen.module.css';

const REFRESH_INTERVAL_MS = 30_000;

type ViewMode = 'day' | 'week' | 'month' | 'annotate';
const VIEW_MODES: { key: ViewMode; label: string }[] = [
  { key: 'day', label: 'Day' },
  { key: 'week', label: 'Week' },
  { key: 'month', label: 'Month' },
  { key: 'annotate', label: 'Annotate' },
];

const ANNOTATION_TYPES = [
  { id: 'a1000001-0000-0000-0000-000000000001', name: 'Note' },
  { id: 'a1000004-0000-0000-0000-000000000004', name: 'Internal Review' },
  { id: 'a1000005-0000-0000-0000-000000000005', name: 'Correction Needed' },
];

export function SupervisorDashboardScreen() {
  const { user } = useAuth();
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [selectedWcId, setSelectedWcId] = useState('');
  const [selectedWcName, setSelectedWcName] = useState('');
  const [metrics, setMetrics] = useState<SupervisorDashboardMetrics | null>(null);
  const [records, setRecords] = useState<SupervisorRecord[]>([]);
  const [perfTable, setPerfTable] = useState<PerformanceTableResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>('day');
  const [selectedOperatorId, setSelectedOperatorId] = useState<string | null>(null);
  const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());
  const [annotationTypeId, setAnnotationTypeId] = useState(ANNOTATION_TYPES[0].id);
  const [annotationTypeName, setAnnotationTypeName] = useState(ANNOTATION_TYPES[0].name);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitMessage, setSubmitMessage] = useState('');
  const refreshTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    workCenterApi.getWorkCenters().then(setWorkCenters).catch(() => {});
  }, []);

  const loadData = useCallback(async () => {
    if (!selectedWcId || !user?.defaultSiteId) return;
    setLoading(true);
    try {
      const today = todayISOString();
      const fetches: Promise<unknown>[] = [
        supervisorDashboardApi.getMetrics(
          selectedWcId, user.defaultSiteId, today,
          selectedOperatorId ?? undefined,
        ).then(setMetrics),
        supervisorDashboardApi.getRecords(selectedWcId, user.defaultSiteId, today)
          .then((data) => {
            setRecords(data);
            setCheckedIds((prev) => {
              const validIds = new Set(data.map((r) => r.id));
              return new Set([...prev].filter((id) => validIds.has(id)));
            });
          }),
      ];

      if (viewMode !== 'annotate') {
        fetches.push(
          supervisorDashboardApi.getPerformanceTable(
            selectedWcId, user.defaultSiteId, viewMode, today,
            selectedOperatorId ?? undefined,
          ).then(setPerfTable),
        );
      }

      await Promise.all(fetches);
    } catch {
      setMetrics(null);
      setRecords([]);
      setPerfTable(null);
    } finally {
      setLoading(false);
    }
  }, [selectedWcId, user?.defaultSiteId, selectedOperatorId, viewMode]);

  useEffect(() => {
    if (selectedWcId) {
      loadData();
    } else {
      setMetrics(null);
      setRecords([]);
      setPerfTable(null);
      setCheckedIds(new Set());
    }
  }, [selectedWcId, loadData]);

  useEffect(() => {
    if (refreshTimerRef.current) clearInterval(refreshTimerRef.current);
    if (selectedWcId) {
      refreshTimerRef.current = setInterval(loadData, REFRESH_INTERVAL_MS);
    }
    return () => {
      if (refreshTimerRef.current) clearInterval(refreshTimerRef.current);
    };
  }, [selectedWcId, loadData]);

  const handleWcChange = (_: unknown, data: { optionValue?: string; optionText?: string }) => {
    setSelectedWcId(data.optionValue ?? '');
    setSelectedWcName(data.optionText ?? '');
    setSelectedOperatorId(null);
    setCheckedIds(new Set());
    setComment('');
    setSubmitMessage('');
  };

  const toggleCheck = (id: string) => {
    setCheckedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleSubmit = async () => {
    if (checkedIds.size === 0) return;
    setSubmitting(true);
    setSubmitMessage('');
    try {
      const result = await supervisorDashboardApi.submitAnnotation({
        recordIds: [...checkedIds],
        annotationTypeId,
        comment: comment.trim() || undefined,
      });
      setSubmitMessage(`${result.annotationsCreated} annotation(s) created.`);
      setCheckedIds(new Set());
      setComment('');
      await loadData();
    } catch {
      setSubmitMessage('Failed to submit annotation. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const formatSeconds = (s: number) => {
    if (s === 0) return '--';
    const min = Math.floor(s / 60);
    const sec = Math.round(s % 60);
    return min > 0 ? `${min}m ${sec}s` : `${sec}s`;
  };

  const formatNum = (v: number | null) => (v !== null ? String(v) : '--');

  return (
    <AdminLayout title="Supervisor Dashboard">
      {/* Toolbar */}
      <div className={styles.toolbar}>
        <div className={styles.toolbarField}>
          <Label weight="semibold">Work Center</Label>
          <Dropdown
            placeholder="Select a work center..."
            value={selectedWcName}
            selectedOptions={selectedWcId ? [selectedWcId] : []}
            onOptionSelect={handleWcChange}
            style={{ minWidth: 260 }}
          >
            {workCenters.map((wc) => (
              <Option key={wc.id} value={wc.id} text={wc.name}>
                {wc.name}
              </Option>
            ))}
          </Dropdown>
        </div>

        {selectedWcId && (
          <div className={styles.viewToggles}>
            {VIEW_MODES.map((vm) => (
              <button
                key={vm.key}
                className={`${styles.viewToggle} ${viewMode === vm.key ? styles.viewToggleActive : ''}`}
                onClick={() => setViewMode(vm.key)}
              >
                {vm.label}
              </button>
            ))}
          </div>
        )}

        {selectedWcId && <span className={styles.refreshNote}>Auto-refreshes every 30s</span>}
      </div>

      {loading && !metrics ? (
        <div className={styles.emptyState}>
          <Spinner size="medium" label="Loading dashboard..." />
        </div>
      ) : !selectedWcId ? (
        <div className={styles.emptyState}>Select a work center to view the dashboard.</div>
      ) : !metrics ? (
        <div className={styles.emptyState}>No data available.</div>
      ) : (
        <>
          {/* Operator chips */}
          {metrics.operators.length > 0 && (
            <div className={styles.operatorChips}>
              <button
                className={`${styles.chip} ${selectedOperatorId === null ? styles.chipActive : ''}`}
                onClick={() => setSelectedOperatorId(null)}
              >
                All
              </button>
              {metrics.operators.map((op) => (
                <button
                  key={op.id}
                  className={`${styles.chip} ${selectedOperatorId === op.id ? styles.chipActive : ''}`}
                  onClick={() => setSelectedOperatorId(selectedOperatorId === op.id ? null : op.id)}
                >
                  {op.displayName} ({op.recordCount})
                </button>
              ))}
            </div>
          )}

          {/* OEE Cards */}
          {metrics.oeeAvailability !== null && (
            <div className={styles.oeeRow}>
              <div className={styles.oeeCard}>
                <div className={styles.oeeHeader}>OEE</div>
                <div className={styles.oeeBody}>
                  <span className={
                    metrics.oeeOverall !== null
                      ? (metrics.oeeOverall >= 85 ? styles.oeeNumberGreen
                        : metrics.oeeOverall >= 60 ? styles.oeeNumberAmber
                        : styles.oeeNumberRed)
                      : styles.oeeNumber
                  }>
                    {metrics.oeeOverall !== null ? `${metrics.oeeOverall}%` : '--'}
                  </span>
                </div>
              </div>
              <div className={styles.oeeCard}>
                <div className={styles.oeeHeader}>Availability</div>
                <div className={styles.oeeBody}>
                  <span className={
                    metrics.oeeAvailability >= 90 ? styles.oeeNumberGreen
                      : metrics.oeeAvailability >= 70 ? styles.oeeNumberAmber
                      : styles.oeeNumberRed
                  }>
                    {metrics.oeeAvailability}%
                  </span>
                  <span className={styles.oeeSubtext}>
                    {metrics.oeeDowntimeMinutes !== null ? `${metrics.oeeDowntimeMinutes}m down` : ''}
                  </span>
                </div>
              </div>
              <div className={styles.oeeCard}>
                <div className={styles.oeeHeader}>Performance</div>
                <div className={styles.oeeBody}>
                  <span className={
                    metrics.oeePerformance !== null
                      ? (metrics.oeePerformance >= 95 ? styles.oeeNumberGreen
                        : metrics.oeePerformance >= 70 ? styles.oeeNumberAmber
                        : styles.oeeNumberRed)
                      : styles.oeeNumber
                  }>
                    {metrics.oeePerformance !== null ? `${metrics.oeePerformance}%` : '--'}
                  </span>
                  <span className={styles.oeeSubtext}>
                    {metrics.oeeRunTimeMinutes !== null ? `${metrics.oeeRunTimeMinutes}m run` : ''}
                  </span>
                </div>
              </div>
              <div className={styles.oeeCard}>
                <div className={styles.oeeHeader}>Quality</div>
                <div className={styles.oeeBody}>
                  <span className={
                    metrics.oeeQuality !== null
                      ? (metrics.oeeQuality >= 99 ? styles.oeeNumberGreen
                        : metrics.oeeQuality >= 95 ? styles.oeeNumberAmber
                        : styles.oeeNumberRed)
                      : styles.oeeNumber
                  }>
                    {metrics.oeeQuality !== null ? `${metrics.oeeQuality}%` : '--'}
                  </span>
                  <span className={styles.oeeSubtext}>
                    {metrics.oeePlannedMinutes !== null ? `${metrics.oeePlannedMinutes}m planned` : ''}
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* KPI Cards */}
          <div className={styles.kpiRow}>
            <div className={styles.kpiCard}>
              <div className={styles.kpiHeader}>Count</div>
              <div className={styles.kpiBody}>
                <div className={styles.kpiValues}>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiLabel}>Day</span>
                    <span className={styles.kpiNumber}>{metrics.dayCount}</span>
                  </div>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiLabel}>Week</span>
                    <span className={styles.kpiNumber}>{metrics.weekCount}</span>
                  </div>
                </div>
              </div>
            </div>

            {metrics.supportsFirstPassYield && (
              <div className={styles.kpiCard}>
                <div className={styles.kpiHeader}>First Pass Yield</div>
                <div className={styles.kpiBody}>
                  <div className={styles.kpiValues}>
                    <div className={styles.kpiValueGroup}>
                      <span className={styles.kpiLabel}>Day</span>
                      <span className={metrics.dayFPY !== null
                        ? (metrics.dayFPY >= 95 ? styles.kpiNumberGreen : styles.kpiNumberRed)
                        : styles.kpiNumber}>
                        {metrics.dayFPY !== null ? `${metrics.dayFPY}%` : '--'}
                      </span>
                    </div>
                    <div className={styles.kpiValueGroup}>
                      <span className={styles.kpiLabel}>Week</span>
                      <span className={metrics.weekFPY !== null
                        ? ((metrics.weekFPY >= 95) ? styles.kpiNumberGreen : styles.kpiNumberRed)
                        : styles.kpiNumber}>
                        {metrics.weekFPY !== null ? `${metrics.weekFPY}%` : '--'}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {metrics.supportsFirstPassYield && (
              <div className={styles.kpiCard}>
                <div className={styles.kpiHeader}>Total Defects</div>
                <div className={styles.kpiBody}>
                  <div className={styles.kpiValues}>
                    <div className={styles.kpiValueGroup}>
                      <span className={styles.kpiLabel}>Day</span>
                      <span className={metrics.dayDefects === 0 ? styles.kpiNumberGreen : styles.kpiNumberRed}>
                        {metrics.dayDefects}
                      </span>
                    </div>
                    <div className={styles.kpiValueGroup}>
                      <span className={styles.kpiLabel}>Week</span>
                      <span className={metrics.weekDefects === 0 ? styles.kpiNumberGreen : styles.kpiNumberRed}>
                        {metrics.weekDefects}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div className={styles.kpiCard}>
              <div className={styles.kpiHeader}>Avg Time Between Scans</div>
              <div className={styles.kpiBody}>
                <div className={styles.kpiValues}>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiLabel}>Day</span>
                    <span className={styles.kpiNumber}>{formatSeconds(metrics.dayAvgTimeBetweenScans)}</span>
                  </div>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiLabel}>Week</span>
                    <span className={styles.kpiNumber}>{formatSeconds(metrics.weekAvgTimeBetweenScans)}</span>
                  </div>
                </div>
              </div>
            </div>

            <div className={styles.kpiCard}>
              <div className={styles.kpiHeader}>Qty / Hour</div>
              <div className={styles.kpiBody}>
                <div className={styles.kpiValues}>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiLabel}>Day</span>
                    <span className={styles.kpiNumber}>{metrics.dayQtyPerHour}</span>
                  </div>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiLabel}>Week</span>
                    <span className={styles.kpiNumber}>{metrics.weekQtyPerHour}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Performance Table (Day / Week / Month) */}
          {viewMode !== 'annotate' && perfTable && (
            <>
              <div className={styles.sectionHeader}>
                {viewMode === 'day' ? 'Hourly Performance - Today'
                  : viewMode === 'week' ? 'Daily Performance - This Week'
                  : 'Weekly Performance - This Month'}
              </div>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{viewMode === 'day' ? 'Hour' : viewMode === 'week' ? 'Day' : 'Week'}</th>
                    <th>Planned</th>
                    <th>Actual</th>
                    <th>Delta</th>
                    <th>FPY</th>
                    <th>Downtime (min)</th>
                  </tr>
                </thead>
                <tbody>
                  {perfTable.rows.map((row) => (
                    <tr key={row.label}>
                      <td className={styles.perfLabel}>{row.label}</td>
                      <td>{formatNum(row.planned)}</td>
                      <td>{row.actual}</td>
                      <td className={
                        row.delta !== null
                          ? (row.delta >= 0 ? styles.deltaPositive : styles.deltaNegative)
                          : undefined
                      }>
                        {row.delta !== null ? (row.delta >= 0 ? `+${row.delta}` : String(row.delta)) : '--'}
                      </td>
                      <td>{row.fpy !== null ? `${row.fpy}%` : '--'}</td>
                      <td>{row.downtimeMinutes > 0 ? row.downtimeMinutes : '--'}</td>
                    </tr>
                  ))}
                </tbody>
                {perfTable.totalRow && (
                  <tfoot>
                    <tr className={styles.totalRow}>
                      <td className={styles.perfLabel}>{perfTable.totalRow.label}</td>
                      <td>{formatNum(perfTable.totalRow.planned)}</td>
                      <td>{perfTable.totalRow.actual}</td>
                      <td className={
                        perfTable.totalRow.delta !== null
                          ? (perfTable.totalRow.delta >= 0 ? styles.deltaPositive : styles.deltaNegative)
                          : undefined
                      }>
                        {perfTable.totalRow.delta !== null
                          ? (perfTable.totalRow.delta >= 0 ? `+${perfTable.totalRow.delta}` : String(perfTable.totalRow.delta))
                          : '--'}
                      </td>
                      <td>{perfTable.totalRow.fpy !== null ? `${perfTable.totalRow.fpy}%` : '--'}</td>
                      <td>{perfTable.totalRow.downtimeMinutes > 0 ? perfTable.totalRow.downtimeMinutes : '--'}</td>
                    </tr>
                  </tfoot>
                )}
              </table>
            </>
          )}

          {/* Annotate View: Production Records & Annotation */}
          {viewMode === 'annotate' && (
            <>
              <div className={styles.sectionHeader}>Production Records - Today</div>
              {records.length === 0 ? (
                <div className={styles.emptyState}>No production records today at this work center.</div>
              ) : (
                <>
                  <table className={styles.table}>
                    <thead>
                      <tr>
                        <th style={{ width: 40 }}></th>
                        <th>Time</th>
                        <th>Serial / Shell Code</th>
                        <th>Size</th>
                        <th>Operator</th>
                        <th>Annotations</th>
                      </tr>
                    </thead>
                    <tbody>
                      {records.map((r) => (
                        <tr key={r.id}>
                          <td>
                            <Checkbox
                              checked={checkedIds.has(r.id)}
                              onChange={() => toggleCheck(r.id)}
                            />
                          </td>
                          <td>{formatTimeOnly(r.timestamp)}</td>
                          <td>{r.serialOrIdentifier}</td>
                          <td>{r.tankSize ?? '—'}</td>
                          <td>{r.operatorName}</td>
                          <td>
                            {r.annotations.map((a, i) => (
                              <span
                                key={i}
                                className={styles.annotationBadge}
                                style={{ color: a.displayColor ?? '#868e96' }}
                              >
                                {a.abbreviation ?? a.typeName}
                              </span>
                            ))}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>

                  <div className={styles.submitArea}>
                    <div className={styles.annotationTypeField}>
                      <Label weight="semibold">Annotation Type</Label>
                      <Dropdown
                        value={annotationTypeName}
                        selectedOptions={[annotationTypeId]}
                        onOptionSelect={(_, data) => {
                          setAnnotationTypeId(data.optionValue ?? ANNOTATION_TYPES[0].id);
                          setAnnotationTypeName(data.optionText ?? ANNOTATION_TYPES[0].name);
                        }}
                        style={{ minWidth: 200 }}
                      >
                        {ANNOTATION_TYPES.map((at) => (
                          <Option key={at.id} value={at.id} text={at.name}>
                            {at.name}
                          </Option>
                        ))}
                      </Dropdown>
                    </div>
                    <div className={styles.commentField}>
                      <Label weight="semibold">Comment (optional)</Label>
                      <Textarea
                        value={comment}
                        onChange={(_, d) => setComment(d.value)}
                        placeholder="Add a note..."
                        rows={2}
                        resize="vertical"
                        style={{ width: '100%' }}
                      />
                    </div>
                    <Button
                      appearance="primary"
                      icon={<NoteRegular />}
                      onClick={handleSubmit}
                      disabled={checkedIds.size === 0 || submitting}
                      style={{ borderRadius: 0 }}
                    >
                      {submitting ? <Spinner size="tiny" /> : `Annotate ${checkedIds.size} Record(s)`}
                    </Button>
                  </div>

                  {submitMessage && (
                    <div
                      className={`${styles.submitMessage} ${
                        submitMessage.includes('Failed') ? styles.submitMessageError : styles.submitMessageSuccess
                      }`}
                    >
                      {submitMessage}
                    </div>
                  )}
                </>
              )}
            </>
          )}
        </>
      )}
    </AdminLayout>
  );
}
