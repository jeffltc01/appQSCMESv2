import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel, parseFullDefect } from '../../types/barcode.ts';
import type { DefectCode, DefectLocation, Characteristic, DefectEntry } from '../../types/domain.ts';
import { serialNumberApi, workCenterApi, inspectionRecordApi } from '../../api/endpoints.ts';
import styles from './LongSeamInspScreen.module.css';

type ScreenState = 'WaitingForShell' | 'AwaitingDefects';

interface PendingDefect {
  defectCodeId?: string;
  defectCodeName?: string;
  locationId?: string;
  locationName?: string;
}

export function LongSeamInspScreen(props: WorkCenterProps) {
  const {
    workCenterId, operatorId,
    showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [screenState, setScreenState] = useState<ScreenState>('WaitingForShell');
  const [serialNumber, setSerialNumber] = useState('');
  const [tankSize, setTankSize] = useState<number | null>(null);
  const [defects, setDefects] = useState<DefectEntry[]>([]);
  const [pending, setPending] = useState<PendingDefect>({});

  const [defectCodes, setDefectCodes] = useState<DefectCode[]>([]);
  const [defectLocations, setDefectLocations] = useState<DefectLocation[]>([]);
  const [characteristics, setCharacteristics] = useState<Characteristic[]>([]);
  const [assumedCharacteristic, setAssumedCharacteristic] = useState<Characteristic | null>(null);

  const [manualSerial, setManualSerial] = useState('');
  const [manualDefectCode, setManualDefectCode] = useState('');
  const [manualLocation, setManualLocation] = useState('');

  useEffect(() => {
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
      if (chars.length > 0) setAssumedCharacteristic(chars[0]);
    } catch { /* keep empty */ }
  }, [workCenterId]);

  const loadShell = useCallback(
    async (serial: string) => {
      try {
        const ctx = await serialNumberApi.getContext(serial);
        setSerialNumber(serial);
        setTankSize(ctx.tankSize);
        setScreenState('AwaitingDefects');
        setDefects([]);
        setPending({});
        showScanResult({ type: 'success', message: `Shell ${serial} loaded` });
      } catch {
        showScanResult({ type: 'error', message: 'Failed to load shell' });
      }
    },
    [showScanResult],
  );

  const addDefectEntry = useCallback(
    (defectCodeId: string, defectCodeName: string, locationId: string, locationName: string) => {
      setDefects((prev) => [
        ...prev,
        {
          defectCodeId,
          defectCodeName,
          characteristicId: assumedCharacteristic?.id ?? '',
          characteristicName: assumedCharacteristic?.name ?? 'Long Seam',
          locationId,
          locationName,
        },
      ]);
      setPending({});
    },
    [assumedCharacteristic],
  );

  const tryCompletePending = useCallback(
    (upd: PendingDefect) => {
      if (upd.defectCodeId && upd.locationId) {
        addDefectEntry(
          upd.defectCodeId,
          upd.defectCodeName ?? '',
          upd.locationId,
          upd.locationName ?? '',
        );
        return true;
      }
      setPending(upd);
      return false;
    },
    [addDefectEntry],
  );

  const saveInspection = useCallback(async () => {
    try {
      await inspectionRecordApi.create({
        serialNumber,
        workCenterId,
        operatorId,
        defects: defects.map((d) => ({
          defectCodeId: d.defectCodeId,
          characteristicId: d.characteristicId,
          locationId: d.locationId,
        })),
      });
      showScanResult({
        type: 'success',
        message: defects.length > 0
          ? `Inspection saved with ${defects.length} defect(s)`
          : 'Inspection saved — clean pass',
      });
      refreshHistory();
      setScreenState('WaitingForShell');
      setSerialNumber('');
      setTankSize(null);
      setDefects([]);
      setPending({});
    } catch {
      showScanResult({ type: 'error', message: 'Failed to save inspection record. Please try again.' });
    }
  }, [serialNumber, workCenterId, operatorId, defects, showScanResult, refreshHistory]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) {
        showScanResult({ type: 'error', message: 'Unknown barcode' });
        return;
      }

      if (screenState === 'WaitingForShell') {
        if (bc.prefix === 'SC') {
          const { serialNumber: serial } = parseShellLabel(bc.value);
          loadShell(serial);
          return;
        }
        showScanResult({ type: 'error', message: 'Scan a shell label to begin' });
        return;
      }

      if (screenState === 'AwaitingDefects') {
        if (bc.prefix === 'SC') {
          showScanResult({ type: 'error', message: 'Save or clear current shell before scanning a new one' });
          return;
        }

        if (bc.prefix === 'S' && bc.value === '1') {
          if (pending.defectCodeId && !pending.locationId) {
            showScanResult({ type: 'error', message: 'Incomplete defect entry — add location or clear' });
            return;
          }
          if (!pending.defectCodeId && pending.locationId) {
            showScanResult({ type: 'error', message: 'Incomplete defect entry — add defect code or clear' });
            return;
          }
          saveInspection();
          return;
        }

        if (bc.prefix === 'CL' && bc.value === '1') {
          setDefects([]);
          setPending({});
          showScanResult({ type: 'success', message: 'Defects cleared' });
          return;
        }

        if (bc.prefix === 'D') {
          const code = defectCodes.find((c) => c.id === bc.value || c.code === bc.value);
          if (!code) {
            showScanResult({ type: 'error', message: 'Defect code not applicable at this work center' });
            return;
          }
          tryCompletePending({ ...pending, defectCodeId: code.id, defectCodeName: code.name });
          return;
        }

        if (bc.prefix === 'L') {
          const loc = defectLocations.find((l) => l.id === bc.value || l.code === bc.value);
          if (!loc) {
            showScanResult({ type: 'error', message: 'Location not applicable at this work center' });
            return;
          }
          tryCompletePending({ ...pending, locationId: loc.id, locationName: loc.name });
          return;
        }

        if (bc.prefix === 'FD') {
          const fd = parseFullDefect(bc.value);
          if (!fd) {
            showScanResult({ type: 'error', message: 'Invalid full defect format' });
            return;
          }
          const code = defectCodes.find((c) => c.id === fd.defectCode || c.code === fd.defectCode);
          const loc = defectLocations.find((l) => l.id === fd.location || l.code === fd.location);
          const char = characteristics.find((ch) => ch.id === fd.characteristic || ch.code === fd.characteristic) ?? assumedCharacteristic;
          if (!code || !loc) {
            showScanResult({ type: 'error', message: 'Invalid defect or location in full defect barcode' });
            return;
          }
          setDefects((prev) => [
            ...prev,
            {
              defectCodeId: code.id,
              defectCodeName: code.name,
              characteristicId: char?.id ?? '',
              characteristicName: char?.name ?? '',
              locationId: loc.id,
              locationName: loc.name,
            },
          ]);
          return;
        }
      }

      showScanResult({ type: 'error', message: 'Invalid barcode in this context' });
    },
    [screenState, pending, defectCodes, defectLocations, characteristics, assumedCharacteristic, loadShell, tryCompletePending, saveInspection, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const handleManualShellSubmit = useCallback(() => {
    if (manualSerial.trim()) {
      loadShell(manualSerial.trim());
      setManualSerial('');
    }
  }, [manualSerial, loadShell]);

  const handleManualDefectAdd = useCallback(() => {
    if (manualDefectCode && manualLocation) {
      const code = defectCodes.find((c) => c.id === manualDefectCode);
      const loc = defectLocations.find((l) => l.id === manualLocation);
      if (code && loc) {
        addDefectEntry(code.id, code.name, loc.id, loc.name);
        setManualDefectCode('');
        setManualLocation('');
      }
    }
  }, [manualDefectCode, manualLocation, defectCodes, defectLocations, addDefectEntry]);

  if (screenState === 'WaitingForShell') {
    return (
      <div className={styles.container}>
        <div className={styles.prompt}>Scan Serial Number to begin...</div>
        <div className={styles.form}>
          <Label className={styles.label}>Serial Number</Label>
          <Input
            value={manualSerial}
            onChange={(_, d) => setManualSerial(d.value)}
            placeholder="enter serial number"
            size="large"
            className={styles.input}
            onKeyDown={(e) => { if (e.key === 'Enter') handleManualShellSubmit(); }}
            disabled={props.externalInput}
          />
          <Button
            appearance="primary" size="large" className={styles.submitBtn}
            onClick={handleManualShellSubmit}
            disabled={props.externalInput || !manualSerial.trim()}
          >
            Submit
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.stateLabel}>AwaitingDefects</span>
        <span>Serial No. <strong>{serialNumber}</strong></span>
        <span>Tank Size <strong>{tankSize ?? '—'}</strong></span>
      </div>

      <div className={styles.defectTable}>
        <div className={styles.tableHeader}>
          <span>Defect</span>
          <span>Characteristic</span>
          <span>Location</span>
        </div>
        {defects.map((d, i) => (
          <div key={i} className={styles.tableRow}>
            <span>{d.defectCodeName}</span>
            <span>{d.characteristicName}</span>
            <span>{d.locationName}</span>
          </div>
        ))}
        {defects.length === 0 && (
          <div className={styles.emptyRow}>No defects — scan Save for clean pass</div>
        )}
      </div>

      {pending.defectCodeId && !pending.locationId && (
        <div className={styles.pendingHint}>Defect scanned: {pending.defectCodeName} — scan Location</div>
      )}
      {!pending.defectCodeId && pending.locationId && (
        <div className={styles.pendingHint}>Location scanned: {pending.locationName} — scan Defect Code</div>
      )}

      {!props.externalInput && (
        <div className={styles.manualDefects}>
          <div className={styles.manualRow}>
            <Dropdown
              placeholder="Defect Code"
              value={defectCodes.find((c) => c.id === manualDefectCode)?.name ?? ''}
              selectedOptions={[manualDefectCode]}
              onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setManualDefectCode(d.optionValue); }}
              className={styles.dropdown}
            >
              {defectCodes.map((c) => (
                <Option key={c.id} value={c.id}>{c.name}</Option>
              ))}
            </Dropdown>
            <Dropdown
              placeholder="Location"
              value={defectLocations.find((l) => l.id === manualLocation)?.name ?? ''}
              selectedOptions={[manualLocation]}
              onOptionSelect={(_: unknown, d: OptionOnSelectData) => { if (d.optionValue) setManualLocation(d.optionValue); }}
              className={styles.dropdown}
            >
              {defectLocations.map((l) => (
                <Option key={l.id} value={l.id}>{l.name}</Option>
              ))}
            </Dropdown>
            <Button appearance="primary" onClick={handleManualDefectAdd} disabled={!manualDefectCode || !manualLocation}>
              Add
            </Button>
          </div>
          <div className={styles.actionRow}>
            <Button appearance="primary" size="large" onClick={saveInspection} className={styles.submitBtn}>
              Save
            </Button>
            <Button appearance="secondary" size="large" onClick={() => { setDefects([]); setPending({}); }}>
              Clear All
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
