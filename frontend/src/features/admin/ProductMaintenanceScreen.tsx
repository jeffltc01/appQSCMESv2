import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, Spinner } from '@fluentui/react-components';
import { EditRegular, DeleteRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { AdminModal } from './AdminModal.tsx';
import { adminProductApi } from '../../api/endpoints.ts';
import type { AdminProduct, ProductType } from '../../types/domain.ts';
import styles from './CardList.module.css';

export function ProductMaintenanceScreen() {
  const [items, setItems] = useState<AdminProduct[]>([]);
  const [types, setTypes] = useState<ProductType[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AdminProduct | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const [productNumber, setProductNumber] = useState('');
  const [tankSize, setTankSize] = useState('');
  const [tankType, setTankType] = useState('');
  const [sageItemNumber, setSageItemNumber] = useState('');
  const [nameplateNumber, setNameplateNumber] = useState('');
  const [productTypeId, setProductTypeId] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [products, productTypes] = await Promise.all([adminProductApi.getAll(), adminProductApi.getTypes()]);
      setItems(products);
      setTypes(productTypes);
    } catch { setError('Failed to load products.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openAdd = () => {
    setEditing(null);
    setProductNumber(''); setTankSize(''); setTankType('');
    setSageItemNumber(''); setNameplateNumber(''); setProductTypeId('');
    setError(''); setModalOpen(true);
  };

  const openEdit = (item: AdminProduct) => {
    setEditing(item);
    setProductNumber(item.productNumber); setTankSize(String(item.tankSize)); setTankType(item.tankType);
    setSageItemNumber(item.sageItemNumber ?? ''); setNameplateNumber(item.nameplateNumber ?? '');
    setProductTypeId(item.productTypeId);
    setError(''); setModalOpen(true);
  };

  const handleSave = async () => {
    setSaving(true); setError('');
    try {
      const payload = {
        productNumber, tankSize: Number(tankSize), tankType,
        sageItemNumber: sageItemNumber || undefined, nameplateNumber: nameplateNumber || undefined,
        productTypeId,
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

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this product?')) return;
    try {
      await adminProductApi.remove(id);
      setItems(prev => prev.filter(p => p.id !== id));
    } catch { alert('Failed to delete product.'); }
  };

  return (
    <AdminLayout title="Product Maintenance" onAdd={openAdd} addLabel="Add Product">
      {loading ? (
        <div className={styles.loadingState}><Spinner size="medium" label="Loading..." /></div>
      ) : (
        <div className={styles.grid}>
          {items.length === 0 && <div className={styles.emptyState}>No products found.</div>}
          {items.map(item => (
            <div key={item.id} className={styles.card}>
              <div className={styles.cardHeader}>
                <span className={styles.cardTitle}>{item.productNumber}</span>
                <div className={styles.cardActions}>
                  <Button appearance="subtle" icon={<EditRegular />} size="small" onClick={() => openEdit(item)} />
                  <Button appearance="subtle" icon={<DeleteRegular />} size="small" onClick={() => handleDelete(item.id)} />
                </div>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Tank Size</span>
                <span className={styles.cardFieldValue}>{item.tankSize}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Type</span>
                <span className={styles.cardFieldValue}>{item.tankType}</span>
              </div>
              <div className={styles.cardField}>
                <span className={styles.cardFieldLabel}>Product Type</span>
                <span className={styles.cardFieldValue}>{item.productTypeName}</span>
              </div>
              {item.nameplateNumber && (
                <div className={styles.cardField}>
                  <span className={styles.cardFieldLabel}>Nameplate #</span>
                  <span className={styles.cardFieldValue}>{item.nameplateNumber}</span>
                </div>
              )}
            </div>
          ))}
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
      </AdminModal>
    </AdminLayout>
  );
}
