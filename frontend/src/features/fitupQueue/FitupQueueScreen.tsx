import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import type { BarcodeCardInfo, MaterialQueueItem, ProductListItem, Vendor } from '../../types/domain.ts';
import { workCenterApi, materialQueueApi, productApi, vendorApi, barcodeCardApi } from '../../api/endpoints.ts';
import { useAuth } from '../../auth/AuthContext.tsx';
import { formatTimeOnly } from '../../utils/dateFormat.ts';
import { reportQueueFlowTelemetry } from '../../telemetry/telemetryClient.ts';
import { clearRetryContext, loadRetryContext, saveRetryContext } from '../shared/queueRetryContext.ts';
import {
  idleActionFeedbackState,
  runActionWithSloFeedback,
  type ActionFeedbackState,
} from '../shared/actionSloFeedback.ts';
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

const MAX_QUEUE_ITEMS = 5;
const RETRY_SCOPE = 'fitup_queue';

const emptyForm: FormData = {
  productId: '', vendorHeadId: '', lotNumber: '',
  heatNumber: '', coilSlabNumber: '', cardCode: '', cardColor: '',
};

function resolveColorName(color?: string): string | null {
  if (!color) return null;
  const compact = color.toLowerCase().replace(/\s+/g, '');
  const known: Record<string, string> = {
    red: 'Red',
    '#ff0000': 'Red',
    '#f00': 'Red',
    'rgb(255,0,0)': 'Red',
    yellow: 'Yellow',
    '#ffff00': 'Yellow',
    '#ff0': 'Yellow',
    'rgb(255,255,0)': 'Yellow',
    blue: 'Blue',
    '#0000ff': 'Blue',
    '#00f': 'Blue',
    'rgb(0,0,255)': 'Blue',
    green: 'Green',
    '#008000': 'Green',
    '#00ff00': 'Green',
    '#0f0': 'Green',
    'rgb(0,128,0)': 'Green',
    'rgb(0,255,0)': 'Green',
    orange: 'Orange',
    '#ffa500': 'Orange',
    purple: 'Purple',
    '#800080': 'Purple',
    gray: 'Gray',
    grey: 'Gray',
    '#808080': 'Gray',
    black: 'Black',
    '#000000': 'Black',
    '#000': 'Black',
    white: 'White',
    '#ffffff': 'White',
    '#fff': 'White',
  };
  return known[compact] ?? null;
}

function buildCardLabel(cardValue: string, colorName?: string, color?: string): string {
  const resolvedName = colorName?.trim() || resolveColorName(color);
  return resolvedName ? `${cardValue} - ${resolvedName}` : cardValue;
}

export function FitupQueueScreen(props: WorkCenterProps) {
  const { workCenterId, showScanResult, registerBarcodeHandler, productionLineId } = props;
  const { user } = useAuth();
  const queueWCId = workCenterId;

  const [queue, setQueue] = useState<MaterialQueueItem[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormData>(emptyForm);
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [vendors, setVendors] = useState<Vendor[]>([]);
  const [cards, setCards] = useState<BarcodeCardInfo[]>([]);
  const [selectingField, setSelectingField] = useState<'product' | 'vendor' | null>(null);
  const [selectedVendorType, setSelectedVendorType] = useState<'cmf' | 'compco' | null>(null);
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);
  const [saveFeedback, setSaveFeedback] = useState<ActionFeedbackState>(idleActionFeedbackState);
  const isQueueFull = queue.length >= MAX_QUEUE_ITEMS;

  const loadQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(queueWCId, 'fitup', productionLineId);
      setQueue(items.filter((i) => i.status === 'queued'));
    } catch { /* keep stale */ }
  }, [queueWCId, productionLineId]);

  const loadLookups = useCallback(async () => {
    try {
      const plantId = user?.defaultSiteId;
      const [p, v, c] = await Promise.all([
        productApi.getProducts('head', plantId),
        vendorApi.getVendors('head', plantId),
        barcodeCardApi.getCards(queueWCId, plantId),
      ]);
      setProducts(p);
      setVendors(v);
      setCards(c);
    } catch { /* keep empty */ }
  }, [queueWCId, user?.defaultSiteId]);

  useEffect(() => {
    loadQueue();
    loadLookups();
  }, [workCenterId, loadQueue, loadLookups]);

  useEffect(() => {
    const restored = loadRetryContext(RETRY_SCOPE, queueWCId);
    if (!restored) {
      return;
    }

    setForm((prev) => ({
      ...prev,
      productId: restored.productId ?? prev.productId,
      vendorHeadId: restored.vendorHeadId ?? prev.vendorHeadId,
      lotNumber: restored.lotNumber ?? prev.lotNumber,
      heatNumber: restored.heatNumber ?? prev.heatNumber,
      coilSlabNumber: restored.coilSlabNumber ?? prev.coilSlabNumber,
      cardCode: restored.cardCode ?? prev.cardCode,
      cardColor: restored.cardColor ?? prev.cardColor,
    }));
    setShowForm(true);
  }, [queueWCId]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) { showScanResult({ type: 'error', message: 'Unknown barcode' }); return; }
      if (bc.prefix === 'KC' && showForm) {
        const scannedCode = bc.value.trim();
        const card = cards.find((c) => c.cardValue === scannedCode);
        if (!card) {
          showScanResult({ type: 'error', message: `Card ${scannedCode} not recognized` });
          return;
        }
        setForm((f) => ({ ...f, cardCode: card.cardValue, cardColor: card.color ?? '' }));
        showScanResult({ type: 'success', message: `Card ${bc.value} scanned` });
        return;
      }
      showScanResult({ type: 'error', message: 'Scan a Queue Card barcode (KC;XX)' });
    },
    [cards, showForm, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const openAdd = useCallback(() => {
    if (isQueueFull) {
      showScanResult({ type: 'error', message: `Queue is full (max ${MAX_QUEUE_ITEMS} items)` });
      return;
    }
    setForm(emptyForm);
    setEditingId(null);
    setSelectedVendorType(null);
    setShowForm(true);
  }, [isQueueFull, showScanResult]);

  const handleSave = useCallback(async () => {
    if (!editingId && isQueueFull) {
      showScanResult({ type: 'error', message: `Queue is full (max ${MAX_QUEUE_ITEMS} items)` });
      return;
    }

    if (!form.productId || !form.vendorHeadId || !form.cardCode) {
      showScanResult({ type: 'error', message: 'Please fill all required fields' });
      return;
    }
    const sequenceId = Date.now();
    try {
      await runActionWithSloFeedback(
        editingId ? 'fitup_queue_update' : 'fitup_queue_create',
        { screen: 'FitupQueue', workCenterId: queueWCId, mode: editingId ? 'edit' : 'create' },
        setSaveFeedback,
        async () => {
          if (editingId) {
            await materialQueueApi.updateFitupItem(queueWCId, editingId, {
              productId: form.productId,
              vendorHeadId: form.vendorHeadId,
              lotNumber: form.lotNumber || undefined,
              heatNumber: form.heatNumber || undefined,
              coilSlabNumber: form.coilSlabNumber || undefined,
              cardCode: form.cardCode,
            });
            showScanResult({ type: 'success', message: 'Queue item updated' });
            return;
          }
          await materialQueueApi.addFitupItem(queueWCId, {
            productId: form.productId,
            vendorHeadId: form.vendorHeadId,
            lotNumber: form.lotNumber || undefined,
            heatNumber: form.heatNumber || undefined,
            coilSlabNumber: form.coilSlabNumber || undefined,
            cardCode: form.cardCode,
            productionLineId: productionLineId || undefined,
          });
          showScanResult({ type: 'success', message: 'Head material added to queue' });
        },
      );
      clearRetryContext(RETRY_SCOPE);
      reportQueueFlowTelemetry('queue_submit_success', {
        screen: 'FitupQueue',
        workCenterId: queueWCId,
        mode: editingId ? 'edit' : 'create',
        sequenceId,
      });
      setShowForm(false);
      loadQueue();
      props.refreshHistory();
    } catch (err: any) {
      saveRetryContext(RETRY_SCOPE, queueWCId, {
        productId: form.productId,
        vendorHeadId: form.vendorHeadId,
        lotNumber: form.lotNumber,
        heatNumber: form.heatNumber,
        coilSlabNumber: form.coilSlabNumber,
        cardCode: form.cardCode,
        cardColor: form.cardColor,
      });
      reportQueueFlowTelemetry('queue_submit_failed_context_preserved', {
        screen: 'FitupQueue',
        workCenterId: queueWCId,
        mode: editingId ? 'edit' : 'create',
        error: err?.message ?? 'Failed to save',
        hasContext: true,
        sequenceId,
      });
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to save' });
    }
  }, [form, editingId, isQueueFull, queueWCId, showScanResult, loadQueue, props.refreshHistory]);

  const confirmDelete = useCallback(async () => {
    if (!pendingDeleteId) return;
    try {
      await materialQueueApi.deleteFitupItem(queueWCId, pendingDeleteId);
      showScanResult({ type: 'success', message: 'Item removed' });
      loadQueue();
      props.refreshHistory();
    } catch {
      showScanResult({ type: 'error', message: 'Failed to remove' });
    } finally {
      setPendingDeleteId(null);
    }
  }, [pendingDeleteId, queueWCId, showScanResult, loadQueue, props.refreshHistory]);

  const openEdit = useCallback((item: MaterialQueueItem) => {
    const vendorId = item.vendorHeadId ?? '';
    const vendor = vendors.find((v) => v.id === vendorId);
    let inferredVendorType: 'cmf' | 'compco' | null = null;
    if (vendor?.name?.toLowerCase().includes('cmf')) {
      inferredVendorType = 'cmf';
    } else if (vendorId) {
      inferredVendorType = 'compco';
    } else if (item.lotNumber) {
      inferredVendorType = 'cmf';
    } else if (item.heatNumber || item.coilNumber) {
      inferredVendorType = 'compco';
    }

    setForm({
      productId: item.productId ?? '',
      vendorHeadId: vendorId,
      lotNumber: item.lotNumber ?? '',
      heatNumber: item.heatNumber ?? '',
      coilSlabNumber: item.coilNumber ?? '',
      cardCode: item.cardId ?? '',
      cardColor: item.cardColor ?? '',
    });
    setEditingId(item.id);
    setSelectedVendorType(inferredVendorType);
    setShowForm(true);
  }, [vendors]);

  const selectedProduct = products.find((p) => p.id === form.productId);
  const selectedVendor = vendors.find((v) => v.id === form.vendorHeadId);
  const selectedCard = cards.find((c) => c.cardValue === form.cardCode);
  const isCmf = selectedVendorType === 'cmf' || (selectedVendor?.name?.toLowerCase().includes('cmf') ?? false);
  const selectedCardLabel = selectedCard
    ? buildCardLabel(selectedCard.cardValue, selectedCard.colorName, selectedCard.color)
    : buildCardLabel(form.cardCode, undefined, form.cardColor);

  const selectionItems = selectingField === 'product'
    ? products.map((p) => ({ id: p.id, label: `(${p.tankSize}) ${p.productNumber}` }))
    : selectingField === 'vendor' ? vendors.map((v) => ({ id: v.id, label: v.name }))
    : [];

  return (
    <div className={styles.container}>
      <div className={styles.queueHeader}>
        <h3 className={styles.queueTitle}>Material Queue for: Fitup</h3>
        <div className={styles.headerActions}>
          <Button appearance="primary" size="large" onClick={openAdd} disabled={isQueueFull}>Add Material to Queue</Button>
          <Button appearance="outline" size="large" onClick={loadQueue}>Refresh</Button>
        </div>
      </div>
      {isQueueFull && <div className={styles.emptyQueue}>Queue is full (max {MAX_QUEUE_ITEMS} items)</div>}
      <div className={styles.queueListPanel}>
        {queue.length === 0 ? (
          <div className={styles.emptyQueue}>No material in queue</div>
        ) : (
          queue.map((item) => (
            <div key={item.id} className={styles.queueCard}>
              <span
                className={styles.cardColorSwatch}
                style={{ backgroundColor: item.cardColor || '#dee2e6' }}
                title={item.cardColor || 'No color'}
              />
              <div className={styles.queueInfo}>
                <span className={styles.queueDesc}>{item.shellSize ? `(${item.shellSize}) ` : ''}{item.productDescription}</span>
                <span className={styles.queueTrace}>
                  {item.lotNumber
                    ? `Lot ${item.lotNumber}`
                    : `Heat ${item.heatNumber || '—'}  Coil/Slab ${item.coilNumber || '—'}`}
                </span>
                <div className={styles.queueMeta}>
                  <span>Card {item.cardId ?? '—'}</span>
                  {item.createdAt && <span>{formatTimeOnly(item.createdAt)}</span>}
                </div>
              </div>
              <div className={styles.queueActions}>
                <Button appearance="outline" size="large" className={styles.actionBtn} onClick={() => openEdit(item)}>✏️ Edit</Button>
                <Button appearance="outline" size="large" className={styles.actionBtn} onClick={() => setPendingDeleteId(item.id)}>🗑️ Delete</Button>
              </div>
            </div>
          ))
        )}
      </div>

      {showForm && (
        <div className={styles.overlay} onClick={() => { if (!selectingField) setShowForm(false); }}>
          <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
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
                  <Dropdown
                    className={styles.selectBtn}
                    value={selectedCardLabel}
                    selectedOptions={form.cardCode ? [form.cardCode] : []}
                    onOptionSelect={(_, d: OptionOnSelectData) => {
                      const selectedCode = d.optionValue ?? '';
                      const card = cards.find((c) => c.cardValue === selectedCode);
                      setForm((f) => ({ ...f, cardCode: selectedCode, cardColor: card?.color ?? '' }));
                    }}
                    placeholder="Select Card..."
                  >
                    {cards.map((card) => (
                      <Option
                        key={card.id}
                        value={card.cardValue}
                        text={buildCardLabel(card.cardValue, card.colorName, card.color)}
                      >
                        <span className={styles.cardOption}>
                          <span>{buildCardLabel(card.cardValue, card.colorName, card.color)}</span>
                          {card.color && (
                            <span
                              className={styles.optionColorSwatch}
                              style={{ backgroundColor: card.color }}
                              aria-hidden="true"
                            />
                          )}
                        </span>
                      </Option>
                    ))}
                    {form.cardCode && !selectedCard && (
                      <Option value={form.cardCode} text={form.cardCode}>{form.cardCode}</Option>
                    )}
                  </Dropdown>
                  {form.cardColor && <span className={styles.colorSwatch} style={{ backgroundColor: form.cardColor }} />}
                </div>
              </div>
            </div>
            <div className={styles.formActions}>
              <Button appearance="secondary" size="large" onClick={() => setShowForm(false)}>Cancel</Button>
              <Button appearance="primary" size="large" onClick={handleSave} disabled={saveFeedback.isPending}>Save</Button>
            </div>
            {saveFeedback.showProcessing && (
              <div className={styles.emptyQueue}>Processing takes longer than expected...</div>
            )}
            {saveFeedback.showRetryGuidance && (
              <div className={styles.emptyQueue}>
                Still working. If this fails, retry with your current values preserved.
              </div>
            )}
          </div>
        </div>
      )}

      {pendingDeleteId && (
        <div className={styles.overlay} onClick={() => setPendingDeleteId(null)}>
          <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
            <h3 className={styles.formTitle}>Remove from queue?</h3>
            <div className={styles.formActions}>
              <Button appearance="secondary" size="large" onClick={() => setPendingDeleteId(null)}>Cancel</Button>
              <Button appearance="primary" size="large" onClick={confirmDelete}>Yes, Remove</Button>
            </div>
          </div>
        </div>
      )}

      {selectingField && (
        <div className={styles.overlay} onClick={() => setSelectingField(null)}>
          <div className={`${styles.popup} ${styles.popupWide}`} onClick={(e) => e.stopPropagation()}>
            <div className={styles.selectionPopup}>
              <div className={styles.selectionHeader}>
                <span>Select {selectingField === 'product' ? 'Product' : 'Head Vendor'}</span>
                <Button appearance="subtle" onClick={() => setSelectingField(null)}>Back</Button>
              </div>
              <div className={styles.selectionGrid}>
                {selectionItems.map((item) => (
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
          </div>
        </div>
      )}
    </div>
  );
}
