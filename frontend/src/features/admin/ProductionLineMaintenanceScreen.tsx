import { useState, useEffect, useCallback, useMemo } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner, SearchBox } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminProductionLineApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { ProductionLineAdmin, Plant } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function ProductionLineMaintenanceScreen() {
  const { user: authUser } = useAuth();
  const isSiteScoped = (authUser?.roleTier ?? 99) > 2;

  const [items, setItems] = useState<ProductionLineAdmin[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ProductionLineAdmin | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<ProductionLineAdmin | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [search, setSearch] = useState('');

  const [name, setName] = useState('');
  const [plantId, setPlantId] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [lines, siteList] = await Promise.all([
        adminProductionLineApi.getAll(), siteApi.getSites()
      ]);
      setItems(lines); setSites(siteList);
    } catch { setError('Failed to load production lines.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setName('');
    setPlantId(isSiteScoped ? (authUser?.defaultSiteId ?? '') : '');
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: ProductionLineAdmin) => {
    setEditing(item);
    setName(item.name);
    setPlantId(item.plantId);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (editing) {
        const updated = await adminProductionLineApi.update(editing.id, { name, plantId });
        setItems(prev => prev.map(i => i.id === updated.id ? updated : i));
      } else {
        const created = await adminProductionLineApi.create({ name, plantId });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch (err: unknown) {
      const msg = (err as { message?: string })?.message;
      setError(msg ?? 'Failed to save production line.');
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await adminProductionLineApi.remove(deleteTarget.id);
      setItems(prev => prev.filter(i => i.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch { alert('Failed to delete production line.'); }
    finally { setDeleting(false); }
  };

  const visibleSites = useMemo(() => {
    if (!isSiteScoped) return sites;
    return sites.filter(s => s.id === authUser?.defaultSiteId);
  }, [sites, isSiteScoped, authUser?.defaultSiteId]);

  const filteredItems = items.filter(item => {
    if (isSiteScoped && item.plantId !== authUser?.defaultSiteId) return false;
    if (!search) return true;
    const q = search.toLowerCase();
    return item.name.toLowerCase().includes(q)
      || item.plantName.toLowerCase().includes(q);
  });

  return (
    <AdminLayout title="Production Line Maintenance" onAdd={openAdd} addLabel="Add Production Line">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <>
        <div className={styles.filterBar}>
          <SearchBox
            placeholder="Search by name or site..."
            value={search}
            onChange={(_, d) => setSearch(d.value)}
          />
        </div>
        <div className={styles.grid}>
          {filteredItems.length === 0 && <div className={styles.emptyState}>No production lines found.</div>}
          {filteredItems.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Site</span>
                <span className={styles.cardFieldValue}>{item.plantName}</span>
              </div>
            </div>
          ))}
        </div>
        </>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Production Line' : 'Add Production Line'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!name || !plantId}
      >
        <Label>Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Site</Label>
        <Dropdown
          value={visibleSites.find(s => s.id === plantId)?.name ?? ''}
          selectedOptions={[plantId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setPlantId(d.optionValue); }}
          disabled={isSiteScoped}
        >
          {visibleSites.map(s => <Option key={s.id} value={s.id} text={`${s.name} (${s.code})`}>{s.name} ({s.code})</Option>)}
        </Dropdown>
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
