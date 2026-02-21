import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Checkbox, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminDefectLocationApi, adminCharacteristicApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminDefectLocation, AdminCharacteristic } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function DefectLocationsScreen() {
  const { user } = useAuth();
  const isReadOnly = (user?.roleTier ?? 99) > 2;
  const [items, setItems] = useState<AdminDefectLocation[]>([]);
  const [characteristics, setCharacteristics] = useState<AdminCharacteristic[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminDefectLocation | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [defaultLocationDetail, setDefaultLocationDetail] = useState('');
  const [characteristicId, setCharacteristicId] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [deleteTarget, setDeleteTarget] = useState<AdminDefectLocation | null>(null);
  const [deleting, setDeleting] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [locs, chars] = await Promise.all([adminDefectLocationApi.getAll(), adminCharacteristicApi.getAll()]);
      setItems(locs); setCharacteristics(chars);
    } catch { setError('Failed to load defect locations.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setCode(''); setName(''); setDefaultLocationDetail(''); setCharacteristicId(''); setIsActive(true);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminDefectLocation) => {
    setEditing(item);
    setCode(item.code); setName(item.name);
    setDefaultLocationDetail(item.defaultLocationDetail ?? '');
    setCharacteristicId(item.characteristicId ?? ''); setIsActive(item.isActive);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = {
        code, name,
        defaultLocationDetail: defaultLocationDetail || undefined,
        characteristicId: characteristicId || undefined,
        isActive,
      };
      if (editing) {
        const updated = await adminDefectLocationApi.update(editing.id, payload);
        setItems(prev => prev.map(d => d.id === updated.id ? updated : d));
      } else {
        const created = await adminDefectLocationApi.create(payload);
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save defect location.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      const updated = await adminDefectLocationApi.remove(deleteTarget.id);
      setItems(prev => prev.map(d => d.id === updated.id ? updated : d));
      setDeleteTarget(null);
    } catch { alert('Failed to deactivate defect location.'); }
    finally { setDeleting(false); }
  };

  return (
    <AdminLayout title="Defect Locations" onAdd={isReadOnly ? undefined : openAdd} addLabel="Add Location">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No defect locations found.</div>}
          {items.map(item => (
            <div key={item.id} className={`${styles.card} ${!item.isActive ? styles.cardInactive : ''}`}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.code} &mdash; {item.name}</span>
                {!isReadOnly && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                    <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                  </div>
                )}
              </div>
              {item.characteristicName && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Characteristic</span>
                  <span className={styles.cardFieldValue}>{item.characteristicName}</span>
                </div>
              )}
              <span className={`${styles.badge} ${item.isActive ? styles.badgeGreen : styles.badgeRed}`}>
                {item.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Defect Location' : 'Add Defect Location'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!code || !name}
      >
        <Label>Code</Label>
        <Input value={code} onChange={(_, d) => setCode(d.value)} />
        <Label>Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Default Location Detail (optional)</Label>
        <Input value={defaultLocationDetail} onChange={(_, d) => setDefaultLocationDetail(d.value)} />
        <Label>Characteristic (optional)</Label>
        <Dropdown
          value={characteristics.find(c => c.id === characteristicId)?.name ?? 'None'}
          selectedOptions={[characteristicId]}
          onOptionSelect={(_, d) => setCharacteristicId(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {characteristics.map(c => <Option key={c.id} value={c.id}>{c.name}</Option>)}
        </Dropdown>
        {editing && (
          <Checkbox label="Active" checked={isActive} onChange={(_, d) => setIsActive(!!d.checked)} />
        )}
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        itemName={deleteTarget ? `${deleteTarget.code} — ${deleteTarget.name}` : ''}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
        loading={deleting}
      />
    </AdminLayout>
  );
}
