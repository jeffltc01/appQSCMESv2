import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Checkbox, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminAnnotationTypeApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminAnnotationType } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function AnnotationTypesScreen() {
  const { user } = useAuth();
  const isAdmin = user?.roleTier === 1;

  const [items, setItems] = useState<AdminAnnotationType[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminAnnotationType | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<AdminAnnotationType | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [name, setName] = useState('');
  const [abbreviation, setAbbreviation] = useState('');
  const [requiresResolution, setRequiresResolution] = useState(false);
  const [operatorCanCreate, setOperatorCanCreate] = useState(false);
  const [displayColor, setDisplayColor] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await adminAnnotationTypeApi.getAll();
      setItems(data);
    } catch { setError('Failed to load annotation types.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setName(''); setAbbreviation(''); setRequiresResolution(false);
    setOperatorCanCreate(false); setDisplayColor('');
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminAnnotationType) => {
    setEditing(item);
    setName(item.name); setAbbreviation(item.abbreviation ?? '');
    setRequiresResolution(item.requiresResolution);
    setOperatorCanCreate(item.operatorCanCreate);
    setDisplayColor(item.displayColor ?? '');
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = {
        name,
        abbreviation: abbreviation || undefined,
        requiresResolution,
        operatorCanCreate,
        displayColor: displayColor || undefined,
      };
      if (editing) {
        const updated = await adminAnnotationTypeApi.update(editing.id, payload);
        setItems(prev => prev.map(a => a.id === updated.id ? updated : a));
      } else {
        const created = await adminAnnotationTypeApi.create(payload);
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save annotation type.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await adminAnnotationTypeApi.remove(deleteTarget.id);
      setItems(prev => prev.filter(a => a.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch { alert('Failed to delete annotation type.'); }
    finally { setDeleting(false); }
  };

  return (
    <AdminLayout title="Annotation Types" onAdd={isAdmin ? openAdd : undefined} addLabel="Add Annotation Type">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No annotation types found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                {isAdmin && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                    <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                  </div>
                )}
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Abbreviation</span>
                <span className={styles.cardFieldValue}>{item.abbreviation || '—'}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Color</span>
                <span className={styles.cardFieldValue}>
                  {item.displayColor ? (
                    <span className={styles.colorSwatch} style={{ backgroundColor: item.displayColor }} />
                  ) : '—'}
                </span>
              </div>
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                <span className={`${styles.badge} ${item.requiresResolution ? styles.badgeRed : styles.badgeGray}`}>
                  Requires Resolution: {item.requiresResolution ? 'Yes' : 'No'}
                </span>
                <span className={`${styles.badge} ${item.operatorCanCreate ? styles.badgeGreen : styles.badgeGray}`}>
                  Operator Can Create: {item.operatorCanCreate ? 'Yes' : 'No'}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Annotation Type' : 'Add Annotation Type'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!name}
      >
        <Label>Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Abbreviation</Label>
        <Input value={abbreviation} onChange={(_, d) => setAbbreviation(d.value)} />
        <Checkbox label="Requires Resolution" checked={requiresResolution} onChange={(_, d) => setRequiresResolution(!!d.checked)} />
        <Checkbox label="Operator Can Create" checked={operatorCanCreate} onChange={(_, d) => setOperatorCanCreate(!!d.checked)} />
        <Label>Display Color (hex)</Label>
        <Input value={displayColor} onChange={(_, d) => setDisplayColor(d.value)} placeholder="#FF0000" />
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        itemName={deleteTarget?.name ?? ''}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
        loading={deleting}
      />
    </AdminLayout>
  );
}
