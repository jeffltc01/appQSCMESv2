import { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Button,
  Input,
  Label,
  Checkbox,
  Spinner,
  Select,
} from '@fluentui/react-components';
import { EditRegular, FlagFilled } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminAnnotationApi, adminAnnotationTypeApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminAnnotation, AdminAnnotationType, Plant } from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './AnnotationMaintenanceScreen.module.css';

type StatusFilter = 'all' | 'flagged' | 'resolved' | 'unresolved';

export function AnnotationMaintenanceScreen() {
  const { user } = useAuth();
  const roleTier = user?.roleTier ?? 99;
  const isDirectorPlus = roleTier <= 2;

  const [items, setItems] = useState<AdminAnnotation[]>([]);
  const [annotationTypes, setAnnotationTypes] = useState<AdminAnnotationType[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [siteFilter, setSiteFilter] = useState<string>(
    isDirectorPlus ? '' : (user?.defaultSiteId ?? ''),
  );

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminAnnotation | null>(null);
  const [saving, setSaving] = useState(false);
  const [modalError, setModalError] = useState('');

  const [editFlag, setEditFlag] = useState(false);
  const [editNotes, setEditNotes] = useState('');
  const [editResolvedNotes, setEditResolvedNotes] = useState('');
  const [markResolved, setMarkResolved] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const siteId = isDirectorPlus ? (siteFilter || undefined) : user?.defaultSiteId;
      const [annotations, types] = await Promise.all([
        adminAnnotationApi.getAll(siteId),
        adminAnnotationTypeApi.getAll(),
      ]);
      setItems(annotations);
      setAnnotationTypes(types);
    } catch {
      setError('Failed to load annotations.');
    } finally {
      setLoading(false);
    }
  }, [isDirectorPlus, siteFilter, user?.defaultSiteId]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (isDirectorPlus) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [isDirectorPlus]);

  const filteredItems = useMemo(() => {
    let result = items;

    if (search) {
      const lc = search.toLowerCase();
      result = result.filter(
        (a) =>
          a.serialNumber.toLowerCase().includes(lc) ||
          (a.notes ?? '').toLowerCase().includes(lc) ||
          a.initiatedByName.toLowerCase().includes(lc),
      );
    }

    if (typeFilter) {
      result = result.filter((a) => a.annotationTypeId === typeFilter);
    }

    if (statusFilter === 'flagged') {
      result = result.filter((a) => a.flag);
    } else if (statusFilter === 'resolved') {
      result = result.filter((a) => !!a.resolvedByName);
    } else if (statusFilter === 'unresolved') {
      result = result.filter((a) => !a.resolvedByName);
    }

    return result;
  }, [items, search, typeFilter, statusFilter]);

  const openEdit = (item: AdminAnnotation) => {
    setEditing(item);
    setEditFlag(item.flag);
    setEditNotes(item.notes ?? '');
    setEditResolvedNotes(item.resolvedNotes ?? '');
    setMarkResolved(!!item.resolvedByName);
    setModalError('');
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true);
    setModalError('');
    try {
      const updated = await adminAnnotationApi.update(editing.id, {
        flag: editFlag,
        notes: editNotes || undefined,
        resolvedNotes: editResolvedNotes || undefined,
        resolvedByUserId: markResolved && !editing.resolvedByName ? user?.id : undefined,
      });
      setItems((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      setModalOpen(false);
    } catch {
      setModalError('Failed to save annotation.');
    } finally {
      setSaving(false);
    }
  };


  return (
    <AdminLayout title="Annotations">
      <div className={styles.filterBar}>
        <div className={styles.filterField}>
          <label>Search</label>
          <Input
            placeholder="Serial, notes, initiated by..."
            value={search}
            onChange={(_, d) => setSearch(d.value)}
            style={{ minWidth: 220 }}
          />
        </div>
        <div className={styles.filterField}>
          <label>Type</label>
          <Select
            value={typeFilter}
            onChange={(_, d) => setTypeFilter(d.value)}
            style={{ minWidth: 160 }}
          >
            <option value="">All Types</option>
            {annotationTypes.map((t) => (
              <option key={t.id} value={t.id}>{t.name}</option>
            ))}
          </Select>
        </div>
        <div className={styles.filterField}>
          <label>Status</label>
          <Select
            value={statusFilter}
            onChange={(_, d) => setStatusFilter(d.value as StatusFilter)}
            style={{ minWidth: 140 }}
          >
            <option value="all">All</option>
            <option value="flagged">Flagged</option>
            <option value="resolved">Resolved</option>
            <option value="unresolved">Unresolved</option>
          </Select>
        </div>
        {isDirectorPlus && (
          <div className={styles.filterField}>
            <label>Site</label>
            <Select
              value={siteFilter}
              onChange={(_, d) => setSiteFilter(d.value)}
              style={{ minWidth: 160 }}
            >
              <option value="">All Sites</option>
              {sites.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
              ))}
            </Select>
          </div>
        )}
      </div>

      {error && <div style={{ color: '#d13438', marginBottom: 12 }}>{error}</div>}

      {loading ? (
        <div className={styles.loadingState}>
          <Spinner size="medium" label="Loading..." />
        </div>
      ) : filteredItems.length === 0 ? (
        <div className={styles.emptyState}>No annotations found.</div>
      ) : (
        <>
          <div className={styles.countLabel}>
            Showing {filteredItems.length} of {items.length} annotations
          </div>
          <div className={styles.tableContainer}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Serial #</th>
                  <th>Type</th>
                  <th>Flag</th>
                  <th>Notes</th>
                  <th>Initiated By</th>
                  <th>Resolved By</th>
                  <th>Resolved Notes</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((a) => (
                  <tr key={a.id}>
                    <td style={{ whiteSpace: 'nowrap' }}>{formatDateTime(a.createdAt)}</td>
                    <td>{a.serialNumber}</td>
                    <td>
                      <span className={styles.typeCellInner}>
                        <FlagFilled
                          fontSize={16}
                          className={styles.typeFlag}
                          style={{ color: annotationTypes.find((t) => t.id === a.annotationTypeId)?.displayColor ?? '#212529' }}
                        />
                        {a.annotationTypeName}
                      </span>
                    </td>
                    <td>
                      <span className={`${styles.flagBadge} ${a.flag ? styles.flagYes : styles.flagNo}`}>
                        {a.flag ? 'Yes' : 'No'}
                      </span>
                    </td>
                    <td className={styles.notesCell} title={a.notes ?? ''}>{a.notes || '—'}</td>
                    <td>{a.initiatedByName}</td>
                    <td>
                      {a.resolvedByName ? (
                        <span className={`${styles.resolvedBadge} ${styles.resolvedYes}`}>{a.resolvedByName}</span>
                      ) : (
                        <span className={`${styles.resolvedBadge} ${styles.resolvedNo}`}>Unresolved</span>
                      )}
                    </td>
                    <td className={styles.notesCell} title={a.resolvedNotes ?? ''}>{a.resolvedNotes || '—'}</td>
                    <td>
                      <Button
                        appearance="subtle"
                        icon={<EditRegular />}
                        size="small"
                        onClick={() => openEdit(a)}
                        aria-label={`Edit annotation ${a.serialNumber}`}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <AdminModal
        open={modalOpen}
        title="Edit Annotation"
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Save"
        loading={saving}
        error={modalError}
      >
        {editing && (
          <>
            <div style={{ fontSize: 13, color: '#495057', marginBottom: 4 }}>
              <strong>Serial:</strong> {editing.serialNumber} &nbsp;|&nbsp;
              <strong>Type:</strong> {editing.annotationTypeName} &nbsp;|&nbsp;
              <strong>Initiated by:</strong> {editing.initiatedByName}
            </div>
            <Checkbox
              label="Flagged"
              checked={editFlag}
              onChange={(_, d) => setEditFlag(!!d.checked)}
            />
            <Label>Notes</Label>
            <Input
              value={editNotes}
              onChange={(_, d) => setEditNotes(d.value)}
              placeholder="Annotation notes..."
            />
            <Label>Resolved Notes</Label>
            <Input
              value={editResolvedNotes}
              onChange={(_, d) => setEditResolvedNotes(d.value)}
              placeholder="Resolution details..."
            />
            {!editing.resolvedByName && (
              <Checkbox
                label={`Mark as Resolved (by ${user?.displayName ?? 'you'})`}
                checked={markResolved}
                onChange={(_, d) => setMarkResolved(!!d.checked)}
              />
            )}
            {editing.resolvedByName && (
              <div style={{ fontSize: 13, color: '#155724' }}>
                Already resolved by {editing.resolvedByName}
              </div>
            )}
          </>
        )}
      </AdminModal>
    </AdminLayout>
  );
}
