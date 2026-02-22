import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner, Switch } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminPlantPrinterApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminPlantPrinter, Plant } from '../../types/domain.ts';
import styles from './CardList.module.css';

const PRINT_LOCATIONS = ['Nameplate', 'Setdown', 'Rolls', 'Receiving'];

export function PlantPrinterScreen() {
  const { user } = useAuth();
  const isAdmin = user?.roleTier === 1;

  const [items, setItems] = useState<AdminPlantPrinter[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminPlantPrinter | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<AdminPlantPrinter | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [printerName, setPrinterName] = useState('');
  const [plantId, setPlantId] = useState('');
  const [printLocation, setPrintLocation] = useState('');
  const [enabled, setEnabled] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [printers, siteList] = await Promise.all([
        adminPlantPrinterApi.getAll(), siteApi.getSites()
      ]);
      setItems(printers);
      setSites(siteList);
    } catch { setError('Failed to load plant printers.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setPrinterName(''); setPlantId(''); setPrintLocation(''); setEnabled(true);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminPlantPrinter) => {
    setEditing(item);
    setPrinterName(item.printerName);
    setPlantId(item.plantId);
    setPrintLocation(item.printLocation);
    setEnabled(item.enabled);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (editing) {
        const updated = await adminPlantPrinterApi.update(editing.id, {
          printerName, enabled, printLocation,
        });
        setItems(prev => prev.map(p => p.id === updated.id ? updated : p));
      } else {
        const created = await adminPlantPrinterApi.create({
          plantId, printerName, enabled, printLocation,
        });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch (err: unknown) {
      const msg = (err as { message?: string })?.message;
      setError(msg ?? 'Failed to save plant printer.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await adminPlantPrinterApi.remove(deleteTarget.id);
      setItems(prev => prev.filter(p => p.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch { alert('Failed to delete plant printer.'); }
    finally { setDeleting(false); }
  };

  return (
    <AdminLayout title="Plant Printers" onAdd={isAdmin ? openAdd : undefined} addLabel="Add Printer">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No plant printers found.</div>}
          {items.map(item => (
            <div key={item.id} className={`${styles.card} ${!item.enabled ? styles.cardInactive : ''}`}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.printerName}</span>
                {isAdmin && (
                  <div className={styles.cardActions}>
                    <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                    <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                  </div>
                )}
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Plant</span>
                <span className={styles.cardFieldValue}>{item.plantName} ({item.plantCode})</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Location</span>
                <span className={styles.cardFieldValue}>{item.printLocation || '—'}</span>
              </div>
              <span className={`${styles.badge} ${item.enabled ? styles.badgeGreen : styles.badgeRed}`}>
                {item.enabled ? 'Enabled' : 'Disabled'}
              </span>
            </div>
          ))}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Printer' : 'Add Printer'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!printerName || !plantId || !printLocation}
      >
        <Label>Plant</Label>
        <Dropdown
          value={sites.find(s => s.id === plantId)?.name ?? ''}
          selectedOptions={[plantId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setPlantId(d.optionValue); }}
          disabled={!!editing}
        >
          {sites.map(s => <Option key={s.id} value={s.id} text={`${s.name} (${s.code})`}>{s.name} ({s.code})</Option>)}
        </Dropdown>
        <Label>Printer Name</Label>
        <Input value={printerName} onChange={(_, d) => setPrinterName(d.value)} />
        <Label>Print Location</Label>
        <Dropdown
          value={printLocation}
          selectedOptions={[printLocation]}
          onOptionSelect={(_, d) => { if (d.optionValue) setPrintLocation(d.optionValue); }}
        >
          {PRINT_LOCATIONS.map(loc => <Option key={loc} value={loc} text={loc}>{loc}</Option>)}
        </Dropdown>
        <Switch
          label="Enabled"
          checked={enabled}
          onChange={(_, d) => setEnabled(d.checked)}
        />
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        itemName={deleteTarget?.printerName ?? ''}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
        loading={deleting}
      />
    </AdminLayout>
  );
}
