import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { XrayQueueItem } from '../../types/domain.ts';
import { xrayQueueApi } from '../../api/endpoints.ts';
import styles from './RtXrayQueueScreen.module.css';

export function RtXrayQueueScreen(props: WorkCenterProps) {
  const { workCenterId, operatorId, showScanResult, registerBarcodeHandler, setRequiresWelder } = props;

  const [queue, setQueue] = useState<XrayQueueItem[]>([]);
  const [manualSerial, setManualSerial] = useState('');

  useEffect(() => {
    setRequiresWelder(false);
    loadQueue();
  }, [workCenterId]);

  const loadQueue = useCallback(async () => {
    try {
      const items = await xrayQueueApi.getQueue(workCenterId);
      setQueue(items);
    } catch { /* keep stale */ }
  }, [workCenterId]);

  const addToQueue = useCallback(async (serial: string) => {
    try {
      await xrayQueueApi.addItem(workCenterId, { serialNumber: serial, operatorId });
      showScanResult({ type: 'success', message: `Shell ${serial} added to queue` });
      loadQueue();
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to add to queue' });
    }
  }, [workCenterId, operatorId, showScanResult, loadQueue]);

  const removeFromQueue = useCallback(async (itemId: string) => {
    try {
      await xrayQueueApi.removeItem(workCenterId, itemId);
      showScanResult({ type: 'success', message: 'Removed from queue' });
      loadQueue();
    } catch {
      showScanResult({ type: 'error', message: 'Failed to remove' });
    }
  }, [workCenterId, showScanResult, loadQueue]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) { showScanResult({ type: 'error', message: 'Unknown barcode' }); return; }
      if (bc.prefix === 'SC') {
        const { serialNumber } = parseShellLabel(bc.value);
        addToQueue(serialNumber);
        return;
      }
      showScanResult({ type: 'error', message: 'Scan a shell barcode to add to queue' });
    },
    [addToQueue, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualSubmit = useCallback(() => {
    if (manualSerial.trim()) {
      addToQueue(manualSerial.trim());
      setManualSerial('');
    }
  }, [manualSerial, addToQueue]);

  return (
    <div className={styles.container}>
      <div className={styles.queueHeader}>
        <h3 className={styles.queueTitle}>Queue for: Real Time X-ray</h3>
        <Button appearance="subtle" onClick={loadQueue}>Refresh</Button>
      </div>

      {!props.externalInput && (
        <div className={styles.addRow}>
          <Label>Add Shell to Queue</Label>
          <div className={styles.inputRow}>
            <Input
              value={manualSerial}
              onChange={(_, d) => setManualSerial(d.value)}
              placeholder="Enter serial number..."
              size="large"
              className={styles.input}
              onKeyDown={(e) => { if (e.key === 'Enter') handleManualSubmit(); }}
            />
            <Button appearance="primary" size="large" onClick={handleManualSubmit} disabled={!manualSerial.trim()}>
              Add
            </Button>
          </div>
        </div>
      )}

      <div className={styles.queueList}>
        {queue.length === 0 ? (
          <div className={styles.emptyQueue}>Queue is empty</div>
        ) : (
          queue.map((item) => (
            <div key={item.id} className={styles.queueCard}>
              <div className={styles.queueInfo}>
                <span className={styles.queueSerial}>{item.serialNumber}</span>
                <span className={styles.queueTime}>{new Date(item.createdAt).toLocaleTimeString()}</span>
              </div>
              <Button appearance="subtle" size="small" onClick={() => removeFromQueue(item.id)}>🗑</Button>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
