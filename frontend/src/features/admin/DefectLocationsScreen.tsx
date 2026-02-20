import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminDefectLocationApi, adminCharacteristicApi } from '../../api/endpoints.ts';
import type { AdminDefectLocation, AdminCharacteristic } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function DefectLocationsScreen() {
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
    setCode(''); setName(''); setDefaultLocationDetail(''); setCharacteristicId('');
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminDefectLocation) => {
    setEditing(item);
    setCode(item.code); setName(item.name);
    setDefaultLocationDetail(item.defaultLocationDetail ?? '');
    setCharacteristicId(item.characteristicId ?? '');
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = {
        code, name,
        defaultLocationDetail: defaultLocationDetail || undefined,
        characteristicId: characteristicId || undefined,
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

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this defect location?')) return;
    try { await adminDefectLocationApi.remove(id); setItems(prev => prev.filter(d => d.id !== id)); }
    catch { alert('Failed to delete defect location.'); }
  };

  return (
    <AdminLayout title="Defect Locations" onAdd={openAdd} addLabel="Add Location">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No defect locations found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.code} &mdash; {item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item.id)} />
                </div>
              </div>
              {item.characteristicName && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Characteristic</span>
                  <span className={styles.cardFieldValue}>{item.characteristicName}</span>
                </div>
              )}
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
      </AdminModal>
    </AdminLayout>
  );
}
