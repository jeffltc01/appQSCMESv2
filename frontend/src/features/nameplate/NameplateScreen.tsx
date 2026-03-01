import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ProductListItem } from '../../types/domain.ts';
import { productApi, nameplateApi } from '../../api/endpoints.ts';
import styles from './NameplateScreen.module.css';

export function NameplateScreen(props: WorkCenterProps) {
  const { workCenterId, productionLineId, operatorId, plantId, plantCode, showScanResult, refreshHistory } = props;

  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [selectedProductId, setSelectedProductId] = useState('');
  const [serialNumber, setSerialNumber] = useState('');

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
    if (!selectedProductId || !serialNumber.trim()) {
      showScanResult({ type: 'error', message: 'Please fill all fields' });
      return;
    }

    const serialValidationError = getSerialPrefixValidationError(serialNumber);
    if (serialValidationError) {
      showScanResult({ type: 'error', message: serialValidationError });
      return;
    }

    try {
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
      refreshHistory();
      setSerialNumber('');
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to save nameplate record' });
    }
  }, [selectedProductId, serialNumber, getSerialPrefixValidationError, workCenterId, operatorId, showScanResult, refreshHistory]);

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
          />
        </div>

        <Button
          appearance="primary"
          size="large"
          className={styles.submitBtn}
          onClick={handleSave}
          disabled={!selectedProductId || !serialNumber.trim()}
        >
          Save
        </Button>
      </div>
    </div>
  );
}
