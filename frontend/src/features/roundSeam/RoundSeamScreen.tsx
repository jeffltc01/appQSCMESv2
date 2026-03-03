import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import {
  Dialog, DialogSurface, DialogTitle, DialogBody, DialogContent, DialogActions,
} from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { RoundSeamSetup, AssemblyLookup } from '../../types/domain.ts';
import { roundSeamApi } from '../../api/endpoints.ts';
import { NextStepBanner } from '../../components/nextStep/NextStepBanner.tsx';
import styles from './RoundSeamScreen.module.css';

function seamCountForSize(size: number): number {
  if (size <= 500) return 2;
  if (size <= 1000) return 3;
  return 4;
}

function normalizeWelderId(id: string | null | undefined): string {
  return (id ?? '').trim().toLowerCase();
}

function setupWelderIdsForSize(setup: RoundSeamSetup): string[] {
  const requiredSeams = seamCountForSize(setup.tankSize);
  return [setup.rs1WelderId, setup.rs2WelderId, setup.rs3WelderId, setup.rs4WelderId]
    .slice(0, requiredSeams)
    .map((id) => normalizeWelderId(id))
    .filter((id) => id.length > 0);
}

function setupMatchesActiveWelders(setup: RoundSeamSetup, activeWelderIds: readonly string[]): boolean {
  if (!setup.isComplete) return false;
  const requiredSeams = seamCountForSize(setup.tankSize);
  const setupWelderIds = setupWelderIdsForSize(setup);
  if (setupWelderIds.length !== requiredSeams) return false;
  const activeWelderIdSet = new Set(activeWelderIds.map((id) => normalizeWelderId(id)).filter((id) => id.length > 0));
  if (activeWelderIdSet.size === 0) return false;
  return setupWelderIds.every((id) => activeWelderIdSet.has(id));
}

function tankBracket(size: number): number {
  if (size <= 500) return 500;
  if (size <= 1000) return 1000;
  return 1001;
}

function welderFingerprint(welderIds: readonly string[]): string {
  return welderIds
    .map((id) => normalizeWelderId(id))
    .filter((id) => id.length > 0)
    .sort((a, b) => a.localeCompare(b))
    .join('|');
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
  const availableWelders = parentWelders;
  const [manualSerial, setManualSerial] = useState('');
  const [setupSaving, setSetupSaving] = useState(false);
  const [setupError, setSetupError] = useState('');
  const [setupWarning, setSetupWarning] = useState('');
  const [setupLoaded, setSetupLoaded] = useState(false);
  const [setupWelderFingerprint, setSetupWelderFingerprint] = useState<string | null>(null);
  const pendingScanRef = useRef<string | null>(null);
  const initialLoadDoneRef = useRef(false);
  const currentWelderFingerprint = welderFingerprint(availableWelders.map((w) => w.userId));

  const invalidateSetupForWelderChange = useCallback(() => {
    setSetup((prev) => {
      if (!prev || !prev.isComplete) return prev;
      return { ...prev, isComplete: false };
    });
    setSetupWelders([
      setup?.rs1WelderId, setup?.rs2WelderId,
      setup?.rs3WelderId, setup?.rs4WelderId,
    ]);
    setSetupWarning('Logged-in welders changed. Please reassign round seam welders.');
    setSetupError('');
    setShowSetup(true);
  }, [setup]);

  useEffect(() => {
    initialLoadDoneRef.current = false;
    setSetupLoaded(false);
    loadSetup();
  }, [workCenterId]);

  const pendingSetupOpenRef = useRef(false);

  const loadSetup = useCallback(async () => {
    try {
      const s = await roundSeamApi.getSetup(workCenterId);
      const activeWelderIds = availableWelders.map((w) => w.userId);
      const staleSetup = welderCountLoaded
        && activeWelderIds.length > 0
        && s.isComplete
        && !setupMatchesActiveWelders(s, activeWelderIds);
      const effectiveSetup = staleSetup ? { ...s, isComplete: false } : s;
      setSetup(effectiveSetup);
      if (s.tankSize > 0) setDetectedTankSize(s.tankSize);
      if (effectiveSetup.isComplete) {
        setSetupWelderFingerprint(currentWelderFingerprint);
      } else {
        setSetupWelderFingerprint(null);
      }
      if (!effectiveSetup.isComplete && !initialLoadDoneRef.current) {
        initialLoadDoneRef.current = true;
        setSetupWelders([s.rs1WelderId, s.rs2WelderId, s.rs3WelderId, s.rs4WelderId]);
        if (staleSetup) {
          setSetupWarning('Setup welders are not all active. Please reassign round seam welders.');
        } else {
          setSetupWarning('');
        }
        if (welderCountLoaded) {
          setShowSetup(true);
        } else {
          pendingSetupOpenRef.current = true;
        }
      } else {
        initialLoadDoneRef.current = true;
      }
    } catch {
      setSetupWelderFingerprint(null);
      initialLoadDoneRef.current = true;
      if (welderCountLoaded) {
        setShowSetup(true);
      } else {
        pendingSetupOpenRef.current = true;
      }
    } finally {
      setSetupLoaded(true);
    }
  }, [workCenterId, welderCountLoaded, currentWelderFingerprint, availableWelders]);

  useEffect(() => {
    if (welderCountLoaded && pendingSetupOpenRef.current) {
      pendingSetupOpenRef.current = false;
      setShowSetup(true);
    }
  }, [welderCountLoaded]);

  const openSetup = useCallback((tankSize?: number) => {
    if (tankSize) setDetectedTankSize(tankSize);
    setSetupWelders([
      setup?.rs1WelderId, setup?.rs2WelderId,
      setup?.rs3WelderId, setup?.rs4WelderId,
    ]);
    setSetupError('');
    setSetupWarning('');
    setShowSetup(true);
  }, [setup]);

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
      setSetupWarning('');
      setSetupWelderFingerprint(currentWelderFingerprint);

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
  }, [workCenterId, detectedTankSize, setupWelders, showScanResult, currentWelderFingerprint, doSubmitShell]);

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
      setSetupWarning('');
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

  useEffect(() => {
    if (!welderCountLoaded || !setup?.isComplete) return;
    const activeWelderIds = availableWelders.map((w) => w.userId);
    if (activeWelderIds.length === 0) return;
    if (!setupMatchesActiveWelders(setup, activeWelderIds)) {
      setSetup((prev) => (prev ? { ...prev, isComplete: false } : prev));
      setSetupWelders([setup.rs1WelderId, setup.rs2WelderId, setup.rs3WelderId, setup.rs4WelderId]);
      setSetupWarning('Setup welders are not all active. Please reassign round seam welders.');
      setSetupError('');
      setShowSetup(true);
      setSetupWelderFingerprint(null);
      return;
    }
    if (!setup?.isComplete || !setupWelderFingerprint) return;
    if (setupWelderFingerprint !== currentWelderFingerprint) {
      invalidateSetupForWelderChange();
      setSetupWelderFingerprint(null);
    }
  }, [setup, setupWelderFingerprint, currentWelderFingerprint, invalidateSetupForWelderChange, welderCountLoaded, availableWelders]);

  const handleManualSubmit = useCallback(() => {
    if (manualSerial.trim()) {
      submitShell(manualSerial.trim());
      setManualSerial('');
    }
  }, [manualSerial, submitShell]);

  const seamCount = seamCountForSize(detectedTankSize);
  const summaryTankSize = setup?.tankSize && setup.tankSize > 0 ? setup.tankSize : detectedTankSize;
  const summarySeamCount = seamCountForSize(summaryTankSize);
  const summaryWelderIds = [
    setup?.rs1WelderId ?? setupWelders[0],
    setup?.rs2WelderId ?? setupWelders[1],
    setup?.rs3WelderId ?? setupWelders[2],
    setup?.rs4WelderId ?? setupWelders[3],
  ];
  const seamAssignments = Array.from({ length: summarySeamCount }, (_, i) => {
    const welderId = summaryWelderIds[i];
    const normalizedWelderId = normalizeWelderId(welderId);
    const welderName = welderId
      ? availableWelders.find((w) => normalizeWelderId(w.userId) === normalizedWelderId)?.displayName ?? 'Inactive welder'
      : 'Unassigned';
    return { seamNo: i + 1, welderName };
  });
  const scanInstruction = (() => {
    if (!setup?.isComplete) {
      return {
        title: 'Complete Roundseam Setup',
        detail: 'Tap "Roundseam Setup", assign welders, then save setup.',
        isActive: false,
      };
    }

    return {
      title: 'Scan shell barcode',
      detail: `Setup ready for ${setup.tankSize} (${seamCountForSize(setup.tankSize)} seams)`,
      isActive: true,
    };
  })();

  return (
    <div className={styles.container}>
      <NextStepBanner instruction={scanInstruction} />

      <div className={styles.setupSummary}>
        <div className={styles.setupInfo}>
          <span>Tank Size: <strong>{summaryTankSize}</strong></span>
          <span>Seams: <strong>{summarySeamCount}</strong></span>
        </div>
        <div className={styles.seamAssignments}>
          {seamAssignments.map((item) => (
            <div key={item.seamNo} className={styles.seamAssignment}>
              Seam {item.seamNo} = <strong>{item.welderName}</strong>
            </div>
          ))}
        </div>
      </div>

      {setupLoaded && !setup?.isComplete && (
        <div className={styles.warningBanner}>
          WARNING: Roundseam setup hasn&apos;t been completed!
        </div>
      )}

      {!props.externalInput && (
        <Button appearance="primary" size="large" onClick={() => openSetup()}>
          Roundseam Setup
        </Button>
      )}

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
                {setupWarning && (
                  <div className={styles.warningBanner}>
                    {setupWarning}
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
