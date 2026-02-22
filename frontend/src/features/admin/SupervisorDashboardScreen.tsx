import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
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
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
  ResponsiveContainer,
  Cell,
} from 'recharts';
import { AdminLayout } from './AdminLayout.tsx';
import { workCenterApi, supervisorDashboardApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { WorkCenter, SupervisorDashboardMetrics, SupervisorRecord } from '../../types/domain.ts';
import { todayISOString, formatTimeOnly } from '../../utils/dateFormat.ts';
import styles from './SupervisorDashboardScreen.module.css';

const REFRESH_INTERVAL_MS = 30_000;

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
  const [loading, setLoading] = useState(false);
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
      const [metricsData, recordsData] = await Promise.all([
        supervisorDashboardApi.getMetrics(
          selectedWcId, user.defaultSiteId, today,
          selectedOperatorId ?? undefined,
        ),
        supervisorDashboardApi.getRecords(selectedWcId, user.defaultSiteId, today),
      ]);
      setMetrics(metricsData);
      setRecords(recordsData);
      setCheckedIds((prev) => {
        const validIds = new Set(recordsData.map((r) => r.id));
        return new Set([...prev].filter((id) => validIds.has(id)));
      });
    } catch {
      setMetrics(null);
      setRecords([]);
    } finally {
      setLoading(false);
    }
  }, [selectedWcId, user?.defaultSiteId, selectedOperatorId]);

  useEffect(() => {
    if (selectedWcId) {
      loadData();
    } else {
      setMetrics(null);
      setRecords([]);
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

  const hourlyAvg = useMemo(() => {
    if (!metrics) return 0;
    const nonZero = metrics.hourlyCounts.filter((h) => h.count > 0);
    if (nonZero.length === 0) return 0;
    return nonZero.reduce((sum, h) => sum + h.count, 0) / nonZero.length;
  }, [metrics]);

  const hourlyChartData = useMemo(() => {
    if (!metrics) return [];
    return metrics.hourlyCounts.map((h) => ({
      name: `${h.hour}:00`,
      count: h.count,
      belowAvg: h.count > 0 && h.count < hourlyAvg,
    }));
  }, [metrics, hourlyAvg]);

  const weeklyChartData = useMemo(() => {
    if (!metrics) return [];
    return metrics.weekDailyCounts.map((d) => {
      const dt = new Date(d.date + 'T00:00:00');
      return {
        name: dt.toLocaleDateString([], { weekday: 'short' }),
        count: d.count,
      };
    });
  }, [metrics]);

  const dayNames: string[] = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  const todayDayName = dayNames[new Date().getDay()];

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

            {metrics.dayFPY !== null && (
              <div className={styles.kpiCard}>
                <div className={styles.kpiHeader}>First Pass Yield</div>
                <div className={styles.kpiBody}>
                  <div className={styles.kpiValues}>
                    <div className={styles.kpiValueGroup}>
                      <span className={styles.kpiLabel}>Day</span>
                      <span className={metrics.dayFPY >= 95 ? styles.kpiNumberGreen : styles.kpiNumberRed}>
                        {metrics.dayFPY}%
                      </span>
                    </div>
                    <div className={styles.kpiValueGroup}>
                      <span className={styles.kpiLabel}>Week</span>
                      <span className={(metrics.weekFPY ?? 0) >= 95 ? styles.kpiNumberGreen : styles.kpiNumberRed}>
                        {metrics.weekFPY ?? '--'}%
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {metrics.dayFPY !== null && (
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

          {/* Charts */}
          <div className={styles.chartsRow}>
            <div className={styles.chartCard}>
              <div className={styles.chartHeader}>Hourly Count - Today</div>
              <div className={styles.chartBody}>
                <ResponsiveContainer width="100%" height={220}>
                  <BarChart data={hourlyChartData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e9ecef" />
                    <XAxis dataKey="name" tick={{ fontSize: 10 }} interval={1} />
                    <YAxis tick={{ fontSize: 11 }} allowDecimals={false} />
                    <Tooltip />
                    {hourlyAvg > 0 && (
                      <ReferenceLine
                        y={hourlyAvg}
                        stroke="#868e96"
                        strokeDasharray="4 4"
                        label={{ value: 'Avg', position: 'insideTopRight', fontSize: 10, fill: '#868e96' }}
                      />
                    )}
                    <Bar dataKey="count" maxBarSize={28}>
                      {hourlyChartData.map((entry, index) => (
                        <Cell
                          key={index}
                          fill={entry.belowAvg ? '#e41e2f' : '#2b3b84'}
                        />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div className={styles.chartCard}>
              <div className={styles.chartHeader}>Weekly Trend</div>
              <div className={styles.chartBody}>
                <ResponsiveContainer width="100%" height={220}>
                  <BarChart data={weeklyChartData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e9ecef" />
                    <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                    <YAxis tick={{ fontSize: 11 }} allowDecimals={false} />
                    <Tooltip />
                    <Bar dataKey="count" maxBarSize={36}>
                      {weeklyChartData.map((entry, index) => (
                        <Cell
                          key={index}
                          fill={entry.name === todayDayName ? '#2b3b84' : '#8da0cb'}
                        />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>

          {/* Production Records & Annotation */}
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
    </AdminLayout>
  );
}
