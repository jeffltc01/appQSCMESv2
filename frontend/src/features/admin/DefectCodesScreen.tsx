import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminDefectCodeApi, adminWorkCenterApi } from '../../api/endpoints.ts';
import type { AdminDefectCode, AdminWorkCenter } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function DefectCodesScreen() {
  const [items, setItems] = useState<AdminDefectCode[]>([]);
  const [workCenters, setWorkCenters] = useState<AdminWorkCenter[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminDefectCode | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [severity, setSeverity] = useState('');
  const [systemType, setSystemType] = useState('');
  const [selectedWcIds, setSelectedWcIds] = useState<string[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [codes, wcs] = await Promise.all([adminDefectCodeApi.getAll(), adminWorkCenterApi.getAll()]);
      setItems(codes); setWorkCenters(wcs);
    } catch { setError('Failed to load defect codes.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setCode(''); setName(''); setSeverity(''); setSystemType(''); setSelectedWcIds([]);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminDefectCode) => {
    setEditing(item);
    setCode(item.code); setName(item.name); setSeverity(item.severity ?? '');
    setSystemType(item.systemType ?? ''); setSelectedWcIds(item.workCenterIds);
    setError(''); setModalOpen(true);
  };

  const toggleWc = (wcId: string) => {
    setSelectedWcIds(prev => prev.includes(wcId) ? prev.filter(id => id !== wcId) : [...prev, wcId]);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = {
        code, name, severity: severity || undefined, systemType: systemType || undefined,
        workCenterIds: selectedWcIds,
      };
      if (editing) {
        const updated = await adminDefectCodeApi.update(editing.id, payload);
        setItems(prev => prev.map(d => d.id === updated.id ? updated : d));
      } else {
        const created = await adminDefectCodeApi.create(payload);
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save defect code.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this defect code?')) return;
    try { await adminDefectCodeApi.remove(id); setItems(prev => prev.filter(d => d.id !== id)); }
    catch { alert('Failed to delete defect code.'); }
  };

  const inspectionWcs = workCenters.filter(wc =>
    wc.workCenterTypeName === 'Inspection' || wc.workCenterTypeName === 'Hydro'
  );

  return (
    <AdminLayout title="Defect Codes" onAdd={openAdd} addLabel="Add Code">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No defect codes found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.code} &mdash; {item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item.id)} />
                </div>
              </div>
              {item.severity && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Severity</span>
                  <span className={styles.cardFieldValue}>{item.severity}</span>
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
        title={editing ? 'Edit Defect Code' : 'Add Defect Code'}
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
        <Label>Severity (optional)</Label>
        <Input value={severity} onChange={(_, d) => setSeverity(d.value)} />
        <Label>System Type (optional)</Label>
        <Input value={systemType} onChange={(_, d) => setSystemType(d.value)} />
        <Label>Assign to Work Centers</Label>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4, maxHeight: 200, overflowY: 'auto' }}>
          {inspectionWcs.map(wc => (
            <label key={wc.id} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
              <input
                type="checkbox"
                checked={selectedWcIds.includes(wc.id)}
                onChange={() => toggleWc(wc.id)}
              />
              {wc.name} ({wc.plantName})
            </label>
          ))}
        </div>
      </AdminModal>
    </AdminLayout>
  );
}
