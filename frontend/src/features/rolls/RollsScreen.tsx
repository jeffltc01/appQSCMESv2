import { useState, useEffect, useCallback } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { MaterialQueueItem, } from '../../types/domain.ts';
import type { QueueAdvanceResponse } from '../../types/api.ts';
import { workCenterApi, productionRecordApi } from '../../api/endpoints.ts';
import styles from './RollsScreen.module.css';

type ScanState = 'idle' | 'scanLabel1' | 'scanLabel2';
type PromptState = 'none' | 'advanceQueue' | 'thicknessInspection';

interface ActiveMaterial {
  shellSize: string;
  heatNumber: string;
  coilNumber: string;
  queueQuantity: number;
  materialRemaining: number;
  shellCount: number;
  productDescription: string;
}

export function RollsScreen(props: WorkCenterProps) {
  const {
    workCenterId, assetId, productionLineId, operatorId, welders,
    showScanResult, refreshHistory, registerBarcodeHandler, setRequiresWelder,
  } = props;

  const [scanState, setScanState] = useState<ScanState>('idle');
  const [promptState, setPromptState] = useState<PromptState>('none');
  const [label1Serial, setLabel1Serial] = useState('');
  const [activeMaterial, setActiveMaterial] = useState<ActiveMaterial | null>(null);
  const [thicknessInspectionRequired, setThicknessInspectionRequired] = useState(false);
  const [queue, setQueue] = useState<MaterialQueueItem[]>([]);
  const [manualSerial, setManualSerial] = useState('');

  useEffect(() => {
    setRequiresWelder(true);
    loadQueue();
  }, [workCenterId]);

  useEffect(() => {
    registerBarcodeHandler(handleBarcode);
  }, [scanState, promptState, activeMaterial, label1Serial, thicknessInspectionRequired]);

  const loadQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(workCenterId);
      setQueue(items.filter((i) => i.status === 'queued'));
    } catch { /* keep stale */ }
  }, [workCenterId]);

  const advanceQueue = useCallback(async () => {
    try {
      const data: QueueAdvanceResponse = await workCenterApi.advanceQueue(workCenterId);
      setActiveMaterial({
        shellSize: data.shellSize,
        heatNumber: data.heatNumber,
        coilNumber: data.coilNumber,
        queueQuantity: data.quantity,
        materialRemaining: data.quantity,
        shellCount: 0,
        productDescription: data.productDescription,
      });
      setScanState('scanLabel1');
      setThicknessInspectionRequired(true);
      setPromptState('none');
      loadQueue();
      showScanResult({ type: 'success', message: 'Queue advanced' });
    } catch {
      showScanResult({ type: 'error', message: 'No material in queue. Contact Material Handling.' });
    }
  }, [workCenterId, loadQueue, showScanResult]);

  const createRecord = useCallback(
    async (serial: string, inspResult?: 'pass' | 'fail') => {
      try {
        const resp = await productionRecordApi.create({
          serialNumber: serial,
          workCenterId,
          assetId,
          productionLineId,
          operatorId,
          welderIds: welders.map((w) => w.userId),
          inspectionResult: inspResult,
          shellSize: activeMaterial?.shellSize,
          heatNumber: activeMaterial?.heatNumber,
          coilNumber: activeMaterial?.coilNumber,
        });
        showScanResult({
          type: 'success',
          message: resp.warning
            ? `Shell ${serial} recorded (${resp.warning})`
            : `Shell ${serial} recorded`,
        });
        setActiveMaterial((prev) =>
          prev
            ? {
                ...prev,
                shellCount: prev.shellCount + 1,
                materialRemaining: prev.materialRemaining - 1,
              }
            : null,
        );
        refreshHistory();
        setScanState('scanLabel1');
        setLabel1Serial('');

        if (activeMaterial && activeMaterial.materialRemaining - 1 <= 0) {
          setPromptState('advanceQueue');
        }
      } catch {
        showScanResult({ type: 'error', message: 'Failed to save production record. Please try again.' });
      }
    },
    [workCenterId, assetId, productionLineId, operatorId, welders, activeMaterial, showScanResult, refreshHistory],
  );

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) {
        showScanResult({ type: 'error', message: 'Unknown barcode' });
        return;
      }

      if (promptState === 'advanceQueue') {
        if (bc.prefix === 'INP' && bc.value === '3') {
          advanceQueue();
          return;
        }
        if (bc.prefix === 'INP' && bc.value === '4') {
          setPromptState('none');
          setScanState('scanLabel1');
          return;
        }
      }

      if (promptState === 'thicknessInspection') {
        if (bc.prefix === 'INP' && bc.value === '3') {
          setThicknessInspectionRequired(false);
          setPromptState('none');
          createRecord(label1Serial, 'pass');
          return;
        }
        if (bc.prefix === 'INP' && bc.value === '4') {
          setPromptState('none');
          createRecord(label1Serial, 'fail');
          return;
        }
      }

      if (bc.prefix === 'INP' && bc.value === '2') {
        advanceQueue();
        return;
      }

      if (bc.prefix === 'FLT') {
        workCenterApi.reportFault(workCenterId, bc.value).catch(() => {});
        showScanResult({ type: 'error', message: `Fault: ${bc.value}` });
        return;
      }

      if (bc.prefix === 'SC') {
        if (!activeMaterial) {
          showScanResult({ type: 'error', message: 'Advance the material queue first' });
          return;
        }

        const parsed = parseShellLabel(bc.value);

        if (scanState === 'scanLabel1' || scanState === 'idle') {
          if (parsed.labelSuffix === 'L1' || parsed.labelSuffix === null) {
            setLabel1Serial(parsed.serialNumber);
            setScanState('scanLabel2');
            showScanResult({ type: 'success', message: 'Label 1 scanned — Scan Label 2' });
            return;
          }
        }

        if (scanState === 'scanLabel2') {
          if (parsed.labelSuffix === 'L1') {
            setLabel1Serial(parsed.serialNumber);
            showScanResult({ type: 'success', message: 'Label 1 replaced — Scan Label 2' });
            return;
          }

          if (parsed.labelSuffix === 'L2' || parsed.labelSuffix === null) {
            if (parsed.serialNumber === label1Serial) {
              if (thicknessInspectionRequired) {
                setPromptState('thicknessInspection');
              } else {
                createRecord(parsed.serialNumber);
              }
            } else {
              showScanResult({ type: 'error', message: 'Labels do not match' });
              setScanState('scanLabel1');
              setLabel1Serial('');
            }
            return;
          }
        }
      }

      showScanResult({ type: 'error', message: 'Invalid barcode in this context' });
    },
    [scanState, promptState, label1Serial, activeMaterial, thicknessInspectionRequired, advanceQueue, createRecord, showScanResult, workCenterId],
  );

  const handleManualSubmit = useCallback(() => {
    if (!manualSerial.trim() || !activeMaterial) return;
    if (thicknessInspectionRequired) {
      setLabel1Serial(manualSerial.trim());
      setPromptState('thicknessInspection');
    } else {
      createRecord(manualSerial.trim());
    }
    setManualSerial('');
  }, [manualSerial, activeMaterial, thicknessInspectionRequired, createRecord]);

  return (
    <div className={styles.container}>
      <div className={styles.topSection}>
        {!props.externalInput && (
          <div className={styles.manualEntry}>
            <Label>Scan Shell Label</Label>
            <div className={styles.manualRow}>
              <Input
                value={manualSerial}
                onChange={(_, d) => setManualSerial(d.value)}
                placeholder="Enter serial number..."
                size="large"
                className={styles.manualInput}
                onKeyDown={(e) => { if (e.key === 'Enter') handleManualSubmit(); }}
              />
              <Button appearance="primary" size="large" onClick={handleManualSubmit} disabled={!activeMaterial}>
                Submit
              </Button>
            </div>
          </div>
        )}

        <div className={styles.scanStateLabel}>
          {scanState === 'idle' && !activeMaterial && 'Advance the material queue to begin'}
          {scanState === 'scanLabel1' && 'Scan Label 1'}
          {scanState === 'scanLabel2' && `Scan Label 2 (Label 1: ${label1Serial})`}
        </div>

        {activeMaterial && (
          <div className={styles.dataGrid}>
            <div className={styles.dataItem}>
              <span className={styles.dataLabel}>Shell Count</span>
              <span className={styles.dataValueLarge}>{activeMaterial.shellCount} of {activeMaterial.queueQuantity}</span>
            </div>
            <div className={styles.dataItem}>
              <span className={styles.dataLabel}>Shell Size</span>
              <span className={styles.dataValue}>{activeMaterial.shellSize}</span>
            </div>
            <div className={styles.dataItem}>
              <span className={styles.dataLabel}>Heat Number</span>
              <span className={styles.dataValue}>{activeMaterial.heatNumber}</span>
            </div>
            <div className={styles.dataItem}>
              <span className={styles.dataLabel}>Coil Number</span>
              <span className={styles.dataValue}>{activeMaterial.coilNumber}</span>
            </div>
            <div className={styles.dataItem}>
              <span className={styles.dataLabel}>Queue Quantity</span>
              <span className={styles.dataValue}>{activeMaterial.queueQuantity}</span>
            </div>
            <div className={styles.dataItem}>
              <span className={styles.dataLabel}>Material Remaining</span>
              <span className={styles.dataValue}>{activeMaterial.materialRemaining}</span>
            </div>
          </div>
        )}
      </div>

      {promptState === 'advanceQueue' && (
        <div className={styles.prompt}>
          <h3>Advance Queue?</h3>
          <p>Material remaining has reached zero. Advance to the next material in the queue?</p>
          <div className={styles.promptButtons}>
            <Button appearance="primary" size="large" onClick={advanceQueue}>Yes</Button>
            <Button appearance="secondary" size="large" onClick={() => { setPromptState('none'); setScanState('scanLabel1'); }}>No</Button>
          </div>
        </div>
      )}

      {promptState === 'thicknessInspection' && (
        <div className={styles.prompt}>
          <h3>Thickness Inspection</h3>
          <p>Did the thickness inspection pass?</p>
          <div className={styles.promptButtons}>
            <Button appearance="primary" size="large" onClick={() => { setThicknessInspectionRequired(false); setPromptState('none'); createRecord(label1Serial, 'pass'); }}>Pass</Button>
            <Button appearance="secondary" size="large" onClick={() => { setPromptState('none'); createRecord(label1Serial, 'fail'); }}>Fail</Button>
          </div>
        </div>
      )}

      <div className={styles.queueSection}>
        <div className={styles.queueHeader}>
          <span className={styles.queueTitle}>Material Queue</span>
          <Button appearance="subtle" size="small" onClick={loadQueue}>Refresh</Button>
        </div>
        {queue.length === 0 ? (
          <div className={styles.emptyQueue}>No material in queue</div>
        ) : (
          queue.map((item) => (
            <button
              key={item.id}
              className={styles.queueCard}
              onClick={() => { if (!props.externalInput) advanceQueue(); }}
              disabled={props.externalInput}
            >
              <span className={styles.queueDesc}>{item.productDescription}</span>
              <span className={styles.queueQty}>{item.quantity}</span>
            </button>
          ))
        )}
      </div>
    </div>
  );
}
