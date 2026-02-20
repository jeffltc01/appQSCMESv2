import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Checkbox, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminVendorApi } from '../../api/endpoints.ts';
import type { AdminVendor } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function VendorMaintenanceScreen() {
  const [items, setItems] = useState<AdminVendor[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminVendor | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [name, setName] = useState('');
  const [vendorType, setVendorType] = useState('');
  const [siteCode, setSiteCode] = useState('');
  const [isActive, setIsActive] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try { setItems(await adminVendorApi.getAll()); }
    catch { setError('Failed to load vendors.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setName(''); setVendorType(''); setSiteCode(''); setIsActive(true);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminVendor) => {
    setEditing(item);
    setName(item.name); setVendorType(item.vendorType); setSiteCode(item.siteCode ?? '');
    setIsActive(item.isActive);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (editing) {
        const updated = await adminVendorApi.update(editing.id, {
          name, vendorType, siteCode: siteCode || undefined, isActive,
        });
        setItems(prev => prev.map(v => v.id === updated.id ? updated : v));
      } else {
        const created = await adminVendorApi.create({ name, vendorType, siteCode: siteCode || undefined });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save vendor.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this vendor?')) return;
    try { await adminVendorApi.remove(id); setItems(prev => prev.filter(v => v.id !== id)); }
    catch { alert('Failed to delete vendor.'); }
  };

  return (
    <AdminLayout title="Vendor Maintenance" onAdd={openAdd} addLabel="Add Vendor">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No vendors found.</div>}
          {items.map(item => (
            <div key={item.id} className={`${styles.card} ${!item.isActive ? styles.cardInactive : ''}`}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item.id)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{item.vendorType}</span>
              </div>
              {item.siteCode && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Site</span>
                  <span className={styles.cardFieldValue}>{item.siteCode}</span>
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
        title={editing ? 'Edit Vendor' : 'Add Vendor'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!name || !vendorType}
      >
        <Label>Vendor Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Vendor Type</Label>
        <Input value={vendorType} onChange={(_, d) => setVendorType(d.value)} placeholder="mill, processor, head..." />
        <Label>Site Code (optional)</Label>
        <Input value={siteCode} onChange={(_, d) => setSiteCode(d.value)} placeholder="Leave blank for all sites" />
        {editing && (
          <Checkbox label="Active" checked={isActive} onChange={(_, d) => setIsActive(!!d.checked)} />
        )}
      </AdminModal>
    </AdminLayout>
  );
}
