import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminWorkCenterApi } from '../../api/endpoints.ts';
import type { AdminWorkCenter } from '../../types/domain.ts';
import styles from './CardList.module.css';

const DATA_ENTRY_TYPES = [
  'Rolls', 'Barcode', 'Fitup', 'Hydro', 'Spot', 'DataPlate',
  'RealTimeXray', 'Plasma', 'MatQueue-Material', 'MatQueue-Fitup', 'MatQueue-Shell',
];

export function WorkCenterConfigScreen() {
  const [items, setItems] = useState<AdminWorkCenter[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminWorkCenter | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [numberOfWelders, setNumberOfWelders] = useState('');
  const [dataEntryType, setDataEntryType] = useState('');
  const [materialQueueForWCId, setMaterialQueueForWCId] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try { setItems(await adminWorkCenterApi.getAll()); }
    catch { setError('Failed to load work centers.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openEdit = (item: AdminWorkCenter) => {
    setEditing(item);
    setNumberOfWelders(String(item.numberOfWelders));
    setDataEntryType(item.dataEntryType ?? '');
    setMaterialQueueForWCId(item.materialQueueForWCId ?? '');
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true); setError('');
    try {
      const updated = await adminWorkCenterApi.updateConfig(editing.id, {
        numberOfWelders: Number(numberOfWelders),
        dataEntryType: dataEntryType || undefined,
        materialQueueForWCId: materialQueueForWCId || undefined,
      });
      setItems(prev => prev.map(w => w.id === updated.id ? updated : w));
      setModalOpen(false);
    } catch { setError('Failed to save config.'); }
    finally { setSaving(false); }
  };

  return (
    <AdminLayout title="Work Center Config">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Plant</span>
                <span className={styles.cardFieldValue}>{item.plantName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{item.workCenterTypeName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Welders</span>
                <span className={styles.cardFieldValue}>{item.numberOfWelders}</span>
              </div>
              {item.dataEntryType && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Entry</span>
                  <span className={styles.cardFieldValue}>{item.dataEntryType}</span>
                </div>
              )}
              {item.materialQueueForWCName && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Queue For</span>
                  <span className={styles.cardFieldValue}>{item.materialQueueForWCName}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={`Edit ${editing?.name ?? 'Work Center'}`}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Save"
        loading={saving}
        error={error}
      >
        <Label>Number of Welders</Label>
        <Input type="number" value={numberOfWelders} onChange={(_, d) => setNumberOfWelders(d.value)} />
        <Label>Data Entry Type</Label>
        <Dropdown
          value={dataEntryType || 'None'}
          selectedOptions={[dataEntryType]}
          onOptionSelect={(_, d) => setDataEntryType(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {DATA_ENTRY_TYPES.map(t => <Option key={t} value={t}>{t}</Option>)}
        </Dropdown>
        <Label>Material Queue For WC</Label>
        <Dropdown
          value={items.find(w => w.id === materialQueueForWCId)?.name ?? 'None'}
          selectedOptions={[materialQueueForWCId]}
          onOptionSelect={(_, d) => setMaterialQueueForWCId(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {items.filter(w => w.id !== editing?.id).map(w => (
            <Option key={w.id} value={w.id} text={`${w.name} (${w.plantName})`}>{w.name} ({w.plantName})</Option>
          ))}
        </Dropdown>
      </AdminModal>
    </AdminLayout>
  );
}
