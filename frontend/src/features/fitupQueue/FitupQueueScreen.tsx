import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import type { MaterialQueueItem, ProductListItem, Vendor } from '../../types/domain.ts';
import { workCenterApi, materialQueueApi, productApi, vendorApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import styles from './FitupQueueScreen.module.css';

interface FormData {
  productId: string;
  vendorHeadId: string;
  lotNumber: string;
  heatNumber: string;
  coilSlabNumber: string;
  cardCode: string;
  cardColor: string;
}

const emptyForm: FormData = {
  productId: '', vendorHeadId: '', lotNumber: '',
  heatNumber: '', coilSlabNumber: '', cardCode: '', cardColor: '',
};

export function FitupQueueScreen(props: WorkCenterProps) {
  const { workCenterId, showScanResult, registerBarcodeHandler, materialQueueForWCId } = props;
  const { user } = useAuth();
  const targetWCId = materialQueueForWCId ?? workCenterId;

  const [queue, setQueue] = useState<MaterialQueueItem[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [selectingField, setSelectingField] = useState<'product' | 'vendor' | null>(null);
  const [selectedVendorType, setSelectedVendorType] = useState<'cmf' | 'compco' | null>(null);

  const loadQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(targetWCId, 'heads');
      setQueue(items.filter((i) => i.status === 'queued'));
    } catch { /* keep stale */ }
  }, [targetWCId]);

  const loadLookups = useCallback(async () => {
    try {
      const plantId = user?.defaultSiteId;
      const [p, v] = await Promise.all([
        productApi.getProducts('head', plantId),
        vendorApi.getVendors('head', plantId),
      ]);
      setProducts(p);
      setVendors(v);
    } catch { /* keep empty */ }
  }, [user?.defaultSiteId]);

  useEffect(() => {
    loadQueue();
    loadLookups();
  }, [workCenterId, loadQueue, loadLookups]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) { showScanResult({ type: 'error', message: 'Unknown barcode' }); return; }
      if (bc.prefix === 'KC' && showForm) {
        setForm((f) => ({ ...f, cardCode: bc.value, cardColor: '' }));
        showScanResult({ type: 'success', message: `Card ${bc.value} scanned` });
        return;
      }
      showScanResult({ type: 'error', message: 'Invalid barcode in this context' });
    },
    [showForm, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const openAdd = useCallback(() => {
    setForm(emptyForm);
    setEditingId(null);
    setSelectedVendorType(null);
    setShowForm(true);
  }, []);

  const handleSave = useCallback(async () => {
    if (!form.productId || !form.vendorHeadId || !form.cardCode) {
      showScanResult({ type: 'error', message: 'Please fill all required fields' });
      return;
    }
    try {
      if (editingId) {
        await materialQueueApi.updateFitupItem(targetWCId, editingId, {
          productId: form.productId,
          vendorHeadId: form.vendorHeadId,
          lotNumber: form.lotNumber || undefined,
          heatNumber: form.heatNumber || undefined,
          coilSlabNumber: form.coilSlabNumber || undefined,
          cardCode: form.cardCode,
        });
        showScanResult({ type: 'success', message: 'Queue item updated' });
      } else {
        await materialQueueApi.addFitupItem(targetWCId, {
          productId: form.productId,
          vendorHeadId: form.vendorHeadId,
          lotNumber: form.lotNumber || undefined,
          heatNumber: form.heatNumber || undefined,
          coilSlabNumber: form.coilSlabNumber || undefined,
          cardCode: form.cardCode,
        });
        showScanResult({ type: 'success', message: 'Head material added to queue' });
      }
      setShowForm(false);
      loadQueue();
      props.refreshHistory();
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to save' });
    }
  }, [form, editingId, targetWCId, showScanResult, loadQueue, props.refreshHistory]);

  const handleDelete = useCallback(async (itemId: string) => {
    try {
      await materialQueueApi.deleteFitupItem(targetWCId, itemId);
      showScanResult({ type: 'success', message: 'Item removed' });
      loadQueue();
      props.refreshHistory();
    } catch { showScanResult({ type: 'error', message: 'Failed to remove' }); }
  }, [targetWCId, showScanResult, loadQueue, props.refreshHistory]);

  const selectedProduct = products.find((p) => p.id === form.productId);
  const selectedVendor = vendors.find((v) => v.id === form.vendorHeadId);
  const isCmf = selectedVendorType === 'cmf' || (selectedVendor?.name?.toLowerCase().includes('cmf') ?? false);

  if (selectingField) {
    const items = selectingField === 'product'
      ? products.map((p) => ({ id: p.id, label: `(${p.tankSize}) ${p.productNumber}` }))
      : vendors.map((v) => ({ id: v.id, label: v.name }));

    return (
      <div className={styles.selectionPopup}>
        <div className={styles.selectionHeader}>
          <span>Select {selectingField === 'product' ? 'Product' : 'Head Vendor'}</span>
          <Button appearance="subtle" onClick={() => setSelectingField(null)}>Back</Button>
        </div>
        <div className={styles.selectionGrid}>
          {items.map((item) => (
            <button key={item.id} className={styles.selectionTile} onClick={() => {
              if (selectingField === 'product') setForm((f) => ({ ...f, productId: item.id }));
              else {
                setForm((f) => ({ ...f, vendorHeadId: item.id, lotNumber: '', heatNumber: '', coilSlabNumber: '' }));
                const v = vendors.find((v) => v.id === item.id);
                setSelectedVendorType(v?.name?.toLowerCase().includes('cmf') ? 'cmf' : 'compco');
              }
              setSelectingField(null);
            }}>
              {item.label}
            </button>
          ))}
        </div>
      </div>
    );
  }

  if (showForm) {
    return (
      <div className={styles.container}>
        <h3 className={styles.formTitle}>{editingId ? 'Edit Head Material' : 'Add Material to Queue'}</h3>
        <div className={styles.formGrid}>
          <div className={styles.formField}>
            <Label required>Product</Label>
            <Button appearance="secondary" size="large" className={styles.selectBtn} onClick={() => setSelectingField('product')}>
              {selectedProduct ? `(${selectedProduct.tankSize}) ${selectedProduct.productNumber}` : 'Select Product...'}
            </Button>
          </div>
          <div className={styles.formField}>
            <Label required>Head Vendor</Label>
            <Button appearance="secondary" size="large" className={styles.selectBtn} onClick={() => setSelectingField('vendor')}>
              {selectedVendor ? selectedVendor.name : 'Select Vendor...'}
            </Button>
          </div>
          {isCmf ? (
            <div className={styles.formField}>
              <Label required>Lot Number</Label>
              <Input size="large" value={form.lotNumber} onChange={(_, d) => setForm((f) => ({ ...f, lotNumber: d.value }))} />
            </div>
          ) : (
            <>
              <div className={styles.formField}>
                <Label required>Heat Number</Label>
                <Input size="large" value={form.heatNumber} onChange={(_, d) => setForm((f) => ({ ...f, heatNumber: d.value }))} />
              </div>
              <div className={styles.formField}>
                <Label required>Coil/Slab No.</Label>
                <Input size="large" value={form.coilSlabNumber} onChange={(_, d) => setForm((f) => ({ ...f, coilSlabNumber: d.value }))} />
              </div>
            </>
          )}
          <div className={styles.formField}>
            <Label required>Queue Card</Label>
            <div className={styles.cardRow}>
              <Input size="large" value={form.cardCode} onChange={(_, d) => setForm((f) => ({ ...f, cardCode: d.value }))} placeholder="Scan card..." />
              {form.cardColor && <span className={styles.colorSwatch} style={{ backgroundColor: form.cardColor }} />}
            </div>
          </div>
        </div>
        <div className={`${styles.formActions} ${styles.formFullWidth}`}>
          <Button appearance="secondary" size="large" onClick={() => setShowForm(false)}>Cancel</Button>
          <Button appearance="primary" size="large" onClick={handleSave}>Save</Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.queueHeader}>
        <h3 className={styles.queueTitle}>Material Queue for: Fitup</h3>
        <div className={styles.headerActions}>
          <Button appearance="primary" size="large" onClick={openAdd}>Add Material to Queue</Button>
          <Button appearance="outline" size="large" onClick={loadQueue}>Refresh</Button>
        </div>
      </div>
      {queue.length === 0 ? (
        <div className={styles.emptyQueue}>No material in queue</div>
      ) : (
        queue.map((item) => (
          <div key={item.id} className={styles.queueCard}>
            <div className={styles.queueInfo}>
              <span className={styles.queueDesc}>{item.productDescription}</span>
              <div className={styles.queueMeta}>
                {item.cardColor && <span className={styles.colorSwatch} style={{ backgroundColor: item.cardColor }} />}
                <span>{item.cardId ?? ''}</span>
                {item.createdAt && <span>{new Date(item.createdAt).toLocaleTimeString()}</span>}
              </div>
            </div>
            <div className={styles.queueActions}>
              <Button appearance="outline" size="large" className={styles.actionBtn} onClick={() => { setForm({ productId: '', vendorHeadId: '', lotNumber: '', heatNumber: item.heatNumber, coilSlabNumber: '', cardCode: item.cardId ?? '', cardColor: item.cardColor ?? '' }); setEditingId(item.id); setShowForm(true); }}>✏️ Edit</Button>
              <Button appearance="outline" size="large" className={styles.actionBtn} onClick={() => handleDelete(item.id)}>🗑️ Delete</Button>
            </div>
          </div>
        ))
      )}
    </div>
  );
}
