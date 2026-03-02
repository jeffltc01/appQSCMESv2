import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ProductListItem } from '../../types/domain.ts';
import { productApi, nameplateApi } from '../../api/endpoints.ts';
import styles from './NameplateScreen.module.css';

export function NameplateScreen(props: WorkCenterProps) {
  const {
    workCenterId,
    productionLineId,
    operatorId,
    plantId,
    plantCode,
    showScanResult,
    refreshHistory,
    selectedHistoryRecord,
    clearSelectedHistoryRecord,
  } = props;

  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [selectedProductId, setSelectedProductId] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [editingRecordId, setEditingRecordId] = useState<string | null>(null);

  const getSerialPrefixValidationError = useCallback((value: string): string | null => {
    const normalizedPlantCode = plantCode?.trim();
    const normalizedSerial = value.trim().toUpperCase();
    if (!normalizedSerial) return null;

    if (normalizedPlantCode === '700' && !normalizedSerial.startsWith('W')) {
      return 'West Jordan (700) serial numbers must start with W';
    }

    if (normalizedPlantCode === '600' && !normalizedSerial.startsWith('F')) {
      return 'Fremont (600) serial numbers must start with F';
    }

    return null;
  }, [plantCode]);

  useEffect(() => {
    loadProducts();
  }, [workCenterId, plantId]);

  const loadProducts = useCallback(async () => {
    try {
      const p = await productApi.getProducts('sellable', plantId || undefined);
      setProducts(p);
    } catch { /* keep empty */ }
  }, [plantId]);

  const handleSave = useCallback(async () => {
    if (!selectedProductId || (!editingRecordId && !serialNumber.trim())) {
      showScanResult({ type: 'error', message: 'Please fill all fields' });
      return;
    }

    const serialValidationError = getSerialPrefixValidationError(serialNumber);
    if (serialValidationError) {
      showScanResult({ type: 'error', message: serialValidationError });
      return;
    }

    try {
      if (editingRecordId) {
        const result = await nameplateApi.update(editingRecordId, {
          productId: selectedProductId,
          operatorId,
        });
        if (result.printSucceeded) {
          showScanResult({ type: 'success', message: `Serial ${serialNumber.trim()} updated. Label reprinting.` });
        } else {
          showScanResult({ type: 'warning', message: `Serial updated but print failed: ${result.printMessage ?? 'Unknown error'}` });
        }
      } else {
        const result = await nameplateApi.create({
          serialNumber: serialNumber.trim(),
          productId: selectedProductId,
          workCenterId,
          productionLineId,
          operatorId,
        });
        if (result.printSucceeded) {
          showScanResult({ type: 'success', message: `Serial ${serialNumber.trim()} saved. Label printing.` });
        } else {
          showScanResult({ type: 'warning', message: `Serial saved but print failed: ${result.printMessage ?? 'Unknown error'}` });
        }
      }
      refreshHistory();
      setSerialNumber('');
      setEditingRecordId(null);
      clearSelectedHistoryRecord?.();
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? (editingRecordId ? 'Failed to update nameplate record' : 'Failed to save nameplate record') });
    }
  }, [selectedProductId, serialNumber, editingRecordId, getSerialPrefixValidationError, workCenterId, productionLineId, operatorId, showScanResult, refreshHistory, clearSelectedHistoryRecord]);

  useEffect(() => {
    if (!selectedHistoryRecord?.serialNumberId) return;
    setEditingRecordId(selectedHistoryRecord.serialNumberId);
    setSerialNumber(selectedHistoryRecord.serialOrIdentifier);
    if (selectedHistoryRecord.productId) {
      setSelectedProductId(selectedHistoryRecord.productId);
    }
  }, [selectedHistoryRecord]);

  const handleCancelEdit = useCallback(() => {
    setEditingRecordId(null);
    setSerialNumber('');
    clearSelectedHistoryRecord?.();
  }, [clearSelectedHistoryRecord]);

  const selectedProduct = products.find((p) => p.id === selectedProductId);

  return (
    <div className={styles.container}>
      <div className={styles.form}>
        <div className={styles.formField}>
          <Label required>Tank Size / Type</Label>
          <Dropdown
            placeholder="Select product"
            value={selectedProduct ? selectedProduct.productNumber : ''}
            selectedOptions={selectedProductId ? [selectedProductId] : []}
            onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setSelectedProductId(d.optionValue); }}
            className={styles.dropdown}
          >
            {products.map((p) => (
              <Option key={p.id} value={p.id} text={p.productNumber}>{p.productNumber}</Option>
            ))}
          </Dropdown>
        </div>

        <div className={styles.formField}>
          <Label required>Serial Number</Label>
          <Input
            value={serialNumber}
            onChange={(_, d) => setSerialNumber(d.value)}
            placeholder="Enter serial number (e.g. W00123456)"
            size="large"
            className={styles.input}
            onKeyDown={(e) => { if (e.key === 'Enter') handleSave(); }}
            disabled={!!editingRecordId}
          />
        </div>

        <Button
          appearance="primary"
          size="large"
          className={styles.submitBtn}
          onClick={handleSave}
          disabled={!selectedProductId || (!editingRecordId && !serialNumber.trim())}
        >
          {editingRecordId ? 'Update & Reprint' : 'Save'}
        </Button>

        {editingRecordId && (
          <Button appearance="secondary" size="large" className={styles.cancelBtn} onClick={handleCancelEdit}>
            Cancel Edit
          </Button>
        )}
      </div>
    </div>
  );
}
