import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Checkbox, Dropdown, Option, Spinner, type OptionOnSelectData } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminVendorApi, siteApi } from '../../api/endpoints.ts';
import type { AdminVendor, Plant } from '../../types/domain.ts';
import styles from './CardList.module.css';

const vendorTypeOptions = ['mill', 'processor', 'head'];

function parseSiteCodes(raw?: string | null): string[] {
  if (!raw) return [];
  return raw.split(';').filter(Boolean);
}

function joinSiteCodes(codes: string[]): string | undefined {
  return codes.length > 0 ? codes.join(';') : undefined;
}

function siteCodesDisplay(raw: string | undefined | null, sites: Plant[]): string {
  const codes = parseSiteCodes(raw);
  if (codes.length === 0) return 'All Sites';
  return codes
    .map(c => sites.find(s => s.code === c)?.name ?? c)
    .join(', ');
}

export function VendorMaintenanceScreen() {
  const [items, setItems] = useState<AdminVendor[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminVendor | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<AdminVendor | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [name, setName] = useState('');
  const [vendorType, setVendorType] = useState('');
  const [selectedSites, setSelectedSites] = useState<string[]>([]);
  const [isActive, setIsActive] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [vendors, siteList] = await Promise.all([adminVendorApi.getAll(), siteApi.getSites()]);
      setItems(vendors);
      setSites(siteList);
    } catch { setError('Failed to load vendors.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setName(''); setVendorType(''); setSelectedSites([]); setIsActive(true);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminVendor) => {
    setEditing(item);
    setName(item.name); setVendorType(item.vendorType); setSelectedSites(parseSiteCodes(item.siteCode));
    setIsActive(item.isActive);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (editing) {
        const updated = await adminVendorApi.update(editing.id, {
          name, vendorType, siteCode: joinSiteCodes(selectedSites), isActive,
        });
        setItems(prev => prev.map(v => v.id === updated.id ? updated : v));
      } else {
        const created = await adminVendorApi.create({ name, vendorType, siteCode: joinSiteCodes(selectedSites) });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save vendor.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      const updated = await adminVendorApi.remove(deleteTarget.id);
      setItems(prev => prev.map(v => v.id === updated.id ? updated : v));
      setDeleteTarget(null);
    } catch { alert('Failed to deactivate vendor.'); }
    finally { setDeleting(false); }
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
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{item.vendorType}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Site</span>
                <span className={styles.cardFieldValue}>
                  {siteCodesDisplay(item.siteCode, sites)}
                </span>
              </div>
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
        <Dropdown
          value={vendorType || ''}
          selectedOptions={vendorType ? [vendorType] : []}
          onOptionSelect={(_, d) => { if (d.optionValue) setVendorType(d.optionValue); }}
          placeholder="Select vendor type..."
        >
          {vendorTypeOptions.map(t => (
            <Option key={t} value={t}>{t}</Option>
          ))}
        </Dropdown>
        <Label>Sites (leave empty for all sites)</Label>
        <Dropdown
          multiselect
          value={selectedSites.length > 0
            ? selectedSites.map(c => sites.find(s => s.code === c)?.name ?? c).join(', ')
            : 'All Sites'}
          selectedOptions={selectedSites}
          onOptionSelect={(_, d: OptionOnSelectData) => {
            setSelectedSites(d.selectedOptions.filter(Boolean));
          }}
          placeholder="All Sites"
        >
          {sites.map(s => (
            <Option key={s.code} value={s.code} text={`${s.name} (${s.code})`}>
              {s.name} ({s.code})
            </Option>
          ))}
        </Dropdown>
        {editing && (
          <Checkbox label="Active" checked={isActive} onChange={(_, d) => setIsActive(!!d.checked)} />
        )}
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
