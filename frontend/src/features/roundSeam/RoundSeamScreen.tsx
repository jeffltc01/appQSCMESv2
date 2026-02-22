import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import {
  Dialog, DialogSurface, DialogTitle, DialogBody, DialogContent, DialogActions,
} from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { Welder, RoundSeamSetup, AssemblyLookup } from '../../types/domain.ts';
import { roundSeamApi, workCenterApi } from '../../api/endpoints.ts';
import styles from './RoundSeamScreen.module.css';

function seamCountForSize(size: number): number {
  if (size <= 500) return 2;
  if (size <= 1000) return 3;
  return 4;
}

function tankBracket(size: number): number {
  if (size <= 500) return 500;
  if (size <= 1000) return 1000;
  return 1001;
}

export function RoundSeamScreen(props: WorkCenterProps) {
  const {
    workCenterId, assetId, productionLineId, operatorId,
    welders: parentWelders,
    welderCountLoaded = true,
    showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [setup, setSetup] = useState<RoundSeamSetup | null>(null);
  const [showSetup, setShowSetup] = useState(false);
  const [detectedTankSize, setDetectedTankSize] = useState(500);
  const [setupWelders, setSetupWelders] = useState<(string | undefined)[]>([undefined, undefined, undefined, undefined]);
  const [availableWelders, setAvailableWelders] = useState<Welder[]>([]);
  const [manualSerial, setManualSerial] = useState('');
  const [setupSaving, setSetupSaving] = useState(false);
  const [setupError, setSetupError] = useState('');
  const pendingScanRef = useRef<string | null>(null);
  const initialLoadDoneRef = useRef(false);

  useEffect(() => {
    initialLoadDoneRef.current = false;
    loadSetup();
    loadAvailableWelders();
  }, [workCenterId]);

  useEffect(() => {
    loadAvailableWelders();
  }, [parentWelders.length]);

  const pendingSetupOpenRef = useRef(false);

  const loadSetup = useCallback(async () => {
    try {
      const s = await roundSeamApi.getSetup(workCenterId);
      setSetup(s);
      if (s.tankSize > 0) setDetectedTankSize(s.tankSize);
      if (!s.isComplete && !initialLoadDoneRef.current) {
        initialLoadDoneRef.current = true;
        setSetupWelders([s.rs1WelderId, s.rs2WelderId, s.rs3WelderId, s.rs4WelderId]);
        if (welderCountLoaded) {
          setShowSetup(true);
        } else {
          pendingSetupOpenRef.current = true;
        }
      } else {
        initialLoadDoneRef.current = true;
      }
    } catch {
      initialLoadDoneRef.current = true;
      if (welderCountLoaded) {
        setShowSetup(true);
      } else {
        pendingSetupOpenRef.current = true;
      }
    }
  }, [workCenterId, welderCountLoaded]);

  useEffect(() => {
    if (welderCountLoaded && pendingSetupOpenRef.current) {
      pendingSetupOpenRef.current = false;
      setShowSetup(true);
    }
  }, [welderCountLoaded]);

  const loadAvailableWelders = useCallback(async () => {
    try {
      const w = await workCenterApi.getWelders(workCenterId);
      setAvailableWelders(w);
    } catch { /* keep empty */ }
  }, [workCenterId]);

  const openSetup = useCallback((tankSize?: number) => {
    if (tankSize) setDetectedTankSize(tankSize);
    setSetupWelders([
      setup?.rs1WelderId, setup?.rs2WelderId,
      setup?.rs3WelderId, setup?.rs4WelderId,
    ]);
    setSetupError('');
    setShowSetup(true);
  }, [setup]);

  const saveSetup = useCallback(async () => {
    setSetupSaving(true);
    setSetupError('');
    try {
      const s = await roundSeamApi.saveSetup(workCenterId, {
        tankSize: detectedTankSize,
        rs1WelderId: setupWelders[0],
        rs2WelderId: setupWelders[1],
        rs3WelderId: setupWelders[2],
        rs4WelderId: setupWelders[3],
      });
      setSetup(s);
      setShowSetup(false);

      if (pendingScanRef.current) {
        const serial = pendingScanRef.current;
        pendingScanRef.current = null;
        await doSubmitShell(serial);
      } else {
        showScanResult({ type: 'success', message: 'Round seam setup saved' });
      }
    } catch {
      setSetupError('Failed to save setup');
    } finally {
      setSetupSaving(false);
    }
  }, [workCenterId, detectedTankSize, setupWelders, showScanResult]);

  const doSubmitShell = useCallback(async (serial: string) => {
    try {
      await roundSeamApi.createRecord({
        serialNumber: serial,
        workCenterId,
        assetId,
        productionLineId,
        operatorId,
      });
      showScanResult({ type: 'success', message: 'Assembly recorded at Round Seam' });
      refreshHistory();
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Failed to record' });
    }
  }, [workCenterId, assetId, productionLineId, operatorId, showScanResult, refreshHistory]);

  const submitShell = useCallback(async (serial: string) => {
    if (!setup?.isComplete) {
      showScanResult({ type: 'error', message: 'Complete Roundseam Setup before scanning' });
      return;
    }

    let assembly: AssemblyLookup | null = null;
    try {
      assembly = await roundSeamApi.getAssemblyByShell(serial);
    } catch { /* assembly lookup failed -- proceed with current setup */ }

    if (assembly && tankBracket(assembly.tankSize) !== tankBracket(setup.tankSize)) {
      pendingScanRef.current = serial;
      setDetectedTankSize(assembly.tankSize);
      setSetupWelders([setup.rs1WelderId, setup.rs2WelderId, setup.rs3WelderId, setup.rs4WelderId]);
      setSetupError('');
      setShowSetup(true);
      return;
    }

    await doSubmitShell(serial);
  }, [setup, doSubmitShell, showScanResult]);

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

  const seamCount = seamCountForSize(detectedTankSize);

  return (
    <div className={styles.container}>
      {!setup?.isComplete && (
        <div className={styles.warningBanner}>
          WARNING: Roundseam setup hasn&apos;t been completed!
        </div>
      )}

      <Button appearance="primary" size="large" onClick={() => openSetup()}>
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

      <Dialog open={showSetup} onOpenChange={(_, data) => { if (!data.open && !pendingScanRef.current) { setShowSetup(false); } }}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Roundseam Setup</DialogTitle>
            <DialogContent>
              <div className={styles.setupForm}>
                <div className={styles.setupInfo}>
                  <span>Tank Size: <strong>{detectedTankSize}</strong></span>
                  <span>Seams Required: <strong>{seamCount}</strong></span>
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
                      {availableWelders.map((w) => (
                        <Option key={w.userId} value={w.userId}>{w.displayName}</Option>
                      ))}
                    </Dropdown>
                  </div>
                ))}
                {pendingScanRef.current && (
                  <div className={styles.warningBanner}>
                    Tank size bracket changed. Please verify welder assignments before continuing.
                  </div>
                )}
                {setupError && <div style={{ color: '#dc3545', fontSize: 13 }}>{setupError}</div>}
              </div>
            </DialogContent>
            <DialogActions>
              {!pendingScanRef.current && (
                <Button appearance="secondary" onClick={() => setShowSetup(false)} disabled={setupSaving}>
                  Cancel
                </Button>
              )}
              <Button appearance="primary" onClick={saveSetup} disabled={setupSaving}>
                {setupSaving ? 'Saving...' : 'Save Setup'}
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    </div>
  );
}
