import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminAssetApi, adminWorkCenterApi } from '../../api/endpoints.ts';
import type { AdminAsset, AdminWorkCenter } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function AssetManagementScreen() {
  const [items, setItems] = useState<AdminAsset[]>([]);
  const [workCenters, setWorkCenters] = useState<AdminWorkCenter[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminAsset | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [name, setName] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [limbleIdentifier, setLimbleIdentifier] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [assets, wcs] = await Promise.all([adminAssetApi.getAll(), adminWorkCenterApi.getAll()]);
      setItems(assets); setWorkCenters(wcs);
    } catch { setError('Failed to load assets.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setName(''); setWorkCenterId(''); setLimbleIdentifier('');
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminAsset) => {
    setEditing(item);
    setName(item.name); setWorkCenterId(item.workCenterId);
    setLimbleIdentifier(item.limbleIdentifier ?? '');
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = { name, workCenterId, limbleIdentifier: limbleIdentifier || undefined };
      if (editing) {
        const updated = await adminAssetApi.update(editing.id, payload);
        setItems(prev => prev.map(a => a.id === updated.id ? updated : a));
      } else {
        const created = await adminAssetApi.create(payload);
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save asset.'); }
    finally { setSaving(false); }
  };

  return (
    <AdminLayout title="Asset Management" onAdd={openAdd} addLabel="Add Asset">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No assets found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Center</span>
                <span className={styles.cardFieldValue}>{item.workCenterName}</span>
              </div>
              {item.limbleIdentifier && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Limble ID</span>
                  <span className={styles.cardFieldValue}>{item.limbleIdentifier}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Asset' : 'Add Asset'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!name || !workCenterId}
      >
        <Label>Asset Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Work Center</Label>
        <Dropdown
          value={workCenters.find(w => w.id === workCenterId)?.name ?? ''}
          selectedOptions={[workCenterId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setWorkCenterId(d.optionValue); }}
        >
          {workCenters.map(w => <Option key={w.id} value={w.id} text={`${w.name} (${w.plantName})`}>{w.name} ({w.plantName})</Option>)}
        </Dropdown>
        <Label>Limble Identifier (optional)</Label>
        <Input value={limbleIdentifier} onChange={(_, d) => setLimbleIdentifier(d.value)} />
      </AdminModal>
    </AdminLayout>
  );
}
