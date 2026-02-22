import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { MaterialQueueItem, OperatorControlPlan } from '../../types/domain.ts';
import type { QueueAdvanceResponse } from '../../types/api.ts';
import { workCenterApi, productionRecordApi, inspectionRecordApi, controlPlanApi } from '../../api/endpoints.ts';
import styles from './RollsScreen.module.css';

type ScanState = 'idle' | 'scanLabel1' | 'scanLabel2';
type PromptState = 'none' | 'advanceQueue' | 'inspectionResults';

function getResultLabels(resultType: string): [string, string] {
  switch (resultType) {
    case 'AcceptReject': return ['Accept', 'Reject'];
    case 'GoNoGo': return ['Go', 'NoGo'];
    default: return ['Pass', 'Fail'];
  }
}

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
    showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [scanState, setScanState] = useState<ScanState>('idle');
  const [promptState, setPromptState] = useState<PromptState>('none');
  const [label1Serial, setLabel1Serial] = useState('');
  const [label1Raw, setLabel1Raw] = useState('');
  const [activeMaterial, setActiveMaterial] = useState<ActiveMaterial | null>(null);
  const [queue, setQueue] = useState<MaterialQueueItem[]>([]);
  const [manualSerial, setManualSerial] = useState('');
  const [controlPlans, setControlPlans] = useState<OperatorControlPlan[]>([]);
  const [inspectionResults, setInspectionResults] = useState<Record<string, string>>({});
  const [inspectionIndex, setInspectionIndex] = useState(0);
  const [needsInspection, setNeedsInspection] = useState(false);

  useEffect(() => {
    loadQueue();
  }, [workCenterId]);

  useEffect(() => {
    if (!workCenterId || !productionLineId) {
      setControlPlans([]);
      return;
    }
    controlPlanApi.getForWorkCenter(workCenterId, productionLineId)
      .then(setControlPlans)
      .catch(() => setControlPlans([]));
  }, [workCenterId, productionLineId]);

  const loadQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(workCenterId);
      setQueue(items.filter((i) => i.status === 'queued'));

      const active = items.find((i) => i.status === 'active');
      if (active && !activeMaterial) {
        setActiveMaterial({
          shellSize: active.shellSize ?? '',
          heatNumber: active.heatNumber,
          coilNumber: active.coilNumber,
          queueQuantity: active.quantity,
          materialRemaining: active.quantity - active.quantityCompleted,
          shellCount: active.quantityCompleted,
          productDescription: active.productDescription,
        });
        if (active.quantityCompleted === 0) setNeedsInspection(true);
        setScanState('scanLabel1');
      }
    } catch { /* keep stale */ }
  }, [workCenterId, activeMaterial]);

  const advanceQueue = useCallback(async () => {
    try {
      const data: QueueAdvanceResponse = await workCenterApi.advanceQueue(workCenterId);
      setActiveMaterial({
        shellSize: data.shellSize,
        heatNumber: data.heatNumber,
        coilNumber: data.coilNumber,
        queueQuantity: data.quantity,
        materialRemaining: data.quantity - data.quantityCompleted,
        shellCount: data.quantityCompleted,
        productDescription: data.productDescription,
      });
      setNeedsInspection(true);
      setScanState('scanLabel1');
      setPromptState('none');
      loadQueue();
      showScanResult({ type: 'success', message: 'Queue advanced' });
    } catch {
      showScanResult({ type: 'error', message: 'No material in queue. Contact Material Handling.' });
    }
  }, [workCenterId, loadQueue, showScanResult]);

  const createRecord = useCallback(
    async (serial: string, resultsOverride?: Record<string, string>) => {
      const effectiveResults = resultsOverride ?? inspectionResults;
      if (!serial) {
        showScanResult({ type: 'error', message: 'No serial number — scan labels first' });
        return;
      }
      if (!workCenterId || !productionLineId || !operatorId) {
        showScanResult({ type: 'error', message: `Missing setup data (wc:${!!workCenterId} line:${!!productionLineId} op:${!!operatorId})` });
        return;
      }
      try {
        const resp = await productionRecordApi.create({
          serialNumber: serial,
          workCenterId,
          assetId: assetId || undefined,
          productionLineId,
          operatorId,
          welderIds: welders.map((w) => w.userId),
          shellSize: activeMaterial?.shellSize,
          heatNumber: activeMaterial?.heatNumber,
          coilNumber: activeMaterial?.coilNumber,
        });
        const hasInspectionResults = controlPlans.length > 0 && controlPlans.every((cp) => effectiveResults[cp.id]);
        if (hasInspectionResults) {
          await inspectionRecordApi.create({
            serialNumber: serial,
            workCenterId,
            operatorId,
            productionRecordId: resp.id,
            results: controlPlans.map((cp) => ({
              controlPlanId: cp.id,
              resultText: effectiveResults[cp.id],
            })),
            defects: [],
          });
          setNeedsInspection(false);
        }
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
        setInspectionResults({});
        refreshHistory();
        setScanState('scanLabel1');
        setLabel1Serial('');
        setLabel1Raw('');

        if (activeMaterial && activeMaterial.materialRemaining - 1 <= 0) {
          setPromptState('advanceQueue');
        }
      } catch (err: unknown) {
        const msg = err && typeof err === 'object' && 'message' in err
          ? (err as { message: string }).message
          : JSON.stringify(err);
        showScanResult({ type: 'error', message: `Record not saved: ${msg}` });
      }
    },
    [workCenterId, assetId, productionLineId, operatorId, welders, activeMaterial, controlPlans, inspectionResults, showScanResult, refreshHistory],
  );

  const handleInspectionChoice = useCallback(
    (resultText: string) => {
      const cp = controlPlans[inspectionIndex];
      if (!cp) return;
      const updatedResults = { ...inspectionResults, [cp.id]: resultText };
      setInspectionResults(updatedResults);

      if (inspectionIndex < controlPlans.length - 1) {
        setInspectionIndex(inspectionIndex + 1);
      } else {
        setPromptState('none');
        createRecord(label1Serial, updatedResults);
      }
    },
    [controlPlans, inspectionIndex, inspectionResults, label1Serial, createRecord],
  );

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) {
        showScanResult({ type: 'error', message: 'Unknown barcode' });
        return;
      }

      // When a prompt is active, only accept responses to that prompt
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
        showScanResult({ type: 'error', message: 'Scan YES or NO to respond' });
        return;
      }

      if (promptState === 'inspectionResults') {
        const currentPlan = controlPlans[inspectionIndex];
        if (currentPlan) {
          const [positiveLabel, negativeLabel] = getResultLabels(currentPlan.resultType);
          if (bc.prefix === 'INP' && bc.value === '3') {
            handleInspectionChoice(positiveLabel);
            return;
          }
          if (bc.prefix === 'INP' && bc.value === '4') {
            handleInspectionChoice(negativeLabel);
            return;
          }
        }
        showScanResult({ type: 'error', message: 'Scan PASS or FAIL to respond' });
        return;
      }

      // Normal flow — no prompt active
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
            setLabel1Raw(bc.value);
            setScanState('scanLabel2');
            showScanResult({ type: 'success', message: 'Label 1 scanned — Scan Label 2' });
            return;
          }
        }

        if (scanState === 'scanLabel2') {
          if (parsed.labelSuffix === 'L1') {
            setLabel1Serial(parsed.serialNumber);
            setLabel1Raw(bc.value);
            showScanResult({ type: 'success', message: 'Label 1 replaced — Scan Label 2' });
            return;
          }

          if (parsed.labelSuffix === 'L2' || parsed.labelSuffix === null) {
            if (parsed.labelSuffix === null && bc.value === label1Raw) {
              showScanResult({ type: 'error', message: 'Same label scanned twice — scan the other label' });
              return;
            }
            if (parsed.serialNumber === label1Serial) {
              const isOverflow = (activeMaterial?.materialRemaining ?? 1) <= 0;
              if (controlPlans.length > 0 && (needsInspection || isOverflow)) {
                setInspectionIndex(0);
                setPromptState('inspectionResults');
              } else {
                createRecord(parsed.serialNumber);
              }
            } else {
              showScanResult({ type: 'error', message: `Labels do not match: Label 1 = ${label1Serial}, Label 2 = ${parsed.serialNumber}` });
              setScanState('scanLabel1');
              setLabel1Serial('');
              setLabel1Raw('');
            }
            return;
          }
        }
      }

      showScanResult({ type: 'error', message: 'Invalid barcode in this context' });
    },
    [scanState, promptState, label1Serial, label1Raw, activeMaterial, controlPlans, inspectionIndex, needsInspection, handleInspectionChoice, advanceQueue, createRecord, showScanResult, workCenterId],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualSubmit = useCallback(() => {
    if (!manualSerial.trim() || !activeMaterial) return;
    const isOverflow = activeMaterial.materialRemaining <= 0;
    if (controlPlans.length > 0 && (needsInspection || isOverflow)) {
      setLabel1Serial(manualSerial.trim());
      setInspectionIndex(0);
      setPromptState('inspectionResults');
    } else {
      createRecord(manualSerial.trim());
    }
    setManualSerial('');
  }, [manualSerial, activeMaterial, controlPlans, needsInspection, createRecord]);

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
              <Button appearance="primary" size="large" onClick={handleManualSubmit} disabled={!activeMaterial || promptState !== 'none'}>
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

      {promptState === 'inspectionResults' && controlPlans[inspectionIndex] && (() => {
        const cp = controlPlans[inspectionIndex];
        const [positiveLabel, negativeLabel] = getResultLabels(cp.resultType);
        return (
          <div className={styles.prompt}>
            <h3>{cp.characteristicName}</h3>
            {controlPlans.length > 1 && (
              <p style={{ margin: '0 0 0.5rem', opacity: 0.7 }}>{inspectionIndex + 1} of {controlPlans.length}</p>
            )}
            <div className={styles.promptButtons}>
              <Button appearance="primary" size="large" onClick={() => handleInspectionChoice(positiveLabel)}>
                {positiveLabel}
              </Button>
              <Button appearance="primary" size="large" onClick={() => handleInspectionChoice(negativeLabel)}>
                {negativeLabel}
              </Button>
            </div>
          </div>
        );
      })()}

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
              onClick={() => { if (!props.externalInput && promptState === 'none') advanceQueue(); }}
              disabled={props.externalInput || promptState !== 'none'}
            >
              <span className={styles.queueDesc}>{item.shellSize ? `(${item.shellSize}) ` : ''}{item.productDescription}</span>
              <span className={styles.queueQty}>{item.quantity}</span>
            </button>
          ))
        )}
      </div>
    </div>
  );
}
