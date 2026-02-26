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
import { NoteRegular, TimerRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { LogDowntimeDialog } from '../../components/dialogs/LogDowntimeDialog.tsx';
import { workCenterApi, supervisorDashboardApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import { Sparkline } from '../../components/charts/Sparkline.tsx';
import { MetricTrendChart } from '../../components/charts/MetricTrendChart.tsx';
import { DefectParetoChart } from '../../components/charts/DefectParetoChart.tsx';
import { DowntimeParetoChart } from '../../components/charts/DowntimeParetoChart.tsx';
import type {
  WorkCenter,
  KpiTrendPoint,
  SupervisorDashboardMetrics,
  SupervisorDashboardTrends,
  DefectParetoResponse,
  DowntimeParetoResponse,
  SupervisorRecord,
  PerformanceTableResponse,
} from '../../types/domain.ts';
import { todayISOString, formatTimeOnly } from '../../utils/dateFormat.ts';
import styles from './SupervisorDashboardScreen.module.css';

const REFRESH_INTERVAL_MS = 30_000;

type ViewMode = 'day' | 'week' | 'month' | 'annotate';
type MetricDrilldownId =
  | 'count'
  | 'fpy'
  | 'defects'
  | 'downtime'
  | 'qtyPerHour'
  | 'oee'
  | 'availability'
  | 'performance'
  | 'quality';

type MetricFormatKind = 'integer' | 'decimal' | 'percent' | 'minutes';
type MetricDrilldownDescriptor = {
  id: MetricDrilldownId;
  title: string;
  cardValueText: string;
  color?: string;
  points: KpiTrendPoint[];
  format: MetricFormatKind;
};

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
  const [trends, setTrends] = useState<SupervisorDashboardTrends | null>(null);
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
  const [showDowntimeDialog, setShowDowntimeDialog] = useState(false);
  const [selectedMetricId, setSelectedMetricId] = useState<MetricDrilldownId | null>(null);
  const [defectPareto, setDefectPareto] = useState<DefectParetoResponse | null>(null);
  const [defectParetoLoading, setDefectParetoLoading] = useState(false);
  const [downtimePareto, setDowntimePareto] = useState<DowntimeParetoResponse | null>(null);
  const [downtimeParetoLoading, setDowntimeParetoLoading] = useState(false);
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
        supervisorDashboardApi.getTrends(
          selectedWcId, user.defaultSiteId, today,
          selectedOperatorId ?? undefined, 30,
        ).then(setTrends),
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
      setTrends(null);
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
      setTrends(null);
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

  const formatMinutes = (minutes: number) => {
    if (minutes === 0) return '--';
    return `${Number.isInteger(minutes) ? minutes : minutes.toFixed(1)} min`;
  };

  const formatNum = (v: number | null) => (v !== null ? String(v) : '--');
  const formatDelta = (v: number | null) => (v !== null ? (v >= 0 ? `+${v}` : String(v)) : '--');
  const formatTrendValue = (value: number | null, format: MetricFormatKind) => {
    if (value === null) return '--';
    if (format === 'minutes') return `${Number.isInteger(value) ? value : value.toFixed(1)} min`;
    if (format === 'percent') return `${value}%`;
    if (format === 'integer') return Number.isInteger(value) ? String(value) : value.toFixed(1);
    return Number.isInteger(value) ? String(value) : value.toFixed(2);
  };
  const deltaBarWidth = (delta: number | null, planned: number | null) => {
    if (delta === null) return 0;
    const base = planned && planned > 0 ? planned : 25;
    const pct = Math.round((Math.abs(delta) / base) * 100);
    return Math.max(10, Math.min(100, pct));
  };
  const metricView: 'day' | 'week' | 'month' = viewMode === 'annotate' ? 'day' : viewMode;
  const selectedPeriodLabel = metricView === 'day' ? 'Day' : metricView === 'week' ? 'Week' : 'Month';
  const selectedCount = metricView === 'day' ? metrics?.dayCount : metricView === 'week' ? metrics?.weekCount : metrics?.monthCount;
  const selectedFpy = metricView === 'day' ? metrics?.dayFPY : metricView === 'week' ? metrics?.weekFPY : metrics?.monthFPY;
  const selectedDefects = metricView === 'day' ? metrics?.dayDefects : metricView === 'week' ? metrics?.weekDefects : metrics?.monthDefects;
  const selectedDowntimeMinutes = metricView === 'day'
    ? metrics?.dayDowntimeMinutes
    : metricView === 'week'
      ? metrics?.weekDowntimeMinutes
      : metrics?.monthDowntimeMinutes;
  const selectedQtyPerHour = metricView === 'day'
    ? metrics?.dayQtyPerHour
    : metricView === 'week'
      ? metrics?.weekQtyPerHour
      : metrics?.monthQtyPerHour;
  const countTrendValues = trends?.count.map((point) => point.value) ?? [];
  const fpyTrendValues = trends?.fpy.map((point) => point.value) ?? [];
  const defectsTrendValues = trends?.defects.map((point) => point.value) ?? [];
  const qtyPerHourTrendValues = trends?.qtyPerHour.map((point) => point.value) ?? [];
  const downtimeTrendValues = trends?.downtimeMinutes.map((point) => point.value) ?? [];
  const oeeTrendValues = trends?.oee.map((point) => point.value) ?? [];
  const availabilityTrendValues = trends?.availability.map((point) => point.value) ?? [];
  const performanceTrendValues = trends?.performance.map((point) => point.value) ?? [];
  const qualityTrendValues = trends?.quality.map((point) => point.value) ?? [];
  const metricDrilldowns: Partial<Record<MetricDrilldownId, MetricDrilldownDescriptor>> = {
    count: {
      id: 'count',
      title: 'Count',
      cardValueText: String(selectedCount ?? '--'),
      points: trends?.count ?? [],
      format: 'integer',
    },
    downtime: {
      id: 'downtime',
      title: 'Total Downtime',
      cardValueText: formatMinutes(selectedDowntimeMinutes ?? 0),
      points: trends?.downtimeMinutes ?? [],
      format: 'minutes',
    },
    qtyPerHour: {
      id: 'qtyPerHour',
      title: 'Qty / Hour',
      cardValueText: String(selectedQtyPerHour ?? '--'),
      points: trends?.qtyPerHour ?? [],
      format: 'decimal',
    },
  };

  if (metrics?.supportsFirstPassYield) {
    metricDrilldowns.fpy = {
      id: 'fpy',
      title: 'First Pass Yield',
      cardValueText: selectedFpy !== null ? `${selectedFpy}%` : '--',
      color: '#65e26f',
      points: trends?.fpy ?? [],
      format: 'percent',
    };
    metricDrilldowns.defects = {
      id: 'defects',
      title: 'Total Defects',
      cardValueText: String(selectedDefects ?? '--'),
      color: '#ff6671',
      points: trends?.defects ?? [],
      format: 'integer',
    };
  }

  if (metrics?.oeeAvailability != null) {
    metricDrilldowns.oee = {
      id: 'oee',
      title: 'OEE',
      cardValueText: metrics?.oeeOverall !== null ? `${metrics.oeeOverall}%` : '--',
      points: trends?.oee ?? [],
      format: 'percent',
    };
    metricDrilldowns.availability = {
      id: 'availability',
      title: 'Availability',
      cardValueText: `${metrics?.oeeAvailability}%`,
      points: trends?.availability ?? [],
      format: 'percent',
    };
    metricDrilldowns.performance = {
      id: 'performance',
      title: 'Performance',
      cardValueText: metrics?.oeePerformance !== null ? `${metrics.oeePerformance}%` : '--',
      points: trends?.performance ?? [],
      format: 'percent',
    };
    metricDrilldowns.quality = {
      id: 'quality',
      title: 'Quality',
      cardValueText: metrics?.oeeQuality !== null ? `${metrics.oeeQuality}%` : '--',
      points: trends?.quality ?? [],
      format: 'percent',
    };
  }

  const selectedMetric = selectedMetricId ? metricDrilldowns[selectedMetricId] ?? null : null;
  const selectedMetricRows = selectedMetric
    ? [...selectedMetric.points].sort((a, b) => b.date.localeCompare(a.date))
    : [];
  const openMetricDrilldown = (id: MetricDrilldownId) => {
    if (metricDrilldowns[id]) setSelectedMetricId(id);
  };

  useEffect(() => {
    if (
      (selectedMetricId !== 'defects' && selectedMetricId !== 'downtime')
      || !selectedWcId
      || !user?.defaultSiteId
    ) {
      setDefectPareto(null);
      setDefectParetoLoading(false);
      setDowntimePareto(null);
      setDowntimeParetoLoading(false);
      return;
    }

    let cancelled = false;
    if (selectedMetricId === 'defects') {
      setDefectParetoLoading(true);
      setDowntimePareto(null);
      supervisorDashboardApi.getDefectPareto(
        selectedWcId,
        user.defaultSiteId,
        metricView,
        todayISOString(),
        selectedOperatorId ?? undefined,
      ).then((data) => {
        if (!cancelled) setDefectPareto(data);
      }).catch(() => {
        if (!cancelled) setDefectPareto({ totalDefects: 0, items: [] });
      }).finally(() => {
        if (!cancelled) setDefectParetoLoading(false);
      });
    } else {
      setDowntimeParetoLoading(true);
      setDefectPareto(null);
      supervisorDashboardApi.getDowntimePareto(
        selectedWcId,
        user.defaultSiteId,
        metricView,
        todayISOString(),
        selectedOperatorId ?? undefined,
      ).then((data) => {
        if (!cancelled) setDowntimePareto(data);
      }).catch(() => {
        if (!cancelled) setDowntimePareto({ totalDowntimeMinutes: 0, items: [] });
      }).finally(() => {
        if (!cancelled) setDowntimeParetoLoading(false);
      });
    }

    return () => {
      cancelled = true;
    };
  }, [selectedMetricId, selectedWcId, user?.defaultSiteId, metricView, selectedOperatorId]);

  return (
    <AdminLayout
      title="Supervisor / Team Lead Dashboard"
      showAskMes
      nlqContext={{
        screenKey: 'supervisor-dashboard',
        workCenterId: selectedWcId || undefined,
        operatorId: selectedOperatorId ?? undefined,
        date: todayISOString(),
        view: viewMode,
        activeFilterTotalCount: viewMode === 'annotate'
          ? records.length
          : (perfTable?.rows.length ?? metrics?.operators.length ?? undefined),
      }}
    >
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

        {selectedWcId && (
          <Button
            appearance="outline"
            icon={<TimerRegular />}
            onClick={() => setShowDowntimeDialog(true)}
            style={{ borderRadius: 0 }}
          >
            Log Downtime
          </Button>
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

          {/* Top Cards */}
          <div className={styles.cardsRow}>
            <button
              type="button"
              className={`${styles.kpiCard} ${styles.metricCardButton}`}
              onClick={() => openMetricDrilldown('count')}
              aria-label="Open Count details"
            >
              <div className={styles.kpiHeader}>Count</div>
              <div className={styles.kpiBody}>
                <div className={styles.kpiValues}>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiNumber}>{selectedCount}</span>
                      <div className={styles.kpiSparkline}>
                        <Sparkline
                          values={countTrendValues}
                          testId="sparkline-count"
                          ariaLabel="Count trend for last 30 days"
                        />
                      </div>
                  </div>
                </div>
              </div>
            </button>

            {metrics.supportsFirstPassYield && (
              <button
                type="button"
                className={`${styles.kpiCard} ${styles.kpiCardFpy} ${styles.metricCardButton}`}
                onClick={() => openMetricDrilldown('fpy')}
                aria-label="Open First Pass Yield details"
              >
                <div className={styles.kpiHeader}>First Pass Yield</div>
                <div className={styles.kpiBody}>
                  <div className={styles.kpiValues}>
                    <div className={styles.kpiValueGroup}>
                      <span className={selectedFpy !== null
                        ? (selectedFpy! >= 95 ? styles.kpiNumberGreen : styles.kpiNumberRed)
                        : styles.kpiNumber}>
                        {selectedFpy !== null
                          ? `${selectedFpy}%`
                          : '--'}
                      </span>
                      <div className={styles.kpiSparkline}>
                        <Sparkline
                          values={fpyTrendValues}
                          color="#65e26f"
                          testId="sparkline-fpy"
                          ariaLabel="First pass yield trend for last 30 days"
                        />
                      </div>
                    </div>
                  </div>
                </div>
              </button>
            )}

            {metrics.supportsFirstPassYield && (
              <button
                type="button"
                className={`${styles.kpiCard} ${styles.kpiCardDefects} ${styles.metricCardButton}`}
                onClick={() => openMetricDrilldown('defects')}
                aria-label="Open Total Defects details"
              >
                <div className={styles.kpiHeader}>Total Defects</div>
                <div className={styles.kpiBody}>
                  <div className={styles.kpiValues}>
                    <div className={styles.kpiValueGroup}>
                      <span
                        className={selectedDefects === 0
                          ? styles.kpiNumberGreen
                          : styles.kpiNumberRed}
                      >
                        {selectedDefects}
                      </span>
                      <div className={styles.kpiSparkline}>
                        <Sparkline
                          values={defectsTrendValues}
                          color="#ff6671"
                          testId="sparkline-defects"
                          ariaLabel="Defect trend for last 30 days"
                        />
                      </div>
                    </div>
                  </div>
                </div>
              </button>
            )}

            <button
              type="button"
              className={`${styles.kpiCard} ${styles.metricCardButton}`}
              onClick={() => openMetricDrilldown('downtime')}
              aria-label="Open Total Downtime details"
            >
              <div className={styles.kpiHeader}>Total Downtime</div>
              <div className={styles.kpiBody}>
                <div className={styles.kpiValues}>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiNumber}>
                      {formatMinutes(selectedDowntimeMinutes ?? 0)}
                    </span>
                    <div className={styles.kpiSparkline}>
                      <Sparkline
                        values={downtimeTrendValues}
                        testId="sparkline-downtime"
                        ariaLabel="Downtime minutes trend for last 30 days"
                      />
                    </div>
                  </div>
                </div>
              </div>
            </button>

            <button
              type="button"
              className={`${styles.kpiCard} ${styles.metricCardButton}`}
              onClick={() => openMetricDrilldown('qtyPerHour')}
              aria-label="Open Qty Per Hour details"
            >
              <div className={styles.kpiHeader}>Qty / Hour</div>
              <div className={styles.kpiBody}>
                <div className={styles.kpiValues}>
                  <div className={styles.kpiValueGroup}>
                    <span className={styles.kpiNumber}>{selectedQtyPerHour}</span>
                    <div className={styles.kpiSparkline}>
                      <Sparkline
                        values={qtyPerHourTrendValues}
                        testId="sparkline-qty-per-hour"
                        ariaLabel="Quantity per hour trend for last 30 days"
                      />
                    </div>
                  </div>
                </div>
              </div>
            </button>

            {metrics.oeeAvailability != null && (
              <button
                type="button"
                className={`${styles.oeeCard} ${styles.oeeCardOverall} ${styles.metricCardButton}`}
                onClick={() => openMetricDrilldown('oee')}
                aria-label="Open OEE details"
              >
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
                  <div className={styles.kpiSparkline}>
                    <Sparkline
                      values={oeeTrendValues}
                      testId="sparkline-oee"
                      ariaLabel="OEE trend for last 30 days"
                    />
                  </div>
                </div>
              </button>
            )}

            {metrics.oeeAvailability != null && (
              <button
                type="button"
                className={`${styles.oeeCard} ${styles.oeeCardAvailability} ${styles.metricCardButton}`}
                onClick={() => openMetricDrilldown('availability')}
                aria-label="Open Availability details"
              >
                <div className={styles.oeeHeader}>Availability</div>
                <div className={styles.oeeBody}>
                  <span className={
                    metrics.oeeAvailability >= 90 ? styles.oeeNumberGreen
                      : metrics.oeeAvailability >= 70 ? styles.oeeNumberAmber
                      : styles.oeeNumberRed
                  }>
                    {metrics.oeeAvailability}%
                  </span>
                  <div className={styles.kpiSparkline}>
                    <Sparkline
                      values={availabilityTrendValues}
                      testId="sparkline-availability"
                      ariaLabel="Availability trend for last 30 days"
                    />
                  </div>
                </div>
              </button>
            )}

            {metrics.oeeAvailability != null && (
              <button
                type="button"
                className={`${styles.oeeCard} ${styles.oeeCardPerformance} ${styles.metricCardButton}`}
                onClick={() => openMetricDrilldown('performance')}
                aria-label="Open Performance details"
              >
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
                  <div className={styles.kpiSparkline}>
                    <Sparkline
                      values={performanceTrendValues}
                      testId="sparkline-performance"
                      ariaLabel="Performance trend for last 30 days"
                    />
                  </div>
                </div>
              </button>
            )}

            {metrics.oeeAvailability != null && (
              <button
                type="button"
                className={`${styles.oeeCard} ${styles.oeeCardQuality} ${styles.metricCardButton}`}
                onClick={() => openMetricDrilldown('quality')}
                aria-label="Open Quality details"
              >
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
                  <div className={styles.kpiSparkline}>
                    <Sparkline
                      values={qualityTrendValues}
                      testId="sparkline-quality"
                      ariaLabel="Quality trend for last 30 days"
                    />
                  </div>
                </div>
              </button>
            )}
          </div>

          {/* Performance Table (Day / Week / Month) */}
          {viewMode !== 'annotate' && perfTable && (
            <>
              <div className={`${styles.sectionHeader} ${styles.sectionHeaderPerf}`}>
                {viewMode === 'day' ? 'Hourly Performance - Today'
                  : viewMode === 'week' ? 'Daily Performance - This Week'
                  : 'Weekly Performance - This Month'}
              </div>
              <div className={styles.perfTableScroll}>
                <table className={`${styles.table} ${styles.perfTable}`}>
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
                        <td className={styles.deltaCell}>
                          {row.delta !== null ? (
                            <div className={styles.deltaWrap}>
                              <span className={row.delta >= 0 ? styles.deltaPositive : styles.deltaNegative}>
                                {formatDelta(row.delta)}
                              </span>
                              <span
                                className={`${styles.deltaBar} ${row.delta >= 0 ? styles.deltaBarPositive : styles.deltaBarNegative}`}
                                style={{ width: `${deltaBarWidth(row.delta, row.planned)}%` }}
                              />
                            </div>
                          ) : '--'}
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
                        <td className={styles.deltaCell}>
                          {perfTable.totalRow.delta !== null ? (
                            <div className={styles.deltaWrap}>
                              <span
                                className={
                                  perfTable.totalRow.delta >= 0 ? styles.deltaPositive : styles.deltaNegative
                                }
                              >
                                {formatDelta(perfTable.totalRow.delta)}
                              </span>
                              <span
                                className={`${styles.deltaBar} ${perfTable.totalRow.delta >= 0 ? styles.deltaBarPositive : styles.deltaBarNegative}`}
                                style={{
                                  width: `${deltaBarWidth(perfTable.totalRow.delta, perfTable.totalRow.planned)}%`,
                                }}
                              />
                            </div>
                          ) : '--'}
                        </td>
                        <td>{perfTable.totalRow.fpy !== null ? `${perfTable.totalRow.fpy}%` : '--'}</td>
                        <td>{perfTable.totalRow.downtimeMinutes > 0 ? perfTable.totalRow.downtimeMinutes : '--'}</td>
                      </tr>
                    </tfoot>
                  )}
                </table>
              </div>
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
      {selectedMetric && (
        <AdminModal
          open
          title={`${selectedMetric.title} Details`}
          onConfirm={() => setSelectedMetricId(null)}
          onCancel={() => setSelectedMetricId(null)}
          confirmLabel="Close"
          hideCancel
          wide
          titleClassName={styles.metricDialogTitle}
          contentClassName={styles.metricDialogContentEdgeToEdge}
          closeButtonClassName={styles.metricDialogCloseButton}
          bodyClassName={styles.metricDialogBodyFlush}
          surfaceClassName={styles.metricDialogSurfaceFlush}
        >
          <div className={styles.metricDialogContent}>
            <div className={styles.metricDialogHeader}>
              <div className={styles.metricDialogHeaderTitle}>Last 30 Days</div>
              <div className={styles.metricDialogCurrentValue}>
                Current {selectedPeriodLabel}: {selectedMetric.cardValueText}
              </div>
            </div>
            {selectedMetric.id === 'defects' || selectedMetric.id === 'downtime' ? (
              <>
                <div className={styles.metricDialogTableHeader}>
                  {selectedMetric.id === 'defects'
                    ? `Defect Pareto (${selectedPeriodLabel})`
                    : `Downtime Reason Pareto (${selectedPeriodLabel})`}
                </div>
                <div className={styles.metricDialogChart}>
                  {selectedMetric.id === 'defects' && defectParetoLoading ? (
                    <Spinner size="small" label="Loading defect Pareto..." />
                  ) : selectedMetric.id === 'downtime' && downtimeParetoLoading ? (
                    <Spinner size="small" label="Loading downtime Pareto..." />
                  ) : selectedMetric.id === 'defects' && defectPareto && defectPareto.items.length > 0 ? (
                    <DefectParetoChart
                      items={defectPareto.items}
                      testId="defect-pareto-chart"
                      ariaLabel={`Defect Pareto for selected ${selectedPeriodLabel.toLowerCase()}`}
                    />
                  ) : selectedMetric.id === 'downtime' && downtimePareto && downtimePareto.items.length > 0 ? (
                    <DowntimeParetoChart
                      items={downtimePareto.items}
                      testId="downtime-pareto-chart"
                      ariaLabel={`Downtime reason Pareto for selected ${selectedPeriodLabel.toLowerCase()}`}
                    />
                  ) : (
                    <div className={styles.emptyState}>
                      {selectedMetric.id === 'defects'
                        ? 'No defect data for selected range.'
                        : 'No downtime data for selected range.'}
                    </div>
                  )}
                </div>
              </>
            ) : (
              <>
                <div className={styles.metricDialogChart}>
                  <MetricTrendChart
                    points={selectedMetric.points}
                    color={selectedMetric.color}
                    testId="metric-drilldown-chart"
                    ariaLabel={`${selectedMetric.title} trend for last 30 days`}
                  />
                </div>
                <div className={styles.metricDialogTableHeader}>Daily Values</div>
                <div className={styles.metricDialogTableWrap}>
                  <table className={`${styles.table} ${styles.metricDialogTable}`} data-testid="metric-drilldown-table">
                    <thead>
                      <tr>
                        <th>Date</th>
                        <th>Value</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedMetricRows.map((point) => (
                        <tr key={point.date}>
                          <td>{point.date}</td>
                          <td>{formatTrendValue(point.value, selectedMetric.format)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </AdminModal>
      )}
      <LogDowntimeDialog
        open={showDowntimeDialog}
        onClose={() => setShowDowntimeDialog(false)}
        onSaved={loadData}
        preselectedWorkCenterId={selectedWcId || undefined}
      />
    </AdminLayout>
  );
}
