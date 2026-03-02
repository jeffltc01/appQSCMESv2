import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import { DeleteRegular } from '@fluentui/react-icons';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { DefectCode, Characteristic, DefectLocation, DefectEntry, OperatorControlPlan } from '../../types/domain.ts';
import type { InspectionResultEntry } from '../../types/api.ts';
import { roundSeamApi, nameplateApi, workCenterApi, hydroApi, controlPlanApi } from '../../api/endpoints.ts';
import { NextStepBanner } from '../../components/nextStep/NextStepBanner.tsx';
import styles from './HydroScreen.module.css';

type HydroState = 'WaitingForScans' | 'ReadyForInspection' | 'DefectEntry';

interface DefectWizardState {
  step: 'defect' | 'characteristic' | 'location';
  defectCodeId?: string;
  defectCodeName?: string;
  characteristicId?: string;
  characteristicName?: string;
}

function supportsCharacteristic(location: DefectLocation, characteristicId: string) {
  return location.characteristicIds.length === 0 || location.characteristicIds.includes(characteristicId);
}

function getResultLabels(resultType: string): [string, string] {
  switch (resultType) {
    case 'AcceptReject': return ['Accept', 'Reject'];
    case 'GoNoGo': return ['Go', 'NoGo'];
    default: return ['Pass', 'Fail'];
  }
}

export function HydroScreen(props: WorkCenterProps) {
  const {
    workCenterId, productionLineId, assetId, operatorId,
    setExternalInput, showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [state, setState] = useState<HydroState>('WaitingForScans');
  const [shellSerial, setShellSerial] = useState('');
  const [noShellMode, setNoShellMode] = useState(false);
  const [assemblyAlpha, setAssemblyAlpha] = useState('');
  const [assemblyShells, setAssemblyShells] = useState<string[]>([]);
  const [assemblyTankSize, setAssemblyTankSize] = useState<number | null>(null);
  const [nameplateSerial, setNameplateSerial] = useState('');
  const [nameplateTankSize, setNameplateTankSize] = useState<number | null>(null);
  const [defects, setDefects] = useState<DefectEntry[]>([]);
  const [wizard, setWizard] = useState<DefectWizardState | null>(null);

  const [defectCodes, setDefectCodes] = useState<DefectCode[]>([]);
  const [characteristics, setCharacteristics] = useState<Characteristic[]>([]);
  const [defectLocations, setDefectLocations] = useState<DefectLocation[]>([]);
  const [controlPlans, setControlPlans] = useState<OperatorControlPlan[]>([]);
  const [inspectionResults, setInspectionResults] = useState<Record<string, string>>({});
  const [currentControlPlanIndex, setCurrentControlPlanIndex] = useState(0);
  const [tankSizeMismatch, setTankSizeMismatch] = useState(false);
  const [manualShellInput, setManualShellInput] = useState('');
  const [manualNameplateInput, setManualNameplateInput] = useState('');
  const resolvedTankSize = assemblyTankSize ?? nameplateTankSize ?? undefined;

  useEffect(() => {
    loadLookups();
  }, [workCenterId]);

  const loadLookups = useCallback(async () => {
    try {
      const [codes, locs] = await Promise.all([
        workCenterApi.getDefectCodes(workCenterId),
        workCenterApi.getDefectLocations(workCenterId),
      ]);
      setDefectCodes(codes);
      setDefectLocations(locs);
    } catch { /* keep empty */ }
    if (productionLineId) {
      try {
        const plans = await controlPlanApi.getForWorkCenter(workCenterId, productionLineId);
        setControlPlans(plans);
      } catch { /* keep empty */ }
    }
  }, [workCenterId, productionLineId]);

  useEffect(() => {
    workCenterApi.getCharacteristics(workCenterId, resolvedTankSize)
      .then(setCharacteristics)
      .catch(() => setCharacteristics([]));
  }, [workCenterId, resolvedTankSize]);

  useEffect(() => {
    setCurrentControlPlanIndex((prev) => {
      if (controlPlans.length === 0) return 0;
      return Math.min(prev, controlPlans.length - 1);
    });
  }, [controlPlans]);

  const scanShell = useCallback(async (serial: string) => {
    try {
      const assembly = await roundSeamApi.getAssemblyByShell(serial);
      setNoShellMode(false);
      setShellSerial(serial);
      setAssemblyAlpha(assembly.alphaCode);
      setAssemblyShells(assembly.shells ?? []);
      setAssemblyTankSize(assembly.tankSize ?? null);
      showScanResult({ type: 'success', message: `Assembly ${assembly.alphaCode} found` });
      if (nameplateSerial) {
        if (nameplateTankSize != null && assembly.tankSize != null && assembly.tankSize !== nameplateTankSize) {
          setTankSizeMismatch(true);
          showScanResult({ type: 'error', message: `Tank size mismatch: shell assembly is ${assembly.tankSize} gal but nameplate is ${nameplateTankSize} gal` });
          return;
        }
        setTankSizeMismatch(false);
        setState('ReadyForInspection');
        setExternalInput(false);
      }
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Shell not found in any assembly' });
    }
  }, [nameplateSerial, nameplateTankSize, showScanResult, setExternalInput]);

  const setNoShell = useCallback(() => {
    setNoShellMode(true);
    setShellSerial('');
    setAssemblyAlpha('');
    setAssemblyShells([]);
    setAssemblyTankSize(null);
    setTankSizeMismatch(false);
    showScanResult({ type: 'success', message: 'No shell mode enabled' });
    if (nameplateSerial) {
      setState('ReadyForInspection');
      setExternalInput(false);
    }
  }, [nameplateSerial, setExternalInput, showScanResult]);

  const scanNameplate = useCallback(async (serial: string) => {
    try {
      const np = await nameplateApi.getBySerial(serial);
      setNameplateSerial(serial);
      setNameplateTankSize(np.tankSize ?? null);
      showScanResult({ type: 'success', message: `Nameplate ${serial} found` });
      if (assemblyAlpha || noShellMode) {
        if (assemblyTankSize != null && np.tankSize != null && assemblyTankSize !== np.tankSize) {
          setTankSizeMismatch(true);
          showScanResult({ type: 'error', message: `Tank size mismatch: shell assembly is ${assemblyTankSize} gal but nameplate is ${np.tankSize} gal` });
          return;
        }
        setTankSizeMismatch(false);
        setState('ReadyForInspection');
        setExternalInput(false);
      }
    } catch {
      showScanResult({ type: 'error', message: 'Nameplate serial number not found' });
    }
  }, [assemblyAlpha, noShellMode, assemblyTankSize, showScanResult, setExternalInput]);

  const scanAuto = useCallback(async (serial: string) => {
    if ((assemblyAlpha || noShellMode) && !nameplateSerial) { await scanNameplate(serial); return; }
    if (nameplateSerial && !assemblyAlpha) { await scanShell(serial); return; }
    try {
      await roundSeamApi.getAssemblyByShell(serial);
      await scanShell(serial);
    } catch {
      await scanNameplate(serial);
    }
  }, [assemblyAlpha, noShellMode, nameplateSerial, scanShell, scanNameplate]);

  const parseShellScanCandidate = useCallback((value: string): string | null => {
    const trimmed = value.trim();
    if (!trimmed) return null;

    if (trimmed.toUpperCase().startsWith('SC;')) {
      return parseShellLabel(trimmed.substring(3)).serialNumber;
    }

    const parsed = parseShellLabel(trimmed);
    return parsed.labelSuffix ? parsed.serialNumber : null;
  }, []);

  const handleAccept = useCallback(async () => {
    const missingControlPlanResult = controlPlans.some((cp) => !inspectionResults[cp.id]);
    if (controlPlans.length > 0 && missingControlPlanResult) {
      showScanResult({ type: 'error', message: 'Complete all control plan results before saving' });
      return;
    }
    try {
      await hydroApi.create({
        assemblyAlphaCode: noShellMode ? '' : assemblyAlpha,
        nameplateSerialNumber: nameplateSerial,
        results: controlPlans.map(cp => ({
          controlPlanId: cp.id,
          resultText: inspectionResults[cp.id] || '',
        })).filter(r => r.resultText) as InspectionResultEntry[],
        workCenterId,
        productionLineId,
        assetId: assetId || undefined,
        operatorId,
        welderIds: props.welders.map((w) => w.userId),
        defects: defects.map((d) => ({ defectCodeId: d.defectCodeId, characteristicId: d.characteristicId, locationId: d.locationId })),
      });
      showScanResult({ type: 'success', message: defects.length > 0 ? `Accepted with ${defects.length} defect(s)` : 'Accepted — no defects' });
      refreshHistory();
      resetScreen();
    } catch {
      showScanResult({ type: 'error', message: 'Failed to save hydro record' });
    }
  }, [assemblyAlpha, noShellMode, nameplateSerial, workCenterId, productionLineId, assetId, operatorId, defects, controlPlans, inspectionResults, showScanResult, refreshHistory]);

  const resetScreen = useCallback(() => {
    setState('WaitingForScans');
    setShellSerial('');
    setNoShellMode(false);
    setAssemblyAlpha('');
    setAssemblyShells([]);
    setAssemblyTankSize(null);
    setNameplateSerial('');
    setNameplateTankSize(null);
    setDefects([]);
    setWizard(null);
    setManualShellInput('');
    setManualNameplateInput('');
    setInspectionResults({});
    setCurrentControlPlanIndex(0);
    setTankSizeMismatch(false);
    setExternalInput(true);
  }, [setExternalInput]);

  const openWizard = useCallback(() => {
    setState('DefectEntry');
    setWizard({ step: 'defect' });
  }, []);

  const selectDefectCode = useCallback((code: DefectCode) => {
    setWizard((prev) => prev ? { ...prev, step: 'characteristic', defectCodeId: code.id, defectCodeName: code.name } : null);
  }, []);

  const selectCharacteristic = useCallback((char: Characteristic) => {
    setWizard((prev) => prev ? { ...prev, step: 'location', characteristicId: char.id, characteristicName: char.name } : null);
  }, []);

  const selectLocation = useCallback((loc: DefectLocation) => {
    if (!wizard) return;
    setDefects((prev) => [...prev, {
      defectCodeId: wizard.defectCodeId!,
      defectCodeName: wizard.defectCodeName,
      characteristicId: wizard.characteristicId!,
      characteristicName: wizard.characteristicName,
      locationId: loc.id,
      locationName: loc.name,
    }]);
    setWizard(null);
    setState('ReadyForInspection');
  }, [wizard]);

  const removeDefect = useCallback((index: number) => {
    setDefects((prev) => prev.filter((_, i) => i !== index));
  }, []);

  const handleBarcode = useCallback(
    async (bc: ParsedBarcode | null, _raw: string) => {
      if (state === 'WaitingForScans') {
        if (bc?.prefix === 'SC') { await scanShell(parseShellLabel(bc.value).serialNumber); return; }
        if (bc?.prefix === 'NOSHELL' && bc.value === '0') { setNoShell(); return; }
        const unprefixedShellSerial = !bc ? parseShellScanCandidate(_raw) : null;
        if (unprefixedShellSerial) {
          await scanShell(unprefixedShellSerial);
          return;
        }

        const serial = bc ? (bc.value.includes(';') ? bc.value : _raw.replace(/^[^;]*;/, '')) : _raw.trim();
        if (serial) {
          await scanAuto(serial);
          return;
        }
      }
      showScanResult({ type: 'error', message: bc ? 'Invalid barcode in this context' : 'Unknown barcode' });
    },
    [state, parseShellScanCandidate, scanShell, scanAuto, setNoShell, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const normalizeShellManualValue = useCallback((value: string): string => {
    const trimmed = value.trim();
    if (!trimmed) return '';
    if (trimmed.toUpperCase().startsWith('SC;')) return parseShellLabel(trimmed.substring(3)).serialNumber;
    return parseShellLabel(trimmed).serialNumber;
  }, []);

  const handleShellManualSubmit = useCallback(() => {
    const serial = normalizeShellManualValue(manualShellInput);
    if (!serial) return;
    scanShell(serial);
    setManualShellInput('');
  }, [manualShellInput, normalizeShellManualValue, scanShell]);

  const handleNameplateManualSubmit = useCallback(() => {
    const serial = manualNameplateInput.trim();
    if (!serial) return;
    scanNameplate(serial);
    setManualNameplateInput('');
  }, [manualNameplateInput, scanNameplate]);

  const scanInstruction = (() => {
    if (state === 'WaitingForScans') {
      if (tankSizeMismatch) {
        return {
          title: 'NEXT: Tank size mismatch - rescan Shell or Nameplate',
          detail: `Shell: ${assemblyTankSize != null ? `${assemblyTankSize} gal` : '—'} | Nameplate: ${nameplateTankSize != null ? `${nameplateTankSize} gal` : '—'}`,
          isActive: true,
        };
      }

      if (!shellSerial && !noShellMode && !nameplateSerial) {
        return {
          title: 'NEXT: Scan Shell or Nameplate to begin',
          detail: '',
          isActive: false,
        };
      }

      if ((shellSerial || noShellMode) && !nameplateSerial) {
        return {
          title: 'NEXT: Scan Nameplate',
          detail: noShellMode ? 'No shell mode selected' : `Shell: ${shellSerial}`,
          isActive: true,
        };
      }

      if (!shellSerial && !noShellMode && nameplateSerial) {
        return {
          title: 'NEXT: Scan Shell',
          detail: `Nameplate: ${nameplateSerial}`,
          isActive: true,
        };
      }
    }

    if (state === 'DefectEntry') {
      const stepLabel = wizard?.step === 'defect'
        ? 'Select Defect'
        : wizard?.step === 'characteristic'
          ? 'Select Characteristic'
          : 'Select Location';
      return {
        title: 'NEXT: Complete defect wizard',
        detail: stepLabel,
        isActive: true,
      };
    }

    return {
      title: 'NEXT: Accept No Defects or Add Defect',
      detail: '',
      isActive: true,
    };
  })();

  if (state === 'DefectEntry' && wizard) {
    const filteredLocations = wizard.characteristicId
      ? defectLocations.filter((location) => supportsCharacteristic(location, wizard.characteristicId!))
      : [];

    return (
      <div className={styles.container}>
        <NextStepBanner instruction={scanInstruction} />
        <div className={styles.wizardHeader}>
          <span className={styles.breadcrumb}>
            {wizard.step === 'defect' && 'Step 1: Select Defect'}
            {wizard.step === 'characteristic' && `${wizard.defectCodeName} > Step 2: Select Characteristic`}
            {wizard.step === 'location' && `${wizard.defectCodeName} > ${wizard.characteristicName} > Step 3: Select Location`}
          </span>
          <div className={styles.wizardActions}>
            {wizard.step !== 'defect' && (
              <Button appearance="subtle" onClick={() => {
                if (wizard.step === 'location') setWizard((p) => p ? { ...p, step: 'characteristic' } : null);
                else setWizard({ step: 'defect' });
              }}>Back</Button>
            )}
            <Button appearance="subtle" onClick={() => { setWizard(null); setState('ReadyForInspection'); }}>Cancel</Button>
          </div>
        </div>
        <div className={styles.tileGrid}>
          {wizard.step === 'defect' && defectCodes.map((c) => (
            <button key={c.id} className={styles.tile} onClick={() => selectDefectCode(c)}>{c.name}</button>
          ))}
          {wizard.step === 'characteristic' && characteristics.map((c) => (
            <button key={c.id} className={styles.tile} onClick={() => selectCharacteristic(c)}>{c.name}</button>
          ))}
          {wizard.step === 'location' && filteredLocations.map((l) => (
            <button key={l.id} className={styles.tile} onClick={() => selectLocation(l)}>{l.name}</button>
          ))}
          {wizard.step === 'location' && filteredLocations.length === 0 && (
            <div className={styles.emptyWizardMessage}>No locations available for this characteristic.</div>
          )}
        </div>
      </div>
    );
  }

  if (state === 'ReadyForInspection') {
    const currentControlPlan = controlPlans[currentControlPlanIndex] ?? null;
    const [positiveLabel, negativeLabel] = currentControlPlan ? getResultLabels(currentControlPlan.resultType) : ['Pass', 'Fail'];
    const selectedResult = currentControlPlan ? inspectionResults[currentControlPlan.id] : undefined;
    const atFirstCharacteristic = currentControlPlanIndex === 0;
    const atLastCharacteristic = currentControlPlanIndex >= controlPlans.length - 1;
    const allControlPlansSelected = controlPlans.length === 0 || controlPlans.every((cp) => !!inspectionResults[cp.id]);

    return (
      <div className={styles.container}>
        <NextStepBanner instruction={scanInstruction} />
        <div className={styles.inspHeader}>
          <div className={styles.scanInfo}>
            <span>Nameplate SN: <strong>{nameplateSerial}</strong></span>
            <span>Shell No.: <strong>{noShellMode ? 'NO SHELL' : (shellSerial || '—')}</strong></span>
            <span>Assembly: <strong>{noShellMode ? 'NO SHELL' : `${assemblyAlpha}${assemblyShells.length > 0 ? ` (${assemblyShells.join(', ')})` : ''}`}</strong></span>
          </div>
          <Button appearance="subtle" onClick={resetScreen}>Reset</Button>
        </div>

        {controlPlans.length > 0 && currentControlPlan && (
          <div className={styles.characteristicCard}>
            <div className={styles.characteristicHeader}>
              {controlPlans.length > 1 && (
                <Button
                  appearance="subtle"
                  className={styles.arrowButton}
                  aria-label="Previous characteristic"
                  disabled={atFirstCharacteristic}
                  onClick={() => setCurrentControlPlanIndex((prev) => Math.max(prev - 1, 0))}
                >
                  {'<'}
                </Button>
              )}
              <div className={styles.characteristicTitleWrap}>
                <div className={styles.characteristicTitle}>Hydro Test</div>
              </div>
              {controlPlans.length > 1 && (
                <Button
                  appearance="subtle"
                  className={styles.arrowButton}
                  aria-label="Next characteristic"
                  disabled={atLastCharacteristic}
                  onClick={() => setCurrentControlPlanIndex((prev) => Math.min(prev + 1, controlPlans.length - 1))}
                >
                  {'>'}
                </Button>
              )}
            </div>

            <div className={styles.characteristicBody}>
              <div className={styles.responseButtons}>
                <Button
                  size="large"
                  className={`${styles.responseButton} ${styles.responseNeutral} ${selectedResult === positiveLabel ? styles.responseSelectedPositive : ''}`}
                  data-state={selectedResult === positiveLabel ? 'positive' : 'neutral'}
                  onClick={() => setInspectionResults((prev) => ({ ...prev, [currentControlPlan.id]: positiveLabel }))}
                >
                  {positiveLabel}
                </Button>
                <Button
                  size="large"
                  className={`${styles.responseButton} ${styles.responseNeutral} ${selectedResult === negativeLabel ? styles.responseSelectedNegative : ''}`}
                  data-state={selectedResult === negativeLabel ? 'negative' : 'neutral'}
                  onClick={() => setInspectionResults((prev) => ({ ...prev, [currentControlPlan.id]: negativeLabel }))}
                >
                  {negativeLabel}
                </Button>
              </div>
            </div>
          </div>
        )}

        {defects.length > 0 && (
          <div className={styles.defectTable}>
            <div className={styles.tableHeader}><span>Defect</span><span>Characteristic</span><span>Location</span><span></span></div>
            {defects.map((d, i) => (
              <div key={i} className={styles.tableRow}>
                <span>{d.defectCodeName}</span>
                <span>{d.characteristicName}</span>
                <span>{d.locationName}</span>
                <Button appearance="subtle" size="small" icon={<DeleteRegular />} aria-label="Delete defect" onClick={() => removeDefect(i)} />
              </div>
            ))}
          </div>
        )}

        <div className={styles.actionButtons}>
          {defects.length === 0 ? (
            <Button appearance="primary" size="large" className={styles.acceptBtn} onClick={handleAccept} disabled={!allControlPlansSelected}>
              No Defects Accept
            </Button>
          ) : (
            <Button appearance="primary" size="large" className={styles.acceptBtn} onClick={handleAccept} disabled={!allControlPlansSelected}>
              Save Defect(s)
            </Button>
          )}
          <Button appearance="secondary" size="large" onClick={openWizard}>
            Add Defect
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <NextStepBanner instruction={scanInstruction} />
      <div className={styles.manualEntry}>
        <div className={styles.manualCards}>
          <div className={`${styles.manualCard} ${styles.shellCard}`}>
            {tankSizeMismatch && <div className={styles.mismatchMark} aria-label="Tank size mismatch">✕</div>}
            <div className={styles.cardHeader}>
              <Label>Shell / Tank</Label>
              <div className={styles.shellBadge} aria-live="polite">
                <span className={styles.shellBadgeLabel}>Shell</span>
                <strong className={styles.shellBadgeValue}>{noShellMode ? 'NO SHELL' : (shellSerial || 'Not scanned')}</strong>
                <span className={styles.shellBadgeMeta}>Tank Size: {assemblyTankSize != null ? `${assemblyTankSize} gal` : '—'}</span>
              </div>
            </div>
            <svg viewBox="0 0 120 72" className={styles.manualSvg} aria-hidden="true">
              <rect x="6" y="16" width="108" height="40" rx="18" fill="#dfe7f6" stroke="#2b3b84" strokeWidth="3" />
              <line x1="24" y1="56" x2="24" y2="66" stroke="#2b3b84" strokeWidth="3" />
              <line x1="96" y1="56" x2="96" y2="66" stroke="#2b3b84" strokeWidth="3" />
            </svg>
            <div className={styles.manualRow}>
              {!props.externalInput && (
                <Button appearance="secondary" size="large" onClick={setNoShell}>No Shell</Button>
              )}
              <Input
                value={props.externalInput ? shellSerial : manualShellInput}
                onChange={(_, d) => setManualShellInput(d.value)}
                placeholder={props.externalInput ? 'Awaiting shell scan...' : 'Enter shell serial and press Enter...'}
                size="large"
                className={styles.manualInput}
                readOnly={props.externalInput}
                onKeyDown={(e) => { if (e.key === 'Enter' && !props.externalInput) handleShellManualSubmit(); }}
              />
              {!props.externalInput && (
                <Button appearance="primary" size="large" onClick={handleShellManualSubmit}>Submit</Button>
              )}
            </div>
          </div>

          <div className={styles.manualCard}>
            {tankSizeMismatch && <div className={styles.mismatchMark} aria-label="Tank size mismatch">✕</div>}
            <div className={styles.cardHeader}>
              <Label>Tank Nameplate</Label>
              <div className={styles.shellBadge} aria-live="polite">
                <span className={styles.shellBadgeLabel}>Nameplate</span>
                <strong className={styles.shellBadgeValue}>{nameplateSerial || 'Not scanned'}</strong>
                <span className={styles.shellBadgeMeta}>Tank Size: {nameplateTankSize != null ? `${nameplateTankSize} gal` : '—'}</span>
              </div>
            </div>
            <svg viewBox="0 0 120 72" className={styles.manualSvg} aria-hidden="true">
              <rect x="18" y="12" width="84" height="48" rx="4" fill="#fff3cd" stroke="#8a6d3b" strokeWidth="3" />
              <line x1="28" y1="26" x2="92" y2="26" stroke="#8a6d3b" strokeWidth="3" />
              <line x1="28" y1="36" x2="92" y2="36" stroke="#8a6d3b" strokeWidth="3" />
              <line x1="28" y1="46" x2="74" y2="46" stroke="#8a6d3b" strokeWidth="3" />
            </svg>
            <div className={styles.manualRow}>
              <Input
                value={props.externalInput ? nameplateSerial : manualNameplateInput}
                onChange={(_, d) => setManualNameplateInput(d.value)}
                placeholder={props.externalInput ? 'Awaiting nameplate scan...' : 'Enter nameplate serial and press Enter...'}
                size="large"
                className={styles.manualInput}
                readOnly={props.externalInput}
                onKeyDown={(e) => { if (e.key === 'Enter' && !props.externalInput) handleNameplateManualSubmit(); }}
              />
              {!props.externalInput && (
                <Button appearance="primary" size="large" onClick={handleNameplateManualSubmit}>Submit</Button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
