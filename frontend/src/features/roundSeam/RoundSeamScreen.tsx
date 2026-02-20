import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { Welder, RoundSeamSetup } from '../../types/domain.ts';
import { roundSeamApi, workCenterApi } from '../../api/endpoints.ts';
import styles from './RoundSeamScreen.module.css';

const TANK_SIZES = [120, 250, 500, 1000, 1500, 1990];

function seamCountForSize(size: number): number {
  if (size <= 500) return 2;
  if (size <= 1000) return 3;
  return 4;
}

export function RoundSeamScreen(props: WorkCenterProps) {
  const {
    workCenterId, assetId, productionLineId, operatorId,
    showScanResult, refreshHistory, registerBarcodeHandler, setRequiresWelder,
  } = props;

  const [setup, setSetup] = useState<RoundSeamSetup | null>(null);
  const [showSetup, setShowSetup] = useState(false);
  const [setupTankSize, setSetupTankSize] = useState<number>(0);
  const [setupWelders, setSetupWelders] = useState<(string | undefined)[]>([undefined, undefined, undefined, undefined]);
  const [availableWelders, setAvailableWelders] = useState<Welder[]>([]);
  const [manualSerial, setManualSerial] = useState('');

  useEffect(() => {
    setRequiresWelder(true);
    loadSetup();
    loadAvailableWelders();
  }, [workCenterId]);

  const loadSetup = useCallback(async () => {
    try {
      const s = await roundSeamApi.getSetup(workCenterId);
      setSetup(s);
    } catch { /* no setup */ }
  }, [workCenterId]);

  const loadAvailableWelders = useCallback(async () => {
    try {
      const w = await workCenterApi.getWelders(workCenterId);
      setAvailableWelders(w);
    } catch { /* keep empty */ }
  }, [workCenterId]);

  const openSetup = useCallback(() => {
    setSetupTankSize(setup?.tankSize ?? 0);
    setSetupWelders([
      setup?.rs1WelderId, setup?.rs2WelderId,
      setup?.rs3WelderId, setup?.rs4WelderId,
    ]);
    setShowSetup(true);
  }, [setup]);

  const saveSetup = useCallback(async () => {
    if (setupTankSize === 0) {
      showScanResult({ type: 'error', message: 'Select a tank size' });
      return;
    }
    try {
      const s = await roundSeamApi.saveSetup(workCenterId, {
        tankSize: setupTankSize,
        rs1WelderId: setupWelders[0],
        rs2WelderId: setupWelders[1],
        rs3WelderId: setupWelders[2],
        rs4WelderId: setupWelders[3],
      });
      setSetup(s);
      setShowSetup(false);
      showScanResult({ type: 'success', message: 'Round seam setup saved' });
    } catch {
      showScanResult({ type: 'error', message: 'Failed to save setup' });
    }
  }, [workCenterId, setupTankSize, setupWelders, showScanResult]);

  const submitShell = useCallback(async (serial: string) => {
    if (!setup?.isComplete) {
      showScanResult({ type: 'error', message: 'Complete Roundseam Setup before scanning' });
      return;
    }
    try {
      await roundSeamApi.createRecord({
        serialNumber: serial,
        workCenterId,
        assetId,
        productionLineId,
        operatorId,
      });
      showScanResult({ type: 'success', message: `Assembly recorded at Round Seam` });
      refreshHistory();
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to record' });
    }
  }, [setup, workCenterId, assetId, productionLineId, operatorId, showScanResult, refreshHistory]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) { showScanResult({ type: 'error', message: 'Unknown barcode' }); return; }
      if (bc.prefix === 'SC') {
        const { serialNumber } = parseShellLabel(bc.value);
        submitShell(serialNumber);
        return;
      }
      showScanResult({ type: 'error', message: 'Scan a shell barcode' });
    },
    [submitShell, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualSubmit = useCallback(() => {
    if (manualSerial.trim()) {
      submitShell(manualSerial.trim());
      setManualSerial('');
    }
  }, [manualSerial, submitShell]);

  if (showSetup) {
    const seamCount = setupTankSize > 0 ? seamCountForSize(setupTankSize) : 2;
    return (
      <div className={styles.container}>
        <h3 className={styles.sectionTitle}>Roundseam Setup</h3>
        <div className={styles.setupForm}>
          <div className={styles.formField}>
            <Label required>Tank Size</Label>
            <Dropdown
              placeholder="Select tank size"
              value={setupTankSize > 0 ? setupTankSize.toString() : ''}
              selectedOptions={setupTankSize > 0 ? [setupTankSize.toString()] : []}
              onOptionSelect={(_: unknown, d: OptionOnSelectData) => {
                const size = parseInt(d.optionValue ?? '0', 10);
                setSetupTankSize(size);
              }}
            >
              {TANK_SIZES.map((s) => <Option key={s} value={s.toString()}>{s.toString()}</Option>)}
            </Dropdown>
          </div>
          {Array.from({ length: seamCount }, (_, i) => (
            <div key={i} className={styles.formField}>
              <Label>Roundseam {i + 1} Welder</Label>
              <Dropdown
                placeholder="Select welder"
                value={availableWelders.find((w) => w.userId === setupWelders[i])?.displayName ?? ''}
                selectedOptions={setupWelders[i] ? [setupWelders[i]!] : []}
                onOptionSelect={(_: unknown, d: OptionOnSelectData) => {
                  setSetupWelders((prev) => {
                    const next = [...prev];
                    next[i] = d.optionValue;
                    return next;
                  });
                }}
              >
                {availableWelders.map((w) => <Option key={w.userId} value={w.userId}>{w.displayName}</Option>)}
              </Dropdown>
            </div>
          ))}
          <div className={styles.formActions}>
            <Button appearance="secondary" size="large" onClick={() => setShowSetup(false)}>Cancel</Button>
            <Button appearance="primary" size="large" onClick={saveSetup}>Save Setup</Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {!setup?.isComplete && (
        <div className={styles.warningBanner}>
          WARNING: Roundseam setup hasn't been completed!
        </div>
      )}

      <Button appearance="secondary" size="large" onClick={openSetup}>
        Roundseam Setup
      </Button>

      {!props.externalInput && (
        <div className={styles.manualEntry}>
          <Label>Serial Number</Label>
          <div className={styles.manualRow}>
            <Input
              value={manualSerial}
              onChange={(_, d) => setManualSerial(d.value)}
              placeholder="Enter serial number..."
              size="large"
              className={styles.manualInput}
              onKeyDown={(e) => { if (e.key === 'Enter') handleManualSubmit(); }}
            />
            <Button appearance="primary" size="large" onClick={handleManualSubmit} disabled={!setup?.isComplete}>
              Submit
            </Button>
          </div>
        </div>
      )}

      {setup?.isComplete && (
        <div className={styles.setupInfo}>
          <span>Tank Size: <strong>{setup.tankSize}</strong></span>
          <span>Seams: <strong>{seamCountForSize(setup.tankSize)}</strong></span>
        </div>
      )}
    </div>
  );
}
