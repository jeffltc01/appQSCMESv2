import { useState, useEffect, useCallback } from 'react';
import { Select, Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { frontendTelemetryApi } from '../../api/endpoints.ts';
import type {
  FrontendTelemetryEntry,
  FrontendTelemetryPage,
  FrontendTelemetryFilterOptions,
  FrontendTelemetryCount,
} from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './FrontendTelemetryScreen.module.css';

const PAGE_SIZE = 50;
const WARNING_THRESHOLD = 250000;

export function FrontendTelemetryScreen() {
  const [data, setData] = useState<FrontendTelemetryPage | null>(null);
  const [filters, setFilters] = useState<FrontendTelemetryFilterOptions>({
    categories: [],
    sources: [],
    severities: [],
  });
  const [countInfo, setCountInfo] = useState<FrontendTelemetryCount | null>(null);
  const [loading, setLoading] = useState(false);
  const [archiving, setArchiving] = useState(false);

  const [category, setCategory] = useState('');
  const [source, setSource] = useState('');
  const [severity, setSeverity] = useState('');
  const [userId, setUserId] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [reactOnly, setReactOnly] = useState(false);
  const [page, setPage] = useState(1);

  const refreshCount = useCallback(async () => {
    try {
      setCountInfo(await frontendTelemetryApi.getCount(WARNING_THRESHOLD));
    } catch {
      setCountInfo(null);
    }
  }, []);

  const loadPage = useCallback(async (targetPage: number) => {
    setLoading(true);
    try {
      const result = await frontendTelemetryApi.getEvents({
        category: category || undefined,
        source: source || undefined,
        severity: severity || undefined,
        userId: userId || undefined,
        workCenterId: workCenterId || undefined,
        from: from ? new Date(`${from}T00:00:00Z`).toISOString() : undefined,
        to: to ? new Date(`${to}T23:59:59Z`).toISOString() : undefined,
        reactRuntimeOnly: reactOnly || undefined,
        page: targetPage,
        pageSize: PAGE_SIZE,
      });
      setData(result);
      setPage(targetPage);
    } catch {
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [category, source, severity, userId, workCenterId, from, to, reactOnly]);

  useEffect(() => {
    frontendTelemetryApi.getFilters().then(setFilters).catch(() => {});
    void refreshCount();
    void loadPage(1);
  }, []);

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  const handleArchive = async () => {
    setArchiving(true);
    try {
      await frontendTelemetryApi.archiveOldest({ keepRows: WARNING_THRESHOLD });
      await refreshCount();
      await loadPage(1);
    } finally {
      setArchiving(false);
    }
  };

  return (
    <AdminLayout
      title="Frontend Telemetry"
      nlqContext={{
        screenKey: 'frontend-telemetry',
        activeFilterTotalCount: data?.totalCount,
        filterSummary: `category=${category || 'all'}, source=${source || 'all'}, severity=${severity || 'all'}, userId=${userId || 'any'}, wcId=${workCenterId || 'any'}, reactOnly=${reactOnly ? 'true' : 'false'}`,
      }}
    >
      {countInfo?.isWarning && (
        <div className={styles.warningBanner}>
          Telemetry has reached {countInfo.rowCount.toLocaleString()} rows (threshold: {countInfo.warningThreshold.toLocaleString()}).
          <button type="button" className={styles.archiveBtn} disabled={archiving} onClick={handleArchive}>
            {archiving ? 'Archiving...' : 'Archive Oldest'}
          </button>
        </div>
      )}

      <div className={styles.filterBar}>
        <div className={styles.filterField}>
          <label>Category</label>
          <Select value={category} onChange={(_, d) => setCategory(d.value)}>
            <option value="">All</option>
            {filters.categories.map((item) => <option key={item} value={item}>{item}</option>)}
          </Select>
        </div>
        <div className={styles.filterField}>
          <label>Source</label>
          <Select value={source} onChange={(_, d) => setSource(d.value)}>
            <option value="">All</option>
            {filters.sources.map((item) => <option key={item} value={item}>{item}</option>)}
          </Select>
        </div>
        <div className={styles.filterField}>
          <label>Severity</label>
          <Select value={severity} onChange={(_, d) => setSeverity(d.value)}>
            <option value="">All</option>
            {filters.severities.map((item) => <option key={item} value={item}>{item}</option>)}
          </Select>
        </div>
        <div className={styles.filterField}>
          <label>User ID</label>
          <input value={userId} onChange={(e) => setUserId(e.target.value)} placeholder="GUID..." />
        </div>
        <div className={styles.filterField}>
          <label>Work Center ID</label>
          <input value={workCenterId} onChange={(e) => setWorkCenterId(e.target.value)} placeholder="GUID..." />
        </div>
        <div className={styles.filterField}>
          <label>From</label>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className={styles.filterField}>
          <label>To</label>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <label className={styles.reactOnlyToggle}>
          <input type="checkbox" checked={reactOnly} onChange={(e) => setReactOnly(e.target.checked)} />
          React red overlay only
        </label>
        <button className={styles.goButton} type="button" onClick={() => loadPage(1)} disabled={loading}>
          Search
        </button>
      </div>

      {loading && <div className={styles.loadingState}><Spinner size="medium" /></div>}

      {!loading && data && data.items.length === 0 && (
        <div className={styles.emptyState}>No telemetry events found.</div>
      )}

      {!loading && data && data.items.length > 0 && (
        <>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Occurred</th>
                  <th>Category</th>
                  <th>Source</th>
                  <th>Message</th>
                  <th>User</th>
                  <th>Work Center</th>
                  <th>HTTP</th>
                  <th>React Runtime</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((entry) => (
                  <TelemetryRow key={entry.id} entry={entry} />
                ))}
              </tbody>
            </table>
          </div>

          <div className={styles.pagination}>
            <button type="button" onClick={() => loadPage(page - 1)} disabled={page <= 1}>Previous</button>
            <span>Page {page} of {totalPages} ({data.totalCount} total)</span>
            <button type="button" onClick={() => loadPage(page + 1)} disabled={page >= totalPages}>Next</button>
          </div>
        </>
      )}
    </AdminLayout>
  );
}

function TelemetryRow({ entry }: { entry: FrontendTelemetryEntry }) {
  return (
    <tr>
      <td>{formatDateTime(entry.occurredAtUtc)}</td>
      <td>{entry.category}</td>
      <td>{entry.source}</td>
      <td className={styles.messageCell} title={entry.message}>{entry.message}</td>
      <td>{entry.userDisplayName ?? entry.userId ?? '-'}</td>
      <td>{entry.workCenterName ?? entry.workCenterId ?? '-'}</td>
      <td>{entry.httpStatus ? `${entry.httpMethod ?? ''} ${entry.httpStatus}`.trim() : '-'}</td>
      <td>{entry.isReactRuntimeOverlayCandidate ? 'Yes' : 'No'}</td>
    </tr>
  );
}
