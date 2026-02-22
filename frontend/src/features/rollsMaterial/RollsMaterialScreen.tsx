import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { MaterialQueueItem, ProductListItem, Vendor } from '../../types/domain.ts';
import { workCenterApi, materialQueueApi, productApi, vendorApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import styles from './RollsMaterialScreen.module.css';

interface FormData {
  productId: string;
  vendorMillId: string;
  vendorProcessorId: string;
  heatNumber: string;
  coilNumber: string;
  lotNumber: string;
  quantity: string;
}

const emptyForm: FormData = {
  productId: '', vendorMillId: '', vendorProcessorId: '',
  heatNumber: '', coilNumber: '', lotNumber: '', quantity: '',
};

export function RollsMaterialScreen(props: WorkCenterProps) {
  const { workCenterId, showScanResult, materialQueueForWCId } = props;
  const { user } = useAuth();
  const targetWCId = materialQueueForWCId ?? workCenterId;

  const [queue, setQueue] = useState<MaterialQueueItem[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [mills, setMills] = useState<Vendor[]>([]);
  const [processors, setProcessors] = useState<Vendor[]>([]);
  const [selectingField, setSelectingField] = useState<'product' | 'mill' | 'processor' | null>(null);

  const loadQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(targetWCId);
      setQueue(items.filter((i) => i.status === 'queued'));
    } catch { /* keep stale */ }
  }, [targetWCId]);

  const loadLookups = useCallback(async () => {
    try {
      const plantId = user?.defaultSiteId;
      const [p, m, pr] = await Promise.all([
        productApi.getProducts('plate', plantId),
        vendorApi.getVendors('mill', plantId),
        vendorApi.getVendors('processor', plantId),
      ]);
      setProducts(p);
      setMills(m);
      setProcessors(pr);
    } catch { /* keep empty */ }
  }, [user?.defaultSiteId]);

  useEffect(() => {
    loadQueue();
    loadLookups();
  }, [workCenterId, loadQueue, loadLookups]);

  const openAdd = useCallback(() => {
    setForm(emptyForm);
    setEditingId(null);
    setShowForm(true);
  }, []);

  const openEdit = useCallback((item: MaterialQueueItem) => {
    setForm({
      productId: item.productId ?? '',
      vendorMillId: item.vendorMillId ?? '',
      vendorProcessorId: item.vendorProcessorId ?? '',
      heatNumber: item.heatNumber,
      coilNumber: item.coilNumber,
      lotNumber: item.lotNumber ?? '',
      quantity: item.quantity.toString(),
    });
    setEditingId(item.id);
    setShowForm(true);
  }, []);

  const handleSave = useCallback(async () => {
    if (!form.productId || !form.heatNumber || !form.coilNumber || !form.quantity) {
      showScanResult({ type: 'error', message: 'Please fill all required fields' });
      return;
    }
    try {
      if (editingId) {
        await materialQueueApi.updateItem(targetWCId, editingId, {
          productId: form.productId,
          vendorMillId: form.vendorMillId || undefined,
          vendorProcessorId: form.vendorProcessorId || undefined,
          heatNumber: form.heatNumber,
          coilNumber: form.coilNumber,
          lotNumber: form.lotNumber || undefined,
          quantity: parseInt(form.quantity, 10),
        });
        showScanResult({ type: 'success', message: 'Queue item updated' });
      } else {
        await materialQueueApi.addItem(targetWCId, {
          productId: form.productId,
          vendorMillId: form.vendorMillId || undefined,
          vendorProcessorId: form.vendorProcessorId || undefined,
          heatNumber: form.heatNumber,
          coilNumber: form.coilNumber,
          lotNumber: form.lotNumber || undefined,
          quantity: parseInt(form.quantity, 10),
        });
        showScanResult({ type: 'success', message: 'Material added to queue' });
      }
      setShowForm(false);
      loadQueue();
      props.refreshHistory();
    } catch (err: unknown) {
      const msg = (err && typeof err === 'object' && 'message' in err) ? String((err as { message: string }).message) : 'Failed to save queue item';
      showScanResult({ type: 'error', message: msg });
    }
  }, [form, editingId, targetWCId, showScanResult, loadQueue, props.refreshHistory]);

  const handleDelete = useCallback(async (itemId: string) => {
    try {
      await materialQueueApi.deleteItem(targetWCId, itemId);
      showScanResult({ type: 'success', message: 'Item removed from queue' });
      loadQueue();
      props.refreshHistory();
    } catch {
      showScanResult({ type: 'error', message: 'Failed to remove item' });
    }
  }, [targetWCId, showScanResult, loadQueue, props.refreshHistory]);

  const selectedProduct = products.find((p) => p.id === form.productId);
  const selectedMill = mills.find((m) => m.id === form.vendorMillId);
  const selectedProcessor = processors.find((p) => p.id === form.vendorProcessorId);

  const selectionItems = selectingField === 'product' ? products.map((p) => ({ id: p.id, label: `(${p.tankSize}) ${p.productNumber}` }))
    : selectingField === 'mill' ? mills.map((m) => ({ id: m.id, label: m.name }))
    : selectingField === 'processor' ? processors.map((p) => ({ id: p.id, label: p.name }))
    : [];

  return (
    <div className={styles.container}>
      <div className={styles.queueHeader}>
        <h3 className={styles.queueTitle}>Material Queue for: Rolls</h3>
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
              <span className={styles.queueDesc}>{item.shellSize ? `(${item.shellSize}) ` : ''}{item.productDescription}</span>
              <span className={styles.queueMeta}>Qty: {item.quantity}{item.createdAt ? ` | ${new Date(item.createdAt).toLocaleTimeString()}` : ''}</span>
            </div>
            <div className={styles.queueActions}>
              <Button appearance="outline" size="large" className={styles.actionBtn} onClick={() => openEdit(item)}>✏️ Edit</Button>
              <Button appearance="outline" size="large" className={styles.actionBtn} onClick={() => handleDelete(item.id)}>🗑️ Delete</Button>
            </div>
          </div>
        ))
      )}

      {showForm && (
        <div className={styles.overlay} onClick={() => { if (!selectingField) setShowForm(false); }}>
          <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
            <h3 className={styles.formTitle}>{editingId ? 'Edit Material' : 'Add Material to Queue'}</h3>
            <div className={styles.formGrid}>
              <div className={styles.formField}>
                <Label required>Product</Label>
                <Button appearance="secondary" size="large" className={styles.selectBtn} onClick={() => setSelectingField('product')}>
                  {selectedProduct ? `(${selectedProduct.tankSize}) ${selectedProduct.productNumber}` : 'Select Product...'}
                </Button>
              </div>
              <div className={styles.formField}>
                <Label required>Plate Mill</Label>
                <Button appearance="secondary" size="large" className={styles.selectBtn} onClick={() => setSelectingField('mill')}>
                  {selectedMill ? selectedMill.name : 'Select Mill...'}
                </Button>
              </div>
              <div className={styles.formField}>
                <Label required>Plate Processor</Label>
                <Button appearance="secondary" size="large" className={styles.selectBtn} onClick={() => setSelectingField('processor')}>
                  {selectedProcessor ? selectedProcessor.name : 'Select Processor...'}
                </Button>
              </div>
              <div className={styles.formField}>
                <Label required>Heat Number</Label>
                <Input size="large" value={form.heatNumber} onChange={(_, d) => setForm((f) => ({ ...f, heatNumber: d.value }))} />
              </div>
              <div className={styles.formField}>
                <Label required>Coil Number</Label>
                <Input size="large" value={form.coilNumber} onChange={(_, d) => setForm((f) => ({ ...f, coilNumber: d.value }))} />
              </div>
              <div className={styles.formField}>
                <Label>Lot Number</Label>
                <Input size="large" value={form.lotNumber} onChange={(_, d) => setForm((f) => ({ ...f, lotNumber: d.value }))} />
              </div>
              <div className={styles.formField}>
                <Label required>Quantity</Label>
                <Input size="large" type="number" value={form.quantity} onChange={(_, d) => setForm((f) => ({ ...f, quantity: d.value }))} />
              </div>
            </div>
            <div className={styles.formActions}>
              <Button appearance="secondary" size="large" onClick={() => setShowForm(false)}>Cancel</Button>
              <Button appearance="primary" size="large" onClick={handleSave}>Save</Button>
            </div>
          </div>
        </div>
      )}

      {selectingField && (
        <div className={styles.overlay} onClick={() => setSelectingField(null)}>
          <div className={`${styles.popup} ${styles.popupWide}`} onClick={(e) => e.stopPropagation()}>
            <div className={styles.selectionPopup}>
              <div className={styles.selectionHeader}>
                <span>Select {selectingField === 'product' ? 'Product' : selectingField === 'mill' ? 'Plate Mill' : 'Plate Processor'}</span>
                <Button appearance="subtle" onClick={() => setSelectingField(null)}>Back</Button>
              </div>
              <div className={styles.selectionGrid}>
                {selectionItems.map((item) => (
                  <button
                    key={item.id}
                    className={styles.selectionTile}
                    onClick={() => {
                      if (selectingField === 'product') setForm((f) => ({ ...f, productId: item.id }));
                      else if (selectingField === 'mill') setForm((f) => ({ ...f, vendorMillId: item.id }));
                      else setForm((f) => ({ ...f, vendorProcessorId: item.id }));
                      setSelectingField(null);
                    }}
                  >
                    {item.label}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
