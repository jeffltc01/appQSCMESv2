import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { DefectCode, Characteristic, DefectLocation, DefectEntry } from '../../types/domain.ts';
import { roundSeamApi, nameplateApi, workCenterApi, hydroApi } from '../../api/endpoints.ts';
import styles from './HydroScreen.module.css';

type HydroState = 'WaitingForScans' | 'ReadyForInspection' | 'DefectEntry';

interface DefectWizardState {
  step: 'defect' | 'characteristic' | 'location';
  defectCodeId?: string;
  defectCodeName?: string;
  characteristicId?: string;
  characteristicName?: string;
}

export function HydroScreen(props: WorkCenterProps) {
  const {
    workCenterId, productionLineId, assetId, operatorId,
    setExternalInput, showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [state, setState] = useState<HydroState>('WaitingForScans');
  const [shellSerial, setShellSerial] = useState('');
  const [assemblyAlpha, setAssemblyAlpha] = useState('');
  const [assemblyShells, setAssemblyShells] = useState<string[]>([]);
  const [assemblyTankSize, setAssemblyTankSize] = useState<number | null>(null);
  const [nameplateSerial, setNameplateSerial] = useState('');
  const [nameplateTankSize, setNameplateTankSize] = useState<number | null>(null);
  const [defects, setDefects] = useState<DefectEntry[]>([]);
  const [wizard, setWizard] = useState<DefectWizardState | null>(null);

  const [defectCodes, setDefectCodes] = useState<DefectCode[]>([]);
  const [characteristics, setCharacteristics] = useState<Characteristic[]>([]);
  const [locations, setLocations] = useState<DefectLocation[]>([]);
  const [manualInput, setManualInput] = useState('');

  useEffect(() => {
    loadLookups();
  }, [workCenterId]);

  const loadLookups = useCallback(async () => {
    try {
      const [codes, chars] = await Promise.all([
        workCenterApi.getDefectCodes(workCenterId),
        workCenterApi.getCharacteristics(workCenterId),
      ]);
      setDefectCodes(codes);
      setCharacteristics(chars);
    } catch { /* keep empty */ }
  }, [workCenterId]);

  const scanShell = useCallback(async (serial: string) => {
    try {
      const assembly = await roundSeamApi.getAssemblyByShell(serial);
      setShellSerial(serial);
      setAssemblyAlpha(assembly.alphaCode);
      setAssemblyShells(assembly.shells ?? []);
      setAssemblyTankSize(assembly.tankSize ?? null);
      showScanResult({ type: 'success', message: `Assembly ${assembly.alphaCode} found` });
      if (nameplateSerial) {
        if (nameplateTankSize != null && assembly.tankSize != null && assembly.tankSize !== nameplateTankSize) {
          showScanResult({ type: 'error', message: `Tank size mismatch: shell assembly is ${assembly.tankSize} gal but nameplate is ${nameplateTankSize} gal` });
          return;
        }
        setState('ReadyForInspection');
        setExternalInput(false);
      }
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Shell not found in any assembly' });
    }
  }, [nameplateSerial, nameplateTankSize, showScanResult, setExternalInput]);

  const scanNameplate = useCallback(async (serial: string) => {
    try {
      const np = await nameplateApi.getBySerial(serial);
      setNameplateSerial(serial);
      setNameplateTankSize(np.tankSize ?? null);
      showScanResult({ type: 'success', message: `Nameplate ${serial} found` });
      if (assemblyAlpha) {
        if (assemblyTankSize != null && np.tankSize != null && assemblyTankSize !== np.tankSize) {
          showScanResult({ type: 'error', message: `Tank size mismatch: shell assembly is ${assemblyTankSize} gal but nameplate is ${np.tankSize} gal` });
          return;
        }
        setState('ReadyForInspection');
        setExternalInput(false);
      }
    } catch {
      showScanResult({ type: 'error', message: 'Nameplate serial number not found' });
    }
  }, [assemblyAlpha, assemblyTankSize, showScanResult, setExternalInput]);

  const handleAccept = useCallback(async () => {
    try {
      await hydroApi.create({
        assemblyAlphaCode: assemblyAlpha,
        nameplateSerialNumber: nameplateSerial,
        result: defects.length > 0 ? 'ACCEPTED' : 'ACCEPTED',
        workCenterId,
        productionLineId,
        assetId: assetId || undefined,
        operatorId,
        defects: defects.map((d) => ({ defectCodeId: d.defectCodeId, characteristicId: d.characteristicId, locationId: d.locationId })),
      });
      showScanResult({ type: 'success', message: defects.length > 0 ? `Accepted with ${defects.length} defect(s)` : 'Accepted — no defects' });
      refreshHistory();
      resetScreen();
    } catch {
      showScanResult({ type: 'error', message: 'Failed to save hydro record' });
    }
  }, [assemblyAlpha, nameplateSerial, workCenterId, assetId, operatorId, defects, showScanResult, refreshHistory]);

  const resetScreen = useCallback(() => {
    setState('WaitingForScans');
    setShellSerial('');
    setAssemblyAlpha('');
    setAssemblyShells([]);
    setAssemblyTankSize(null);
    setNameplateSerial('');
    setNameplateTankSize(null);
    setDefects([]);
    setWizard(null);
    setManualInput('');
    setExternalInput(true);
  }, [setExternalInput]);

  const openWizard = useCallback(() => {
    setState('DefectEntry');
    setWizard({ step: 'defect' });
  }, []);

  const selectDefectCode = useCallback((code: DefectCode) => {
    setWizard((prev) => prev ? { ...prev, step: 'characteristic', defectCodeId: code.id, defectCodeName: code.name } : null);
  }, []);

  const selectCharacteristic = useCallback(async (char: Characteristic) => {
    setWizard((prev) => prev ? { ...prev, step: 'location', characteristicId: char.id, characteristicName: char.name } : null);
    try {
      const locs = await hydroApi.getLocationsByCharacteristic(char.id);
      setLocations(locs);
    } catch { setLocations([]); }
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
    (bc: ParsedBarcode | null, _raw: string) => {
      if (state === 'WaitingForScans') {
        if (bc?.prefix === 'SC') { scanShell(parseShellLabel(bc.value).serialNumber); return; }
        if (bc?.prefix === 'NOSHELL' && bc.value === '0') { setShellSerial(''); showScanResult({ type: 'success', message: 'No shell mode' }); return; }
        const serial = bc ? (bc.value.includes(';') ? bc.value : _raw.replace(/^[^;]*;/, '')) : _raw.trim();
        if (serial) {
          if (assemblyAlpha) { scanNameplate(serial); }
          else { scanShell(serial); }
          return;
        }
      }
      showScanResult({ type: 'error', message: bc ? 'Invalid barcode in this context' : 'Unknown barcode' });
    },
    [state, assemblyAlpha, scanShell, scanNameplate, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualSubmit = useCallback(() => {
    if (!manualInput.trim()) return;
    const val = manualInput.trim();
    if (val.startsWith('SC;')) { scanShell(parseShellLabel(val.substring(3)).serialNumber); }
    else if (!assemblyAlpha) { scanShell(val); }
    else { scanNameplate(val); }
    setManualInput('');
  }, [manualInput, assemblyAlpha, scanShell, scanNameplate]);

  if (state === 'DefectEntry' && wizard) {
    return (
      <div className={styles.container}>
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
          {wizard.step === 'location' && locations.map((l) => (
            <button key={l.id} className={styles.tile} onClick={() => selectLocation(l)}>{l.name}</button>
          ))}
        </div>
      </div>
    );
  }

  if (state === 'ReadyForInspection') {
    return (
      <div className={styles.container}>
        <div className={styles.inspHeader}>
          <div className={styles.scanInfo}>
            <span>Nameplate SN: <strong>{nameplateSerial}</strong></span>
            <span>Shell No.: <strong>{shellSerial || '—'}</strong></span>
            <span>Assembly: <strong>{assemblyAlpha}{assemblyShells.length > 0 ? ` (${assemblyShells.join(', ')})` : ''}</strong></span>
          </div>
          <Button appearance="subtle" onClick={resetScreen}>Reset</Button>
        </div>

        {defects.length > 0 && (
          <div className={styles.defectTable}>
            <div className={styles.tableHeader}><span>Defect</span><span>Characteristic</span><span>Location</span><span></span></div>
            {defects.map((d, i) => (
              <div key={i} className={styles.tableRow}>
                <span>{d.defectCodeName}</span>
                <span>{d.characteristicName}</span>
                <span>{d.locationName}</span>
                <Button appearance="subtle" size="small" onClick={() => removeDefect(i)}>🗑</Button>
              </div>
            ))}
          </div>
        )}

        <div className={styles.actionButtons}>
          {defects.length === 0 ? (
            <Button appearance="primary" size="large" className={styles.acceptBtn} onClick={handleAccept}>
              No Defects - Accept
            </Button>
          ) : (
            <Button appearance="primary" size="large" className={styles.acceptBtn} onClick={handleAccept}>
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
      <div className={styles.waitingPrompt}>Scan Shell or Nameplate to begin...</div>
      <div className={styles.scanStatus}>
        <span>Shell: {shellSerial ? `✓ ${shellSerial}` : 'Not scanned'}</span>
        <span>Nameplate: {nameplateSerial ? `✓ ${nameplateSerial}` : 'Not scanned'}</span>
      </div>
      {!props.externalInput && (
        <div className={styles.manualEntry}>
          <Label>Enter Shell or Nameplate Serial</Label>
          <div className={styles.manualRow}>
            <Input value={manualInput} onChange={(_, d) => setManualInput(d.value)} placeholder="Enter serial..." size="large" className={styles.manualInput} onKeyDown={(e) => { if (e.key === 'Enter') handleManualSubmit(); }} />
            <Button appearance="primary" size="large" onClick={handleManualSubmit}>Submit</Button>
          </div>
        </div>
      )}
    </div>
  );
}
