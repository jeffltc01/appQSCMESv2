import { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Button,
  Input,
  Label,
  Spinner,
  Select,
  Textarea,
} from '@fluentui/react-components';
import { AddRegular, EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import {
  adminAnnotationApi,
  adminAnnotationTypeApi,
  adminProductionLineApi,
  adminWorkCenterApi,
  siteApi,
} from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type {
  AdminAnnotation,
  AdminAnnotationType,
  AdminWorkCenter,
  Plant,
  ProductionLineAdmin,
} from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './AnnotationMaintenanceScreen.module.css';

type StatusFilter = 'all' | 'open' | 'closed' | 'resolved' | 'unresolved';

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

  const [editStatus, setEditStatus] = useState('Open');
  const [editNotes, setEditNotes] = useState('');
  const [editResolvedNotes, setEditResolvedNotes] = useState('');

  const [createOpen, setCreateOpen] = useState(false);
  const [createSaving, setCreateSaving] = useState(false);
  const [createError, setCreateError] = useState('');
  const [createTypeId, setCreateTypeId] = useState('');
  const [createNotes, setCreateNotes] = useState('');
  const [linkToType, setLinkToType] = useState<'' | 'Plant' | 'ProductionLine' | 'WorkCenter'>('');
  const [linkToId, setLinkToId] = useState('');

  const [allPlants, setAllPlants] = useState<Plant[]>([]);
  const [allLines, setAllLines] = useState<ProductionLineAdmin[]>([]);
  const [allWorkCenters, setAllWorkCenters] = useState<AdminWorkCenter[]>([]);

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

    if (statusFilter === 'open') {
      result = result.filter((a) => a.status === 'Open');
    } else if (statusFilter === 'closed') {
      result = result.filter((a) => a.status === 'Closed');
    } else if (statusFilter === 'resolved') {
      result = result.filter((a) => !!a.resolvedByName);
    } else if (statusFilter === 'unresolved') {
      result = result.filter((a) => !a.resolvedByName);
    }

    return result;
  }, [items, search, typeFilter, statusFilter]);

  const openEdit = (item: AdminAnnotation) => {
    setEditing(item);
    setEditStatus(item.status);
    setEditNotes(item.notes ?? '');
    setEditResolvedNotes(item.resolvedNotes ?? '');
    setModalError('');
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true);
    setModalError('');
    try {
      const updated = await adminAnnotationApi.update(editing.id, {
        status: editStatus,
        notes: editNotes || undefined,
        resolvedNotes: editResolvedNotes || undefined,
        resolvedByUserId: editStatus === 'Closed' && !editing.resolvedByName ? user?.id : undefined,
      });
      setItems((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      setModalOpen(false);
    } catch {
      setModalError('Failed to save annotation.');
    } finally {
      setSaving(false);
    }
  };

  const isNoteType = useMemo(() => {
    if (!createTypeId) return false;
    const t = annotationTypes.find((at) => at.id === createTypeId);
    return t?.name.toLowerCase() === 'note';
  }, [createTypeId, annotationTypes]);

  const openCreate = async () => {
    setCreateError('');
    setCreateNotes('');
    setLinkToType('');
    setLinkToId('');
    const noteType = annotationTypes.find((t) => t.name.toLowerCase() === 'note');
    setCreateTypeId(noteType?.id ?? annotationTypes[0]?.id ?? '');
    setCreateOpen(true);

    try {
      const [plants, lines, wcs] = await Promise.all([
        siteApi.getSites(),
        adminProductionLineApi.getAll(),
        adminWorkCenterApi.getAll(),
      ]);
      setAllPlants(plants);
      setAllLines(lines);
      setAllWorkCenters(wcs);
    } catch {
      /* entity lists are optional; create still works without linking */
    }
  };

  const handleCreate = async () => {
    if (!createTypeId) { setCreateError('Select an annotation type.'); return; }
    if (!createNotes.trim()) { setCreateError('Notes are required.'); return; }
    setCreateSaving(true);
    setCreateError('');
    try {
      const created = await adminAnnotationApi.create({
        annotationTypeId: createTypeId,
        notes: createNotes.trim(),
        initiatedByUserId: user!.id,
        linkedEntityType: isNoteType && linkToType ? linkToType : undefined,
        linkedEntityId: isNoteType && linkToId ? linkToId : undefined,
      });
      setItems((prev) => [created, ...prev]);
      setCreateOpen(false);
    } catch (err: unknown) {
      const msg =
        err && typeof err === 'object' && 'message' in err
          ? (err as { message: string }).message
          : 'Failed to create annotation.';
      setCreateError(msg);
    } finally {
      setCreateSaving(false);
    }
  };

  const linkEntityOptions = useMemo(() => {
    if (linkToType === 'Plant') return allPlants.map((p) => ({ id: p.id, label: `${p.name} (${p.code})` }));
    if (linkToType === 'ProductionLine') return allLines.map((l) => ({ id: l.id, label: `${l.name} — ${l.plantName}` }));
    if (linkToType === 'WorkCenter') return allWorkCenters.map((w) => ({ id: w.id, label: w.name }));
    return [];
  }, [linkToType, allPlants, allLines, allWorkCenters]);

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
            <option value="open">Open</option>
            <option value="closed">Closed</option>
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

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={openCreate}
        >
          Create Annotation
        </Button>
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
                  <th>Status</th>
                  <th>Notes</th>
                  <th>Initiated By</th>
                  <th>Resolved By</th>
                  <th>Resolved Notes</th>
                  <th>Linked To</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((a) => (
                  <tr key={a.id}>
                    <td style={{ whiteSpace: 'nowrap' }}>{formatDateTime(a.createdAt)}</td>
                    <td>{a.serialNumber}</td>
                    <td>{a.annotationTypeName}</td>
                    <td>
                      <span className={`${styles.statusBadge} ${a.status === 'Open' ? styles.statusOpen : styles.statusClosed}`}>
                        {a.status}
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
                      {a.linkedEntityName
                        ? `${a.linkedEntityType}: ${a.linkedEntityName}`
                        : '—'}
                    </td>
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
            <Label>Status</Label>
            <Select
              value={editStatus}
              onChange={(_, d) => setEditStatus(d.value)}
            >
              <option value="Open">Open</option>
              <option value="Closed">Closed</option>
            </Select>
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
            {editing.resolvedByName && (
              <div style={{ fontSize: 13, color: '#155724' }}>
                Already resolved by {editing.resolvedByName}
              </div>
            )}
          </>
        )}
      </AdminModal>

      <AdminModal
        open={createOpen}
        title="Create Annotation"
        onConfirm={handleCreate}
        onCancel={() => setCreateOpen(false)}
        confirmLabel="Create"
        loading={createSaving}
        error={createError}
        confirmDisabled={!createTypeId || !createNotes.trim()}
      >
        <Label htmlFor="create-type">Annotation Type</Label>
        <Select
          id="create-type"
          value={createTypeId}
          onChange={(_, d) => { setCreateTypeId(d.value); setLinkToType(''); setLinkToId(''); }}
        >
          {annotationTypes.map((t) => (
            <option key={t.id} value={t.id}>{t.name}</option>
          ))}
        </Select>

        <Label htmlFor="create-notes" style={{ marginTop: 8 }}>Notes</Label>
        <Textarea
          id="create-notes"
          value={createNotes}
          onChange={(_, d) => setCreateNotes(d.value)}
          placeholder="Annotation notes..."
          rows={3}
        />

        {isNoteType && (
          <>
            <Label htmlFor="link-to-type" style={{ marginTop: 8 }}>Link To (optional)</Label>
            <Select
              id="link-to-type"
              value={linkToType}
              onChange={(_, d) => { setLinkToType(d.value as typeof linkToType); setLinkToId(''); }}
            >
              <option value="">None</option>
              <option value="Plant">Plant</option>
              <option value="ProductionLine">Production Line</option>
              <option value="WorkCenter">Work Center</option>
            </Select>

            {linkToType && (
              <>
                <Label htmlFor="link-to-entity" style={{ marginTop: 8 }}>
                  {linkToType === 'Plant' ? 'Plant' : linkToType === 'ProductionLine' ? 'Production Line' : 'Work Center'}
                </Label>
                <Select
                  id="link-to-entity"
                  value={linkToId}
                  onChange={(_, d) => setLinkToId(d.value)}
                >
                  <option value="">-- Select --</option>
                  {linkEntityOptions.map((e) => (
                    <option key={e.id} value={e.id}>{e.label}</option>
                  ))}
                </Select>
              </>
            )}
          </>
        )}
      </AdminModal>
    </AdminLayout>
  );
}
