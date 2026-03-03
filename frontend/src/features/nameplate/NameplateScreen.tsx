import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import type { ProductListItem } from '../../types/domain.ts';
import { productApi, nameplateApi } from '../../api/endpoints.ts';
import { NextStepBanner } from '../../components/nextStep/NextStepBanner.tsx';
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
    registerBarcodeHandler,
    externalInput,
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

  const saveRecord = useCallback(async (serialOverride?: string) => {
    const serialToSave = (serialOverride ?? serialNumber).trim();
    if (!selectedProductId || (!editingRecordId && !serialToSave)) {
      showScanResult({ type: 'error', message: 'Please fill all fields' });
      return;
    }

    const serialValidationError = getSerialPrefixValidationError(serialToSave);
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
          showScanResult({ type: 'success', message: `Serial ${serialToSave} updated. Label reprinting.` });
        } else {
          showScanResult({ type: 'warning', message: `Serial updated but print failed: ${result.printMessage ?? 'Unknown error'}` });
        }
      } else {
        const result = await nameplateApi.create({
          serialNumber: serialToSave,
          productId: selectedProductId,
          workCenterId,
          productionLineId,
          operatorId,
        });
        if (result.printSucceeded) {
          showScanResult({ type: 'success', message: `Serial ${serialToSave} saved. Label printing.` });
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

  const handleSave = useCallback(async () => {
    await saveRecord();
  }, [saveRecord]);

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
  const nextInstruction = (() => {
    if (externalInput) {
      if (editingRecordId) {
        return {
          title: 'Toggle External Input off to edit this record',
          detail: 'Editing existing records requires manual mode.',
          isActive: true,
        };
      }
      return {
        title: selectedProductId
          ? 'Scan finished serial barcode'
          : 'Select Tank Size / Type, then scan finished serial barcode',
        detail: 'Scanning auto-saves and prints the barcode label.',
        isActive: true,
      };
    }
    return {
      title: 'Select Tank Size / Type, enter serial, then Save',
      detail: '',
      isActive: false,
    };
  })();

  const handleBarcode = useCallback((bc: ParsedBarcode | null, raw: string) => {
    if (!externalInput) return;
    if (editingRecordId) {
      showScanResult({ type: 'error', message: 'Finish or cancel edit before scanning a new serial' });
      return;
    }
    if (!selectedProductId) {
      showScanResult({ type: 'error', message: 'Select Tank Size / Type before scanning' });
      return;
    }

    // Nameplate scanner labels contain the finished serial directly (no prefix).
    const scannedSerial = !bc ? raw.trim() : bc.prefix === 'SC' ? bc.value.trim() : '';
    if (!scannedSerial) {
      showScanResult({ type: 'error', message: 'Scan a valid nameplate serial barcode' });
      return;
    }

    setSerialNumber(scannedSerial);
    void saveRecord(scannedSerial);
  }, [externalInput, editingRecordId, selectedProductId, showScanResult, saveRecord]);

  const barcodeHandlerRef = useRef(handleBarcode);
  barcodeHandlerRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => barcodeHandlerRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  return (
    <div className={styles.container}>
      <NextStepBanner instruction={nextInstruction} />
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

        {!externalInput && (
          <>
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
          </>
        )}
      </div>
    </div>
  );
}
