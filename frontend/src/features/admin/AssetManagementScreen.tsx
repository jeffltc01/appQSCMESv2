import { useState, useEffect, useCallback, useMemo } from 'react';
import { Button, Input, Label, Checkbox, Dropdown, Option, Spinner, Select } from '@fluentui/react-components';
import { DeleteRegular, EditRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminAssetApi, adminWorkCenterApi, productionLineApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminAsset, AdminWorkCenter, Plant, ProductionLineAdmin } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function AssetManagementScreen() {
  const { user } = useAuth();
  const roleTier = user?.roleTier ?? 99;
  const isDirectorPlus = roleTier <= 2;
  const isSiteScoped = (user?.roleTier ?? 99) > 2;

  const [items, setItems] = useState<AdminAsset[]>([]);
  const [workCenters, setWorkCenters] = useState<AdminWorkCenter[]>([]);
  const [productionLines, setProductionLines] = useState<ProductionLineAdmin[]>([]);
  const [sites, setSites] = useState<Plant[]>([]);
  const [siteFilter, setSiteFilter] = useState<string>(
    isDirectorPlus ? '' : (user?.defaultSiteId ?? ''),
  );
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminAsset | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AdminAsset | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState('');

  const [name, setName] = useState('');
  const [workCenterId, setWorkCenterId] = useState('');
  const [productionLineId, setProductionLineId] = useState('');
  const [limbleIdentifier, setLimbleIdentifier] = useState('');
  const [laneName, setLaneName] = useState('');
  const [isActive, setIsActive] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const siteId = isDirectorPlus ? (siteFilter || undefined) : user?.defaultSiteId;
      const [assets, wcs, pls] = await Promise.all([
        adminAssetApi.getAll(siteId), adminWorkCenterApi.getAll(), productionLineApi.getAll(),
      ]);
      setItems(assets); setWorkCenters(wcs); setProductionLines(pls);
    } catch { setError('Failed to load assets.'); }
    finally { setLoading(false); }
  }, [isDirectorPlus, siteFilter, user?.defaultSiteId]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (isDirectorPlus) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [isDirectorPlus]);

  const filteredPLs = useMemo(() => {
    if (isDirectorPlus) {
      if (!siteFilter) return productionLines;
      return productionLines.filter(pl => pl.plantId === siteFilter);
    }
    if (!isSiteScoped) return productionLines;
    return productionLines.filter(pl => pl.plantId === user?.defaultSiteId);
  }, [productionLines, isDirectorPlus, isSiteScoped, siteFilter, user?.defaultSiteId]);

  const openAdd = () => {
    setEditing(null);
    setName(''); setWorkCenterId(''); setProductionLineId(''); setLimbleIdentifier(''); setLaneName(''); setIsActive(true);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminAsset) => {
    setEditing(item);
    setName(item.name); setWorkCenterId(item.workCenterId);
    setProductionLineId(item.productionLineId);
    setLimbleIdentifier(item.limbleIdentifier ?? '');
    setLaneName(item.laneName ?? '');
    setIsActive(item.isActive);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      if (editing) {
        const updated = await adminAssetApi.update(editing.id, { name, workCenterId, productionLineId, limbleIdentifier: limbleIdentifier || undefined, laneName: laneName || undefined, isActive });
        setItems(prev => prev.map(a => a.id === updated.id ? updated : a));
      } else {
        const created = await adminAssetApi.create({ name, workCenterId, productionLineId, limbleIdentifier: limbleIdentifier || undefined, laneName: laneName || undefined });
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save asset.'); }
    finally { setSaving(false); }
  };

  const handleConfirmDeactivate = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    setError('');
    try {
      await adminAssetApi.remove(deleteTarget.id);
      setItems(prev => prev.map(a => (a.id === deleteTarget.id ? { ...a, isActive: false } : a)));
      setDeleteTarget(null);
    } catch {
      setError('Failed to deactivate asset.');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <AdminLayout
      title="Asset Management"
      onAdd={openAdd}
      addLabel="Add Asset"
      nlqContext={{
        screenKey: 'asset-management',
        activeFilterTotalCount: items.length,
        filterSummary: `site=${siteFilter || 'all'}`,
      }}
    >
      {isDirectorPlus && (
        <div className={styles.filterBar}>
          <label style={{ fontSize: 12, fontWeight: 600, color: '#495057' }}>Site</label>
          <Select
            value={siteFilter}
            onChange={(_, d) => setSiteFilter(d.value)}
            style={{ minWidth: 160 }}
          >
            <option value="">All Sites</option>
            {sites.map(s => (
              <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
            ))}
          </Select>
        </div>
      )}

      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No assets found.</div>}
          {items.map(item => (
            <div key={item.id} className={`${styles.card} ${!item.isActive ? styles.cardInactive : ''}`}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.name}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  {item.isActive && (
                    <Button
                      appearance="subtle"
                      icon={<DeleteRegular />}
                      size="small"
                      onClick={() => setDeleteTarget(item)}
                      aria-label={`Deactivate ${item.name}`}
                    />
                  )}
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Work Center</span>
                <span className={styles.cardFieldValue}>{item.workCenterName}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Production Line</span>
                <span className={styles.cardFieldValue}>{item.productionLineName}</span>
              </div>
              {item.limbleIdentifier && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Limble ID</span>
                  <span className={styles.cardFieldValue}>{item.limbleIdentifier}</span>
                </div>
              )}
              {item.laneName && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Lane Name</span>
                  <span className={styles.cardFieldValue}>{item.laneName}</span>
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
        title={editing ? 'Edit Asset' : 'Add Asset'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!name || !workCenterId || !productionLineId}
      >
        <Label>Asset Name</Label>
        <Input value={name} onChange={(_, d) => setName(d.value)} />
        <Label>Work Center</Label>
        <Dropdown
          value={workCenters.find(w => w.id === workCenterId)?.name ?? ''}
          selectedOptions={[workCenterId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setWorkCenterId(d.optionValue); }}
        >
          {workCenters.map(w => <Option key={w.id} value={w.id} text={w.name}>{w.name}</Option>)}
        </Dropdown>
        <Label>Production Line</Label>
        <Dropdown
          value={filteredPLs.find(p => p.id === productionLineId) ? `${filteredPLs.find(p => p.id === productionLineId)!.name} (${filteredPLs.find(p => p.id === productionLineId)!.plantName})` : ''}
          selectedOptions={[productionLineId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setProductionLineId(d.optionValue); }}
        >
          {filteredPLs.map(p => <Option key={p.id} value={p.id} text={`${p.name} (${p.plantName})`}>{p.name} ({p.plantName})</Option>)}
        </Dropdown>
        <Label>Limble Identifier (optional)</Label>
        <Input value={limbleIdentifier} onChange={(_, d) => setLimbleIdentifier(d.value)} />
        <Label>Lane Name (optional)</Label>
        <Input value={laneName} onChange={(_, d) => setLaneName(d.value)} maxLength={50} />
        {editing && (
          <Checkbox label="Active" checked={isActive} onChange={(_, d) => setIsActive(!!d.checked)} />
        )}
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        itemName={deleteTarget?.name ?? ''}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleConfirmDeactivate}
        loading={deleting}
      />
    </AdminLayout>
  );
}
