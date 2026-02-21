import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminCharacteristicApi, adminProductApi, adminWorkCenterApi } from '../../api/endpoints.ts';
import type { AdminCharacteristic, ProductType, AdminWorkCenter } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function CharacteristicsScreen() {
  const [items, setItems] = useState<AdminCharacteristic[]>([]);
  const [productTypes, setProductTypes] = useState<ProductType[]>([]);
  const [workCenters, setWorkCenters] = useState<AdminWorkCenter[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminCharacteristic | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [name, setName] = useState('');
  const [specHigh, setSpecHigh] = useState('');
  const [specLow, setSpecLow] = useState('');
  const [specTarget, setSpecTarget] = useState('');
  const [productTypeId, setProductTypeId] = useState('');
  const [selectedWcIds, setSelectedWcIds] = useState<string[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [chars, types, wcs] = await Promise.all([
        adminCharacteristicApi.getAll(), adminProductApi.getTypes(), adminWorkCenterApi.getAll()
      ]);
      setItems(chars); setProductTypes(types); setWorkCenters(wcs);
    } catch { setError('Failed to load characteristics.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openEdit = (item: AdminCharacteristic) => {
    setEditing(item);
    setName(item.name);
    setSpecHigh(item.specHigh != null ? String(item.specHigh) : '');
    setSpecLow(item.specLow != null ? String(item.specLow) : '');
    setSpecTarget(item.specTarget != null ? String(item.specTarget) : '');
    setProductTypeId(item.productTypeId ?? '');
    setSelectedWcIds(item.workCenterIds);
    setError(''); setModalOpen(true);
  };

  const toggleWc = (wcId: string) => {
    setSelectedWcIds(prev => prev.includes(wcId) ? prev.filter(id => id !== wcId) : [...prev, wcId]);
  };

  const handleSave = async () => {
    if (!editing) return;
    setSaving(true); setError('');
    try {
      const updated = await adminCharacteristicApi.update(editing.id, {
        name,
        specHigh: specHigh ? Number(specHigh) : undefined,
        specLow: specLow ? Number(specLow) : undefined,
        specTarget: specTarget ? Number(specTarget) : undefined,
        productTypeId: productTypeId || undefined,
        workCenterIds: selectedWcIds,
      });
      setItems(prev => prev.map(c => c.id === updated.id ? updated : c));
      setModalOpen(false);
    } catch { setError('Failed to save characteristic.'); }
    finally { setSaving(false); }
  };

  return (
    <AdminLayout title="Characteristics">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No characteristics found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                </div>
              </div>
              {(item.specLow != null || item.specHigh != null) && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Spec Range</span>
                  <span className={styles.cardFieldValue}>{item.specLow ?? '—'} to {item.specHigh ?? '—'}</span>
                </div>
              )}
              {item.specTarget != null && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Target</span>
                  <span className={styles.cardFieldValue}>{item.specTarget}</span>
                </div>
              )}
              {item.productTypeName && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Product Type</span>
                  <span className={styles.cardFieldValue}>{item.productTypeName}</span>
                </div>
              )}
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Centers</span>
                <span className={styles.cardFieldValue}>{item.workCenterIds.length} assigned</span>
              </div>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={`Edit ${editing?.name ?? 'Characteristic'}`}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel="Save"
        loading={saving}
        error={error}
        confirmDisabled={!name}
      >
        <Label>Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Spec Low</Label>
        <Input type="number" value={specLow} onChange={(_, d) => setSpecLow(d.value)} placeholder="Optional" />
        <Label>Spec High</Label>
        <Input type="number" value={specHigh} onChange={(_, d) => setSpecHigh(d.value)} placeholder="Optional" />
        <Label>Spec Target</Label>
        <Input type="number" value={specTarget} onChange={(_, d) => setSpecTarget(d.value)} placeholder="Optional" />
        <Label>Product Type</Label>
        <Dropdown
          value={productTypes.find(t => t.id === productTypeId)?.name ?? 'None'}
          selectedOptions={[productTypeId]}
          onOptionSelect={(_, d) => setProductTypeId(d.optionValue ?? '')}
        >
          <Option value="">None</Option>
          {productTypes.map(t => <Option key={t.id} value={t.id}>{t.name}</Option>)}
        </Dropdown>
        <Label>Assign to Work Centers</Label>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4, maxHeight: 200, overflowY: 'auto' }}>
          {workCenters.filter(wc => wc.workCenterTypeName === 'Inspection').map(wc => (
            <label key={wc.id} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
              <input type="checkbox" checked={selectedWcIds.includes(wc.id)} onChange={() => toggleWc(wc.id)} />
              {wc.name}
            </label>
          ))}
        </div>
      </AdminModal>
    </AdminLayout>
  );
}
