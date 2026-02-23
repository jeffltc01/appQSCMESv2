import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Dropdown, Input, Label, Option, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { workCenterApi, downtimeEventApi } from '../../api/endpoints.ts';
import type { WorkCenter, DowntimeEvent } from '../../types/domain.ts';
import { LogDowntimeDialog } from '../../components/dialogs/LogDowntimeDialog.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import styles from './DowntimeEventsScreen.module.css';

const REFRESH_INTERVAL_MS = 30_000;

function todayStr(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function DowntimeEventsScreen() {
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [selectedWcId, setSelectedWcId] = useState('');
  const [selectedWcName, setSelectedWcName] = useState('');
  const [fromDate, setFromDate] = useState(todayStr());
  const [toDate, setToDate] = useState('');
  const [events, setEvents] = useState<DowntimeEvent[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 50;

  const [showAddDialog, setShowAddDialog] = useState(false);
  const [editingEvent, setEditingEvent] = useState<DowntimeEvent | undefined>();
  const [deletingEvent, setDeletingEvent] = useState<DowntimeEvent | null>(null);
  const [deleting, setDeleting] = useState(false);
  const refreshTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    workCenterApi.getWorkCenters().then(setWorkCenters).catch(() => {});
  }, []);

  const loadEvents = useCallback(async () => {
    if (!selectedWcId) return;
    setLoading(true);
    try {
      const toParam = toDate
        ? new Date(new Date(toDate).getTime() + 86400000).toISOString()
        : undefined;
      const fromParam = fromDate ? new Date(fromDate).toISOString() : undefined;
      const data = await downtimeEventApi.getAll(selectedWcId, fromParam, toParam);
      setEvents(data);
    } catch {
      setEvents([]);
    } finally {
      setLoading(false);
    }
  }, [selectedWcId, fromDate, toDate]);

  useEffect(() => {
    if (selectedWcId) {
      setPage(1);
      loadEvents();
    } else {
      setEvents([]);
    }
  }, [selectedWcId, loadEvents]);

  useEffect(() => {
    if (refreshTimerRef.current) clearInterval(refreshTimerRef.current);
    if (selectedWcId) {
      refreshTimerRef.current = setInterval(loadEvents, REFRESH_INTERVAL_MS);
    }
    return () => {
      if (refreshTimerRef.current) clearInterval(refreshTimerRef.current);
    };
  }, [selectedWcId, loadEvents]);

  const handleDelete = async () => {
    if (!deletingEvent) return;
    setDeleting(true);
    try {
      await downtimeEventApi.delete(deletingEvent.id);
      setDeletingEvent(null);
      loadEvents();
    } catch { /* ignore */ } finally {
      setDeleting(false);
    }
  };

  const handleAddClick = () => {
    setEditingEvent(undefined);
    setShowAddDialog(true);
  };

  return (
    <AdminLayout
      title="Downtime Log"
      onAdd={selectedWcId ? handleAddClick : undefined}
      addLabel="Log Downtime"
    >
      <div className={styles.toolbar}>
        <div className={styles.toolbarField}>
          <Label weight="semibold">Work Center</Label>
          <Dropdown
            placeholder="Select a work center..."
            value={selectedWcName}
            selectedOptions={selectedWcId ? [selectedWcId] : []}
            onOptionSelect={(_, data) => {
              setSelectedWcId(data.optionValue ?? '');
              setSelectedWcName(data.optionText ?? '');
            }}
            style={{ minWidth: 260 }}
          >
            {workCenters.map((wc) => (
              <Option key={wc.id} value={wc.id} text={wc.name}>{wc.name}</Option>
            ))}
          </Dropdown>
        </div>
        <div className={styles.toolbarField}>
          <Label weight="semibold">From</Label>
          <Input
            type="date"
            value={fromDate}
            onChange={(_, d) => setFromDate(d.value)}
          />
        </div>
        <div className={styles.toolbarField}>
          <Label weight="semibold">To</Label>
          <Input
            type="date"
            value={toDate}
            onChange={(_, d) => setToDate(d.value)}
            placeholder="Open-ended"
          />
        </div>
      </div>

      {loading && events.length === 0 ? (
        <div className={styles.emptyState}>
          <Spinner size="medium" label="Loading downtime events..." />
        </div>
      ) : !selectedWcId ? (
        <div className={styles.emptyState}>Select a work center to view downtime events.</div>
      ) : events.length === 0 ? (
        <div className={styles.emptyState}>No downtime events found for the selected filters.</div>
      ) : (
        <>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Start</th>
                <th>End</th>
                <th>Duration</th>
                <th>Production Line</th>
                <th>Operator</th>
                <th>Category</th>
                <th>Reason</th>
                <th>Source</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {events.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE).map((evt) => (
                <tr key={evt.id}>
                  <td className={styles.nowrap}>{formatDateTime(evt.startedAt)}</td>
                  <td className={styles.nowrap}>{formatDateTime(evt.endedAt)}</td>
                  <td>{evt.durationMinutes.toFixed(1)} min</td>
                  <td>{evt.productionLineName}</td>
                  <td>{evt.operatorName}</td>
                  <td>{evt.downtimeReasonCategoryName ?? '—'}</td>
                  <td>{evt.downtimeReasonName ?? '—'}</td>
                  <td>
                    <span className={`${styles.badge} ${evt.isAutoGenerated ? styles.badgeAuto : styles.badgeManual}`}>
                      {evt.isAutoGenerated ? 'Auto' : 'Manual'}
                    </span>
                  </td>
                  <td>
                    <div className={styles.actions}>
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<EditRegular />}
                        aria-label="Edit"
                        onClick={() => {
                          setEditingEvent(evt);
                          setShowAddDialog(true);
                        }}
                      />
                      <Button
                        size="small"
                        appearance="subtle"
                        icon={<DeleteRegular />}
                        aria-label="Delete"
                        onClick={() => setDeletingEvent(evt)}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {events.length > PAGE_SIZE && (
            <div className={styles.pagination}>
              <Button size="small" appearance="subtle" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Previous
              </Button>
              <span className={styles.pageInfo}>Page {page} of {Math.ceil(events.length / PAGE_SIZE)}</span>
              <Button size="small" appearance="subtle" disabled={page >= Math.ceil(events.length / PAGE_SIZE)} onClick={() => setPage((p) => p + 1)}>
                Next
              </Button>
            </div>
          )}
        </>
      )}

      <LogDowntimeDialog
        open={showAddDialog}
        onClose={() => { setShowAddDialog(false); setEditingEvent(undefined); }}
        onSaved={loadEvents}
        preselectedWorkCenterId={selectedWcId || undefined}
        editEvent={editingEvent}
      />

      {deletingEvent && (
        <ConfirmDeleteDialog
          open
          itemName={`downtime event (${formatDateTime(deletingEvent.startedAt)} – ${formatDateTime(deletingEvent.endedAt)})`}
          onConfirm={handleDelete}
          onCancel={() => setDeletingEvent(null)}
          loading={deleting}
        />
      )}
    </AdminLayout>
  );
}
