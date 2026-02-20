import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner, Checkbox, type OptionOnSelectData } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { ConfirmDeleteDialog } from './ConfirmDeleteDialog.tsx';
import { adminProductApi, siteApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import type { AdminProduct, ProductType, Plant } from '../../types/domain.ts';
import styles from './CardList.module.css';

function parseSiteCodes(siteNumbers?: string): string[] {
  if (!siteNumbers) return [];
  return siteNumbers.split(',').map(s => s.trim()).filter(Boolean);
}

function siteCodesToNames(codes: string[], plants: Plant[]): string[] {
  return codes.map(code => {
    const plant = plants.find(p => p.code === code);
    return plant ? plant.name : code;
  });
}

export function ProductMaintenanceScreen() {
  const { user } = useAuth();
  const isAdmin = user?.roleTier === 1;

  const [items, setItems] = useState<AdminProduct[]>([]);
  const [types, setTypes] = useState<ProductType[]>([]);
  const [plants, setPlants] = useState<Plant[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminProduct | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState<AdminProduct | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [productNumber, setProductNumber] = useState('');
  const [tankSize, setTankSize] = useState('');
  const [tankType, setTankType] = useState('');
  const [sageItemNumber, setSageItemNumber] = useState('');
  const [nameplateNumber, setNameplateNumber] = useState('');
  const [productTypeId, setProductTypeId] = useState('');
  const [selectedSites, setSelectedSites] = useState<string[]>([]);
  const [isActive, setIsActive] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [products, productTypes, sitesData] = await Promise.all([
        adminProductApi.getAll(),
        adminProductApi.getTypes(),
        siteApi.getSites(),
      ]);
      setItems(products);
      setTypes(productTypes);
      setPlants(sitesData);
    } catch { setError('Failed to load products.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setProductNumber(''); setTankSize(''); setTankType('');
    setSageItemNumber(''); setNameplateNumber(''); setProductTypeId('');
    setSelectedSites([]); setIsActive(true);
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminProduct) => {
    setEditing(item);
    setProductNumber(item.productNumber); setTankSize(String(item.tankSize)); setTankType(item.tankType);
    setSageItemNumber(item.sageItemNumber ?? ''); setNameplateNumber(item.nameplateNumber ?? '');
    setProductTypeId(item.productTypeId);
    setSelectedSites(parseSiteCodes(item.siteNumbers));
    setIsActive(item.isActive);
    setError(''); setModalOpen(true);
  };


  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = {
        productNumber, tankSize: Number(tankSize), tankType,
        sageItemNumber: sageItemNumber || undefined,
        nameplateNumber: nameplateNumber || undefined,
        siteNumbers: selectedSites.length > 0 ? selectedSites.join(',') : undefined,
        productTypeId, isActive,
      };
      if (editing) {
        const updated = await adminProductApi.update(editing.id, payload);
        setItems(prev => prev.map(p => p.id === updated.id ? updated : p));
      } else {
        const created = await adminProductApi.create(payload);
        setItems(prev => [...prev, created]);
      }
      setModalOpen(false);
    } catch { setError('Failed to save product.'); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      const updated = await adminProductApi.remove(deleteTarget.id);
      setItems(prev => prev.map(p => p.id === updated.id ? updated : p));
      setDeleteTarget(null);
    } catch { alert('Failed to deactivate product.'); }
    finally { setDeleting(false); }
  };

  return (
    <AdminLayout title="Product Maintenance" onAdd={isAdmin ? openAdd : undefined} addLabel="Add Product">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No products found.</div>}
          {items.map(item => {
            const codes = parseSiteCodes(item.siteNumbers);
            const names = siteCodesToNames(codes, plants);
            return (
              <div key={item.id} className={`${styles.card} ${!item.isActive ? styles.cardInactive : ''}`}>
                <div className={styles.cardHeader}>
                  <span className={styles.cardTitle}>{item.productNumber}</span>
                  {isAdmin && (
                    <div className={styles.cardActions}>
                      <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                      <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => setDeleteTarget(item)} />
                    </div>
                  )}
                </div>
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Tank Size</span>
                  <span className={styles.cardFieldValue}>{item.tankSize}</span>
                </div>
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Tank Type</span>
                  <span className={styles.cardFieldValue}>{item.tankType}</span>
                </div>
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Product Type</span>
                  <span className={styles.cardFieldValue}>{item.productTypeName}</span>
                </div>
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Sites</span>
                  <span className={styles.cardFieldValue}>
                    {names.length > 0 ? (
                      names.map(name => (
                        <span key={name} className={`${styles.badge} ${styles.badgeBlue}`} style={{ marginRight: 4 }}>
                          {name}
                        </span>
                      ))
                    ) : (
                      '—'
                    )}
                  </span>
                </div>
                <span className={`${styles.badge} ${item.isActive ? styles.badgeGreen : styles.badgeRed}`}>
                  {item.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            );
          })}
        </div>
      )}

      <AdminModal
        open={modalOpen}
        title={editing ? 'Edit Product' : 'Add Product'}
        onConfirm={handleSave}
        onCancel={() => setModalOpen(false)}
        confirmLabel={editing ? 'Save' : 'Add'}
        loading={saving}
        error={error}
        confirmDisabled={!productNumber || !tankSize || !tankType || !productTypeId}
      >
        <Label>Product Number</Label>
        <Input value={productNumber} onChange={(_, d) => setProductNumber(d.value)} />
        <Label>Tank Size</Label>
        <Input type="number" value={tankSize} onChange={(_, d) => setTankSize(d.value)} />
        <Label>Tank Type</Label>
        <Input value={tankType} onChange={(_, d) => setTankType(d.value)} />
        <Label>Product Type</Label>
        <Dropdown
          value={types.find(t => t.id === productTypeId)?.name ?? ''}
          selectedOptions={[productTypeId]}
          onOptionSelect={(_, d) => { if (d.optionValue) setProductTypeId(d.optionValue); }}
        >
          {types.map(t => <Option key={t.id} value={t.id}>{t.name}</Option>)}
        </Dropdown>
        <Label>Sage Item Number</Label>
        <Input value={sageItemNumber} onChange={(_, d) => setSageItemNumber(d.value)} />
        <Label>Nameplate Number</Label>
        <Input value={nameplateNumber} onChange={(_, d) => setNameplateNumber(d.value)} />
        <Label>Sites</Label>
        <Dropdown
          multiselect
          value={selectedSites.length > 0
            ? selectedSites.map(c => plants.find(p => p.code === c)?.name ?? c).join(', ')
            : ''}
          selectedOptions={selectedSites}
          onOptionSelect={(_, d: OptionOnSelectData) => {
            setSelectedSites(d.selectedOptions.filter(Boolean));
          }}
          placeholder="Select sites..."
        >
          {plants.map(p => (
            <Option key={p.code} value={p.code} text={`${p.name} (${p.code})`}>
              {p.name} ({p.code})
            </Option>
          ))}
        </Dropdown>
        {editing && (
          <Checkbox label="Active" checked={isActive} onChange={(_, d) => setIsActive(!!d.checked)} />
        )}
      </AdminModal>

      <ConfirmDeleteDialog
        open={!!deleteTarget}
        itemName={deleteTarget?.productNumber ?? ''}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
        loading={deleting}
      />
    </AdminLayout>
  );
}
