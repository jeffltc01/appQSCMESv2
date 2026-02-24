import React, { useState, useEffect, useCallback } from 'react';
import { Select, Spinner } from '@fluentui/react-components';
import { AdminLayout } from './AdminLayout.tsx';
import { auditLogApi } from '../../api/endpoints.ts';
import type { AuditLogEntry, AuditLogPage } from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './AuditLogScreen.module.css';

const PAGE_SIZE = 50;
const ACTIONS = ['Created', 'Updated', 'Deleted'];

interface ChangeField {
  field: string;
  old: string | null;
  new: string | null;
}

function parseChanges(json: string | null): ChangeField[] {
  if (!json) return [];
  try {
    const obj = JSON.parse(json) as Record<string, { old?: string | null; new?: string | null }>;
    return Object.entries(obj).map(([field, vals]) => ({
      field,
      old: vals.old ?? null,
      new: vals.new ?? null,
    }));
  } catch {
    return [];
  }
}

function ActionBadge({ action }: { action: string }) {
  const cls =
    action === 'Created' ? styles.actionCreated :
    action === 'Updated' ? styles.actionUpdated :
    action === 'Deleted' ? styles.actionDeleted : '';
  return <span className={`${styles.actionBadge} ${cls}`}>{action}</span>;
}

function ChangesDetailRow({ entry }: { entry: AuditLogEntry }) {
  const fields = parseChanges(entry.changes);
  if (fields.length === 0) {
    return (
      <tr>
        <td colSpan={7} className={styles.changesDetail}>
          <em className={styles.nullValue}>No field details recorded.</em>
        </td>
      </tr>
    );
  }

  return (
    <tr>
      <td colSpan={7} className={styles.changesDetail}>
        <table className={styles.changesTable}>
          <thead>
            <tr>
              <th>Field</th>
              <th>Old Value</th>
              <th>New Value</th>
            </tr>
          </thead>
          <tbody>
            {fields.map((f) => (
              <tr key={f.field}>
                <td>{f.field}</td>
                <td>{f.old != null ? <span className={styles.oldValue}>{f.old}</span> : <span className={styles.nullValue}>null</span>}</td>
                <td>{f.new != null ? <span className={styles.newValue}>{f.new}</span> : <span className={styles.nullValue}>null</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </td>
    </tr>
  );
}

export function AuditLogScreen() {
  const [data, setData] = useState<AuditLogPage | null>(null);
  const [loading, setLoading] = useState(false);
  const [entityNames, setEntityNames] = useState<string[]>([]);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const [filterEntity, setFilterEntity] = useState('');
  const [filterAction, setFilterAction] = useState('');
  const [filterEntityId, setFilterEntityId] = useState('');
  const [filterFrom, setFilterFrom] = useState('');
  const [filterTo, setFilterTo] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    auditLogApi.getEntityNames().then(setEntityNames).catch(() => {});
  }, []);

  const fetchLogs = useCallback(async (p: number) => {
    setLoading(true);
    setExpandedId(null);
    try {
      const result = await auditLogApi.getLogs({
        entityName: filterEntity || undefined,
        entityId: filterEntityId || undefined,
        action: filterAction || undefined,
        from: filterFrom ? new Date(filterFrom + 'T00:00:00Z').toISOString() : undefined,
        to: filterTo ? new Date(filterTo + 'T23:59:59Z').toISOString() : undefined,
        page: p,
        pageSize: PAGE_SIZE,
      });
      setData(result);
      setPage(p);
    } catch {
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [filterEntity, filterEntityId, filterAction, filterFrom, filterTo]);

  useEffect(() => {
    fetchLogs(1);
  }, []);

  const handleSearch = () => {
    fetchLogs(1);
  };

  const handleClear = () => {
    setFilterEntity('');
    setFilterAction('');
    setFilterEntityId('');
    setFilterFrom('');
    setFilterTo('');
  };

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 0;

  return (
    <AdminLayout
      title="Audit Log"
      nlqContext={{
        screenKey: 'audit-log',
        activeFilterTotalCount: data?.totalCount,
        filterSummary: `entity=${filterEntity || 'all'}, action=${filterAction || 'all'}, entityId=${filterEntityId || 'any'}, from=${filterFrom || 'any'}, to=${filterTo || 'any'}`,
      }}
    >
      <div className={styles.filterBar}>
        <div className={styles.filterField}>
          <label>Entity</label>
          <Select value={filterEntity} onChange={(_e, d) => setFilterEntity(d.value)}>
            <option value="">All</option>
            {entityNames.map((n) => (
              <option key={n} value={n}>{n}</option>
            ))}
          </Select>
        </div>

        <div className={styles.filterField}>
          <label>Action</label>
          <Select value={filterAction} onChange={(_e, d) => setFilterAction(d.value)}>
            <option value="">All</option>
            {ACTIONS.map((a) => (
              <option key={a} value={a}>{a}</option>
            ))}
          </Select>
        </div>

        <div className={styles.filterField}>
          <label>Entity ID</label>
          <input
            type="text"
            placeholder="Paste GUID..."
            value={filterEntityId}
            onChange={(e) => setFilterEntityId(e.target.value)}
            style={{ height: 32, padding: '0 8px', border: '1px solid #d1d5db', borderRadius: 4, fontSize: 13 }}
          />
        </div>

        <div className={styles.filterField}>
          <label>From</label>
          <input
            type="date"
            value={filterFrom}
            onChange={(e) => setFilterFrom(e.target.value)}
            style={{ height: 32, padding: '0 8px', border: '1px solid #d1d5db', borderRadius: 4, fontSize: 13 }}
          />
        </div>

        <div className={styles.filterField}>
          <label>To</label>
          <input
            type="date"
            value={filterTo}
            onChange={(e) => setFilterTo(e.target.value)}
            style={{ height: 32, padding: '0 8px', border: '1px solid #d1d5db', borderRadius: 4, fontSize: 13 }}
          />
        </div>

        <button className={styles.goButton} onClick={handleSearch} disabled={loading}>
          Search
        </button>
        <button className={styles.clearBtn} onClick={handleClear} type="button">
          Clear
        </button>
      </div>

      {loading && (
        <div className={styles.loadingState}>
          <Spinner size="medium" />
        </div>
      )}

      {!loading && data && data.items.length === 0 && (
        <div className={styles.emptyState}>No audit log entries found.</div>
      )}

      {!loading && data && data.items.length > 0 && (
        <>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Date / Time</th>
                  <th>User</th>
                  <th>Action</th>
                  <th>Entity</th>
                  <th>Entity ID</th>
                  <th>Changes</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((entry) => (
                  <React.Fragment key={entry.id}>
                    <tr
                      className={expandedId === entry.id ? styles.expandedRow : ''}
                    >
                      <td>{formatDateTime(entry.changedAtUtc)}</td>
                      <td>{entry.changedByUserName}</td>
                      <td><ActionBadge action={entry.action} /></td>
                      <td>{entry.entityName}</td>
                      <td className={styles.entityIdCell} title={entry.entityId}>
                        {entry.entityId.substring(0, 8)}...
                      </td>
                      <td>
                        <button
                          className={styles.changesToggle}
                          onClick={() => setExpandedId(expandedId === entry.id ? null : entry.id)}
                        >
                          {expandedId === entry.id ? 'Hide' : 'View'}
                        </button>
                      </td>
                    </tr>
                    {expandedId === entry.id && (
                      <ChangesDetailRow entry={entry} />
                    )}
                  </React.Fragment>
                ))}
              </tbody>
            </table>
          </div>

          <div className={styles.pagination}>
            <button
              onClick={() => fetchLogs(page - 1)}
              disabled={page <= 1}
            >
              Previous
            </button>
            <span className={styles.pageInfo}>
              Page {page} of {totalPages} ({data.totalCount} total)
            </span>
            <button
              onClick={() => fetchLogs(page + 1)}
              disabled={page >= totalPages}
            >
              Next
            </button>
          </div>
        </>
      )}
    </AdminLayout>
  );
}
