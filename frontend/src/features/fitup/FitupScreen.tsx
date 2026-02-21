import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { MaterialQueueItem, HeadLotInfo } from '../../types/domain.ts';
import { serialNumberApi, materialQueueApi, workCenterApi, assemblyApi } from '../../api/endpoints.ts';
import styles from './FitupScreen.module.css';

function shellCountForSize(tankSize: number): number {
  if (tankSize >= 1500) return 3;
  if (tankSize >= 1000) return 2;
  return 1;
}

interface ShellSlot {
  serial: string;
  filled: boolean;
}

export function FitupScreen(props: WorkCenterProps) {
  const {
    workCenterId, assetId, productionLineId, operatorId, welders,
    showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;

  const [tankSize, setTankSize] = useState<number>(0);
  const [shells, setShells] = useState<ShellSlot[]>([]);
  const [leftHead, setLeftHead] = useState<HeadLotInfo | null>(null);
  const [rightHead, setRightHead] = useState<HeadLotInfo | null>(null);
  const [headScanCount, setHeadScanCount] = useState(0);
  const [alphaCode, setAlphaCode] = useState<string | null>(null);
  const [alphaTimer, setAlphaTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  const [headsQueue, setHeadsQueue] = useState<MaterialQueueItem[]>([]);
  const [manualSerial, setManualSerial] = useState('');

  const [reassemblyPrompt, setReassemblyPrompt] = useState<{
    alphaCode: string;
    serial: string;
  } | null>(null);

  useEffect(() => {
    loadHeadsQueue();
  }, [workCenterId]);

  const loadHeadsQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(workCenterId, 'heads');
      setHeadsQueue(items.filter((i) => i.status === 'queued'));
    } catch { /* keep stale */ }
  }, [workCenterId]);

  const resetAssembly = useCallback(() => {
    setTankSize(0);
    setShells([]);
    setLeftHead(null);
    setRightHead(null);
    setHeadScanCount(0);
    setReassemblyPrompt(null);
    if (alphaTimer) clearTimeout(alphaTimer);
    setAlphaCode(null);
  }, [alphaTimer]);

  const updateTankSize = useCallback((size: number) => {
    const count = shellCountForSize(size);
    setTankSize(size);
    setShells((prev) => {
      const newShells: ShellSlot[] = [];
      for (let i = 0; i < count; i++) {
        newShells.push(prev[i] ?? { serial: '', filled: false });
      }
      return newShells;
    });
  }, []);

  const addShell = useCallback(
    async (serial: string) => {
      if (alphaCode) {
        resetAssembly();
      }

      try {
        const ctx = await serialNumberApi.getContext(serial);

        if (ctx.existingAssembly) {
          setReassemblyPrompt({ alphaCode: ctx.existingAssembly.alphaCode, serial });
          return;
        }

        if (tankSize === 0) {
          updateTankSize(ctx.tankSize);
          setShells([{ serial, filled: true }]);
          showScanResult({ type: 'success', message: `Shell ${serial} added` });
        } else {
          if (ctx.tankSize !== tankSize) {
            showScanResult({ type: 'error', message: 'Shell size does not match the assembly' });
            return;
          }
          const idx = shells.findIndex((s) => !s.filled);
          if (idx === -1) {
            showScanResult({ type: 'error', message: 'All shell slots are filled' });
            return;
          }
          if (shells.some((s) => s.serial === serial)) {
            showScanResult({ type: 'error', message: 'This shell has already been added to this assembly' });
            return;
          }
          setShells((prev) => prev.map((s, i) => (i === idx ? { serial, filled: true } : s)));
          showScanResult({ type: 'success', message: `Shell ${serial} added` });
        }
      } catch {
        showScanResult({ type: 'error', message: 'Failed to look up shell' });
      }
    },
    [tankSize, shells, alphaCode, resetAssembly, updateTankSize, showScanResult],
  );

  const applyHeadLot = useCallback(
    async (cardId: string) => {
      try {
        const data = await materialQueueApi.getCardLookup(cardId);
        const lot: HeadLotInfo = {
          heatNumber: data.heatNumber,
          coilNumber: data.coilNumber,
          productDescription: data.productDescription,
          cardId,
          cardColor: data.cardColor,
        };
        if (headScanCount === 0) {
          setLeftHead(lot);
          setRightHead(lot);
          setHeadScanCount(1);
          showScanResult({ type: 'success', message: 'Head lot applied to both heads' });
        } else {
          setRightHead(lot);
          setHeadScanCount(2);
          showScanResult({ type: 'success', message: 'Right head lot updated' });
        }
      } catch {
        showScanResult({ type: 'error', message: 'Kanban card not found or not associated with any queued material' });
      }
    },
    [headScanCount, showScanResult],
  );

  const swapHeads = useCallback(() => {
    setLeftHead((prev) => {
      const old = prev;
      setRightHead((rh) => {
        setLeftHead(rh);
        return old;
      });
      return prev;
    });
    const tmp = leftHead;
    setLeftHead(rightHead);
    setRightHead(tmp);
    showScanResult({ type: 'success', message: 'Heads swapped' });
  }, [leftHead, rightHead, showScanResult]);

  const saveAssembly = useCallback(async () => {
    const allShellsFilled = shells.length > 0 && shells.every((s) => s.filled);
    if (!allShellsFilled) {
      showScanResult({ type: 'error', message: 'Scan all required shells before saving' });
      return;
    }
    if (!leftHead) {
      showScanResult({ type: 'error', message: 'Scan a kanban card for head material before saving' });
      return;
    }

    try {
      const resp = await assemblyApi.create({
        shells: shells.map((s) => s.serial),
        leftHeadLotId: leftHead.cardId ?? '',
        rightHeadLotId: rightHead?.cardId ?? leftHead.cardId ?? '',
        tankSize,
        workCenterId,
        assetId,
        productionLineId,
        operatorId,
        welderIds: welders.map((w) => w.userId),
      });

      setAlphaCode(resp.alphaCode);
      showScanResult({ type: 'success', message: `Assembly ${resp.alphaCode} saved` });
      refreshHistory();

      const timer = setTimeout(resetAssembly, 30000);
      setAlphaTimer(timer);
    } catch {
      showScanResult({ type: 'error', message: 'Failed to save assembly. Please try again.' });
    }
  }, [shells, leftHead, rightHead, tankSize, workCenterId, assetId, productionLineId, operatorId, welders, showScanResult, refreshHistory, resetAssembly]);

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) {
        showScanResult({ type: 'error', message: 'Unknown barcode' });
        return;
      }

      if (reassemblyPrompt) {
        if (bc.prefix === 'INP' && bc.value === '3') {
          // Yes - reassemble (simplified: just reset and use the shell)
          setReassemblyPrompt(null);
          showScanResult({ type: 'success', message: 'Reassembly mode' });
          return;
        }
        if (bc.prefix === 'INP' && bc.value === '4') {
          setReassemblyPrompt(null);
          resetAssembly();
          return;
        }
        return;
      }

      if (bc.prefix === 'SC') {
        const { serialNumber: serial } = parseShellLabel(bc.value);
        addShell(serial);
        return;
      }

      if (bc.prefix === 'KC') {
        applyHeadLot(bc.value);
        return;
      }

      if (bc.prefix === 'INP') {
        if (bc.value === '1') { swapHeads(); return; }
        if (bc.value === '2') { resetAssembly(); showScanResult({ type: 'success', message: 'Reset' }); return; }
        if (bc.value === '3') { saveAssembly(); return; }
      }

      if (bc.prefix === 'TS') {
        const size = parseInt(bc.value, 10);
        if (!isNaN(size) && size > 0) {
          updateTankSize(size);
          showScanResult({ type: 'success', message: `Tank size changed to ${size}` });
        }
        return;
      }

      showScanResult({ type: 'error', message: 'Invalid barcode in this context' });
    },
    [reassemblyPrompt, addShell, applyHeadLot, swapHeads, resetAssembly, saveAssembly, updateTankSize, showScanResult],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  if (alphaCode) {
    return (
      <div className={styles.alphaDisplay}>
        <span className={styles.alphaLabel}>Alpha Code</span>
        <span className={styles.alphaValue}>{alphaCode}</span>
        <span className={styles.alphaHint}>Write this on the assembly</span>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.topRow}>
        {!props.externalInput && (
          <>
            <Button appearance="secondary" size="medium" onClick={resetAssembly}>Reset</Button>
            <Button
              appearance="primary" size="medium" onClick={saveAssembly}
              disabled={!shells.every((s) => s.filled) || shells.length === 0 || !leftHead}
            >
              Save
            </Button>
          </>
        )}
        <div className={styles.tankSizeDisplay}>
          <Label>Tank Size</Label>
          {!props.externalInput ? (
            <Dropdown
              value={tankSize ? String(tankSize) : ''}
              selectedOptions={tankSize ? [String(tankSize)] : []}
              onOptionSelect={(_: unknown, d: OptionOnSelectData) => {
                if (d.optionValue) updateTankSize(parseInt(d.optionValue, 10));
              }}
              className={styles.tankDropdown}
            >
              {[120, 250, 320, 500, 1000, 1500].map((s) => (
                <Option key={s} value={String(s)}>{String(s)}</Option>
              ))}
            </Dropdown>
          ) : (
            <span className={styles.tankValue}>{tankSize || '—'}</span>
          )}
        </div>
      </div>

      {reassemblyPrompt && (
        <div className={styles.reassemblyPrompt}>
          <p>This shell is part of assembly <strong>{reassemblyPrompt.alphaCode}</strong>. Are you reassembling?</p>
          <div className={styles.promptButtons}>
            <Button appearance="primary" onClick={() => { setReassemblyPrompt(null); }}>Yes</Button>
            <Button appearance="secondary" onClick={() => { setReassemblyPrompt(null); resetAssembly(); }}>No</Button>
          </div>
        </div>
      )}

      <div className={styles.assemblyDiagram}>
        <div className={styles.headSlot}>
          <span className={styles.slotLabel}>Left Head</span>
          <div className={`${styles.slotBox} ${leftHead ? styles.filled : ''}`}>
            {leftHead ? (
              <span className={styles.slotInfo}>
                {leftHead.productDescription}<br />
                H: {leftHead.heatNumber} / C: {leftHead.coilNumber}
              </span>
            ) : (
              <span className={styles.slotPlaceholder}>Scan KC</span>
            )}
          </div>
        </div>

        {shells.map((shell, idx) => (
          <div key={idx} className={styles.shellSlot}>
            <span className={styles.slotLabel}>Shell {idx + 1}</span>
            <div className={`${styles.slotBox} ${shell.filled ? styles.filled : ''}`}>
              {shell.filled ? (
                <span className={styles.slotInfo}>{shell.serial}</span>
              ) : (
                <span className={styles.slotPlaceholder}>Scan Shell</span>
              )}
            </div>
          </div>
        ))}

        {tankSize === 0 && (
          <div className={styles.shellSlot}>
            <span className={styles.slotLabel}>Shell 1</span>
            <div className={styles.slotBox}>
              <span className={styles.slotPlaceholder}>Scan Shell</span>
            </div>
          </div>
        )}

        <div className={styles.headSlot}>
          <span className={styles.slotLabel}>Right Head</span>
          <div className={`${styles.slotBox} ${rightHead ? styles.filled : ''}`}>
            {rightHead ? (
              <span className={styles.slotInfo}>
                {rightHead.productDescription}<br />
                H: {rightHead.heatNumber} / C: {rightHead.coilNumber}
              </span>
            ) : (
              <span className={styles.slotPlaceholder}>Scan KC</span>
            )}
          </div>
        </div>
      </div>

      {!props.externalInput && (
        <div className={styles.manualEntry}>
          <div className={styles.manualRow}>
            <Input
              value={manualSerial}
              onChange={(_, d) => setManualSerial(d.value)}
              placeholder="Shell serial number..."
              size="medium"
              className={styles.serialInput}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && manualSerial.trim()) {
                  addShell(manualSerial.trim());
                  setManualSerial('');
                }
              }}
            />
            <Button appearance="primary" size="medium" onClick={() => {
              if (manualSerial.trim()) { addShell(manualSerial.trim()); setManualSerial(''); }
            }}>
              Add Shell
            </Button>
            <Button appearance="secondary" size="medium" onClick={swapHeads} disabled={!leftHead}>
              Swap Heads
            </Button>
          </div>
        </div>
      )}

      <div className={styles.queueSection}>
        <div className={styles.queueHeader}>
          <span className={styles.queueTitle}>Heads Queue</span>
          <Button appearance="subtle" size="small" onClick={loadHeadsQueue}>Refresh</Button>
        </div>
        {headsQueue.length === 0 ? (
          <div className={styles.emptyQueue}>No head material in queue. Contact Material Handling.</div>
        ) : (
          headsQueue.map((item) => (
            <button
              key={item.id}
              className={styles.queueCard}
              onClick={() => { if (!props.externalInput && item.cardId) applyHeadLot(item.cardId); }}
              disabled={props.externalInput}
            >
              <div className={styles.queueCardInfo}>
                <span>{item.productDescription}</span>
                <span className={styles.queueCardDetail}>Heat {item.heatNumber} / Coil {item.coilNumber}</span>
              </div>
              {item.cardColor && (
                <span className={styles.cardColorDot} style={{ backgroundColor: item.cardColor.toLowerCase() }} />
              )}
            </button>
          ))
        )}
      </div>
    </div>
  );
}
