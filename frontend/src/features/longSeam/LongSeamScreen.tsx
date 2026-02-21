import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import { productionRecordApi } from '../../api/endpoints.ts';
import styles from './LongSeamScreen.module.css';

export function LongSeamScreen(props: WorkCenterProps) {
  const {
    workCenterId, assetId, productionLineId, operatorId, welders,
    showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [manualSerial, setManualSerial] = useState('');

  const recordShell = useCallback(
    async (serial: string) => {
      try {
        const resp = await productionRecordApi.create({
          serialNumber: serial,
          workCenterId,
          assetId: assetId || undefined,
          productionLineId,
          operatorId,
          welderIds: welders.map((w) => w.userId),
        });
        showScanResult({
          type: 'success',
          message: resp.warning
            ? `Shell ${serial} recorded (${resp.warning})`
            : `Shell ${serial} recorded`,
        });
        refreshHistory();
      } catch (err: unknown) {
        const msg = (err as { message?: string })?.message ?? 'Failed to save record. Please try again.';
        showScanResult({ type: 'error', message: msg });
      }
    },
    [workCenterId, assetId, productionLineId, operatorId, welders, showScanResult, refreshHistory],
  );

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc || bc.prefix !== 'SC') {
        showScanResult({ type: 'error', message: 'Scan a shell label to record' });
        return;
      }
      const { serialNumber } = parseShellLabel(bc.value);
      recordShell(serialNumber);
    },
    [recordShell, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualSubmit = useCallback(() => {
    if (!manualSerial.trim()) return;
    recordShell(manualSerial.trim());
    setManualSerial('');
  }, [manualSerial, recordShell]);

  return (
    <div className={styles.container}>
      <div className={styles.prompt}>Scan Serial Number to begin...</div>

      <div className={styles.form}>
        <Label htmlFor="ls-serial" className={styles.label}>
          Serial Number
        </Label>
        <Input
          id="ls-serial"
          value={manualSerial}
          onChange={(_, d) => setManualSerial(d.value)}
          placeholder="enter serial number"
          size="large"
          className={styles.input}
          onKeyDown={(e) => { if (e.key === 'Enter') handleManualSubmit(); }}
          disabled={props.externalInput}
        />
        <Button
          appearance="primary"
          size="large"
          className={styles.submitBtn}
          onClick={handleManualSubmit}
          disabled={props.externalInput || !manualSerial.trim()}
        >
          Submit
        </Button>
      </div>
    </div>
  );
}
