import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { XrayQueueItem } from '../../types/domain.ts';
import { xrayQueueApi } from '../../api/endpoints.ts';
import { formatTimeOnly } from '../../utils/dateFormat.ts';
import styles from './RtXrayQueueScreen.module.css';

const MAX_QUEUE_ITEMS = 5;

export function RtXrayQueueScreen(props: WorkCenterProps) {
  const { workCenterId, operatorId, showScanResult, registerBarcodeHandler } = props;

  const [queue, setQueue] = useState<XrayQueueItem[]>([]);
  const [manualSerial, setManualSerial] = useState('');
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);
  const isQueueFull = queue.length >= MAX_QUEUE_ITEMS;

  useEffect(() => {
    loadQueue();
  }, [workCenterId]);

  const loadQueue = useCallback(async () => {
    try {
      const items = await xrayQueueApi.getQueue(workCenterId);
      setQueue(items);
    } catch { /* keep stale */ }
  }, [workCenterId]);

  const addToQueue = useCallback(async (serial: string) => {
    if (isQueueFull) {
      showScanResult({ type: 'error', message: `Queue is full (max ${MAX_QUEUE_ITEMS} items)` });
      return;
    }

    try {
      await xrayQueueApi.addItem(workCenterId, { serialNumber: serial, operatorId });
      showScanResult({ type: 'success', message: `Shell ${serial} added to queue` });
      loadQueue();
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to add to queue' });
    }
  }, [isQueueFull, workCenterId, operatorId, showScanResult, loadQueue]);

  const confirmDelete = useCallback(async () => {
    if (!pendingDeleteId) return;
    try {
      await xrayQueueApi.removeItem(workCenterId, pendingDeleteId);
      showScanResult({ type: 'success', message: 'Removed from queue' });
      loadQueue();
    } catch {
      showScanResult({ type: 'error', message: 'Failed to remove' });
    } finally {
      setPendingDeleteId(null);
    }
  }, [pendingDeleteId, workCenterId, showScanResult, loadQueue]);

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
      {isQueueFull && <div className={styles.emptyQueue}>Queue is full (max {MAX_QUEUE_ITEMS} items)</div>}

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
            <Button appearance="primary" size="large" onClick={handleManualSubmit} disabled={!manualSerial.trim() || isQueueFull}>
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
                <span className={styles.queueTime}>{formatTimeOnly(item.createdAt)}</span>
              </div>
              <Button appearance="subtle" size="small" onClick={() => setPendingDeleteId(item.id)}>🗑</Button>
            </div>
          ))
        )}
      </div>

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
    </div>
  );
}
