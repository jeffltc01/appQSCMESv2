import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ProductListItem } from '../../types/domain.ts';
import { productApi, nameplateApi } from '../../api/endpoints.ts';
import styles from './NameplateScreen.module.css';

export function NameplateScreen(props: WorkCenterProps) {
  const { workCenterId, operatorId, showScanResult, refreshHistory } = props;

  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [selectedProductId, setSelectedProductId] = useState('');
  const [serialNumber, setSerialNumber] = useState('');

  useEffect(() => {
    loadProducts();
  }, [workCenterId]);

  const loadProducts = useCallback(async () => {
    try {
      const p = await productApi.getProducts('sellable');
      setProducts(p);
    } catch { /* keep empty */ }
  }, []);

  const handleSave = useCallback(async () => {
    if (!selectedProductId || !serialNumber.trim()) {
      showScanResult({ type: 'error', message: 'Please fill all fields' });
      return;
    }
    try {
      await nameplateApi.create({
        serialNumber: serialNumber.trim(),
        productId: selectedProductId,
        workCenterId,
        operatorId,
      });
      showScanResult({ type: 'success', message: `Serial ${serialNumber.trim()} saved. Label printing.` });
      refreshHistory();
      setSelectedProductId('');
      setSerialNumber('');
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to save nameplate record' });
    }
  }, [selectedProductId, serialNumber, workCenterId, operatorId, showScanResult, refreshHistory]);

  const selectedProduct = products.find((p) => p.id === selectedProductId);

  return (
    <div className={styles.container}>
      <div className={styles.form}>
        <div className={styles.formField}>
          <Label required>Tank Size / Type</Label>
          <Dropdown
            placeholder="Select tank size/type"
            value={selectedProduct ? `${selectedProduct.tankSize} ${selectedProduct.tankType}` : ''}
            selectedOptions={selectedProductId ? [selectedProductId] : []}
            onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setSelectedProductId(d.optionValue); }}
            className={styles.dropdown}
          >
            {products.map((p) => (
              <Option key={p.id} value={p.id} text={`${p.tankSize} ${p.tankType}`}>{`${p.tankSize} ${p.tankType}`}</Option>
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
