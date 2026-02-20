import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel, parseFullDefect } from '../../types/barcode.ts';
import type { DefectCode, DefectLocation, Characteristic, DefectEntry } from '../../types/domain.ts';
import { roundSeamApi, workCenterApi, inspectionRecordApi } from '../../api/endpoints.ts';
import styles from './RoundSeamInspScreen.module.css';

type ScreenState = 'WaitingForShell' | 'AwaitingDefects';

interface PendingDefect {
  defectCodeId?: string;
  defectCodeName?: string;
  characteristicId?: string;
  characteristicName?: string;
  locationId?: string;
  locationName?: string;
}

export function RoundSeamInspScreen(props: WorkCenterProps) {
  const {
    workCenterId, operatorId,
    showScanResult, refreshHistory, registerBarcodeHandler, setRequiresWelder,
  } = props;

  const [screenState, setScreenState] = useState<ScreenState>('WaitingForShell');
  const [alphaCode, setAlphaCode] = useState('');
  const [tankSize, setTankSize] = useState<number>(0);
  const [defects, setDefects] = useState<DefectEntry[]>([]);
  const [pending, setPending] = useState<PendingDefect>({});

  const [defectCodes, setDefectCodes] = useState<DefectCode[]>([]);
  const [defectLocations, setDefectLocations] = useState<DefectLocation[]>([]);
  const [characteristics, setCharacteristics] = useState<Characteristic[]>([]);

  const [manualSerial, setManualSerial] = useState('');
  const [manualDefectCode, setManualDefectCode] = useState('');
  const [manualCharacteristic, setManualCharacteristic] = useState('');
  const [manualLocation, setManualLocation] = useState('');

  useEffect(() => {
    setRequiresWelder(false);
    loadLookups();
  }, [workCenterId]);

  const loadLookups = useCallback(async () => {
    try {
      const [codes, locs, chars] = await Promise.all([
        workCenterApi.getDefectCodes(workCenterId),
        workCenterApi.getDefectLocations(workCenterId),
        workCenterApi.getCharacteristics(workCenterId),
      ]);
      setDefectCodes(codes);
      setDefectLocations(locs);
      setCharacteristics(chars);
    } catch { /* keep empty */ }
  }, [workCenterId]);

  const loadShell = useCallback(async (serial: string) => {
    try {
      const assembly = await roundSeamApi.getAssemblyByShell(serial);
      setAlphaCode(assembly.alphaCode);
      setTankSize(assembly.tankSize);
      setScreenState('AwaitingDefects');
      setDefects([]);
      setPending({});
      showScanResult({ type: 'success', message: `Assembly ${assembly.alphaCode} loaded` });
    } catch (err: any) {
      showScanResult({ type: 'error', message: err?.message ?? 'Shell is not part of any assembly' });
    }
  }, [showScanResult]);

  const addDefectEntry = useCallback(
    (defectCodeId: string, defectCodeName: string, charId: string, charName: string, locId: string, locName: string) => {
      setDefects((prev) => [...prev, { defectCodeId, defectCodeName, characteristicId: charId, characteristicName: charName, locationId: locId, locationName: locName }]);
      setPending({});
    }, [],
  );

  const tryCompletePending = useCallback((upd: PendingDefect) => {
    if (upd.defectCodeId && upd.characteristicId && upd.locationId) {
      addDefectEntry(upd.defectCodeId, upd.defectCodeName ?? '', upd.characteristicId, upd.characteristicName ?? '', upd.locationId, upd.locationName ?? '');
      return true;
    }
    setPending(upd);
    return false;
  }, [addDefectEntry]);

  const saveInspection = useCallback(async () => {
    try {
      await inspectionRecordApi.create({
        serialNumber: alphaCode,
        workCenterId,
        operatorId,
        defects: defects.map((d) => ({ defectCodeId: d.defectCodeId, characteristicId: d.characteristicId, locationId: d.locationId })),
      });
      showScanResult({ type: 'success', message: defects.length > 0 ? `Inspection saved with ${defects.length} defect(s)` : 'Inspection saved — clean pass' });
      refreshHistory();
      setScreenState('WaitingForShell');
      setAlphaCode('');
      setDefects([]);
      setPending({});
    } catch {
      showScanResult({ type: 'error', message: 'Failed to save inspection record' });
    }
  }, [alphaCode, workCenterId, operatorId, defects, showScanResult, refreshHistory]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) { showScanResult({ type: 'error', message: 'Unknown barcode' }); return; }
      if (screenState === 'WaitingForShell') {
        if (bc.prefix === 'SC') { loadShell(parseShellLabel(bc.value).serialNumber); return; }
        showScanResult({ type: 'error', message: 'Scan a shell label to begin' }); return;
      }
      if (screenState === 'AwaitingDefects') {
        if (bc.prefix === 'SC') { showScanResult({ type: 'error', message: 'Save or clear current assembly before scanning a new one' }); return; }
        if (bc.prefix === 'S' && bc.value === '1') { saveInspection(); return; }
        if (bc.prefix === 'CL' && bc.value === '1') { setDefects([]); setPending({}); showScanResult({ type: 'success', message: 'Defects cleared' }); return; }
        if (bc.prefix === 'D') {
          const code = defectCodes.find((c) => c.id === bc.value || c.code === bc.value);
          if (!code) { showScanResult({ type: 'error', message: 'Defect code not applicable' }); return; }
          tryCompletePending({ ...pending, defectCodeId: code.id, defectCodeName: code.name }); return;
        }
        if (bc.prefix === 'L') {
          const parts = bc.value.split(';C;');
          const loc = defectLocations.find((l) => l.id === parts[0] || l.code === parts[0]);
          if (!loc) { showScanResult({ type: 'error', message: 'Location not applicable' }); return; }
          let charMatch: Characteristic | undefined;
          if (parts.length > 1) charMatch = characteristics.find((c) => c.id === parts[1] || c.name === parts[1]);
          tryCompletePending({ ...pending, locationId: loc.id, locationName: loc.name, characteristicId: charMatch?.id ?? pending.characteristicId, characteristicName: charMatch?.name ?? pending.characteristicName }); return;
        }
        if (bc.prefix === 'FD') {
          const fd = parseFullDefect(bc.value);
          if (!fd) { showScanResult({ type: 'error', message: 'Invalid full defect format' }); return; }
          const code = defectCodes.find((c) => c.id === fd.defectCode || c.code === fd.defectCode);
          const char = characteristics.find((c) => c.id === fd.characteristic || c.name === fd.characteristic);
          const loc = defectLocations.find((l) => l.id === fd.location || l.code === fd.location);
          if (!code || !loc) { showScanResult({ type: 'error', message: 'Invalid defect or location' }); return; }
          addDefectEntry(code.id, code.name, char?.id ?? '', char?.name ?? '', loc.id, loc.name); return;
        }
      }
      showScanResult({ type: 'error', message: 'Invalid barcode in this context' });
    },
    [screenState, pending, defectCodes, defectLocations, characteristics, loadShell, tryCompletePending, saveInspection, showScanResult, addDefectEntry],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualShellSubmit = useCallback(() => { if (manualSerial.trim()) { loadShell(manualSerial.trim()); setManualSerial(''); } }, [manualSerial, loadShell]);
  const handleManualDefectAdd = useCallback(() => {
    if (manualDefectCode && manualCharacteristic && manualLocation) {
      const code = defectCodes.find((c) => c.id === manualDefectCode);
      const char = characteristics.find((c) => c.id === manualCharacteristic);
      const loc = defectLocations.find((l) => l.id === manualLocation);
      if (code && char && loc) { addDefectEntry(code.id, code.name, char.id, char.name, loc.id, loc.name); setManualDefectCode(''); setManualCharacteristic(''); setManualLocation(''); }
    }
  }, [manualDefectCode, manualCharacteristic, manualLocation, defectCodes, characteristics, defectLocations, addDefectEntry]);

  if (screenState === 'WaitingForShell') {
    return (
      <div className={styles.container}>
        <div className={styles.prompt}>Scan Serial Number to begin...</div>
        <div className={styles.form}>
          <Label className={styles.label}>Serial Number</Label>
          <Input value={manualSerial} onChange={(_, d) => setManualSerial(d.value)} placeholder="enter serial number" size="large" className={styles.input} onKeyDown={(e) => { if (e.key === 'Enter') handleManualShellSubmit(); }} disabled={props.externalInput} />
          <Button appearance="primary" size="large" className={styles.submitBtn} onClick={handleManualShellSubmit} disabled={props.externalInput || !manualSerial.trim()}>Submit</Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.stateLabel}>AwaitingDefects</span>
        <span>Assembly <strong>{alphaCode}</strong></span>
        <span>Tank Size <strong>{tankSize}</strong></span>
      </div>
      <div className={styles.defectTable}>
        <div className={styles.tableHeader}><span>Defect</span><span>Characteristic</span><span>Location</span></div>
        {defects.map((d, i) => (<div key={i} className={styles.tableRow}><span>{d.defectCodeName}</span><span>{d.characteristicName}</span><span>{d.locationName}</span></div>))}
        {defects.length === 0 && <div className={styles.emptyRow}>No defects — scan Save for clean pass</div>}
      </div>
      {!props.externalInput && (
        <div className={styles.manualDefects}>
          <div className={styles.manualRow}>
            <Dropdown placeholder="Defect Code" value={defectCodes.find((c) => c.id === manualDefectCode)?.name ?? ''} selectedOptions={[manualDefectCode]} onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setManualDefectCode(d.optionValue); }} className={styles.dropdown}>
              {defectCodes.map((c) => <Option key={c.id} value={c.id}>{c.name}</Option>)}
            </Dropdown>
            <Dropdown placeholder="Characteristic" value={characteristics.find((c) => c.id === manualCharacteristic)?.name ?? ''} selectedOptions={[manualCharacteristic]} onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setManualCharacteristic(d.optionValue); }} className={styles.dropdown}>
              {characteristics.map((c) => <Option key={c.id} value={c.id}>{c.name}</Option>)}
            </Dropdown>
            <Dropdown placeholder="Location" value={defectLocations.find((l) => l.id === manualLocation)?.name ?? ''} selectedOptions={[manualLocation]} onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setManualLocation(d.optionValue); }} className={styles.dropdown}>
              {defectLocations.map((l) => <Option key={l.id} value={l.id}>{l.name}</Option>)}
            </Dropdown>
            <Button appearance="primary" onClick={handleManualDefectAdd} disabled={!manualDefectCode || !manualCharacteristic || !manualLocation}>Add</Button>
          </div>
          <div className={styles.actionRow}>
            <Button appearance="primary" size="large" onClick={saveInspection} className={styles.submitBtn}>Save</Button>
            <Button appearance="secondary" size="large" onClick={() => { setDefects([]); setPending({}); }}>Clear All</Button>
          </div>
        </div>
      )}
    </div>
  );
}
