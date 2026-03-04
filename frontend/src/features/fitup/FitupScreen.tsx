import { useState, useEffect, useCallback, useRef } from 'react';
import { Button, Input, Label, Dropdown, Option, type OptionOnSelectData } from '@fluentui/react-components';
import type { WorkCenterProps } from '../../components/layout/OperatorLayout.tsx';
import type { ParsedBarcode } from '../../types/barcode.ts';
import { parseShellLabel } from '../../types/barcode.ts';
import type { MaterialQueueItem, HeadLotInfo } from '../../types/domain.ts';
import { serialNumberApi, materialQueueApi, workCenterApi, assemblyApi, adminPlantGearApi } from '../../api/endpoints.ts';
import { NextStepBanner } from '../../components/nextStep/NextStepBanner.tsx';
import styles from './FitupScreen.module.css';

function shellCountForSize(tankSize: number): number {
  if (tankSize >= 1500) return 3;
  if (tankSize >= 1000) return 2;
  return 1;
}

function isComponentMismatch(assemblyTankSize: number, componentTankSize: number | undefined | null): boolean {
  if (assemblyTankSize === 0 || componentTankSize == null) return false;
  return componentTankSize !== assemblyTankSize;
}

interface ShellSlot {
  serial: string;
  filled: boolean;
  tankSize?: number;
}

type ReassemblyOperation = 'replace' | 'split';

interface ReassemblyAssemblyDraft {
  tankSize: number;
  shells: ShellSlot[];
  leftHead: HeadLotInfo | null;
  rightHead: HeadLotInfo | null;
}

interface ReassemblyFieldSelection {
  target: 'primary' | 'secondary';
  component: 'leftHead' | 'rightHead' | 'shell';
  shellIndex?: number;
}

interface ReassemblyModeState {
  sourceAlphaCode: string;
  source: ReassemblyAssemblyDraft;
  primary: ReassemblyAssemblyDraft;
  secondary: ReassemblyAssemblyDraft | null;
  operationType: ReassemblyOperation;
  selectedField: ReassemblyFieldSelection | null;
  entryValue: string;
  previousExternalInput: boolean;
}

interface ReassemblyPromptState {
  alphaCode: string;
  context: {
    alphaCode: string;
    tankSize: number;
    shells: string[];
    leftHeadInfo?: { heatNumber: string; coilNumber: string; productDescription: string };
    rightHeadInfo?: { heatNumber: string; coilNumber: string; productDescription: string };
  };
}

function headFromExisting(
  head: { heatNumber: string; coilNumber: string; productDescription: string } | undefined,
  tankSize: number,
): HeadLotInfo | null {
  if (!head) return null;
  return {
    heatNumber: head.heatNumber,
    coilNumber: head.coilNumber,
    productDescription: head.productDescription,
    tankSize,
  };
}

function cloneDraft(draft: ReassemblyAssemblyDraft): ReassemblyAssemblyDraft {
  return {
    tankSize: draft.tankSize,
    shells: draft.shells.map((s) => ({ ...s })),
    leftHead: draft.leftHead ? { ...draft.leftHead } : null,
    rightHead: draft.rightHead ? { ...draft.rightHead } : null,
  };
}

function normalizeKanbanCardId(input: string): string {
  const trimmed = input.trim();
  if (!trimmed) return '';
  if (/^KC;/i.test(trimmed)) {
    return trimmed.slice(3).trim();
  }
  return trimmed;
}

function formatHeadTraceability(head: { lotNumber?: string; heatNumber?: string; coilNumber?: string }): string {
  if (head.lotNumber?.trim()) return `Lot ${head.lotNumber.trim()}`;
  return `Heat ${head.heatNumber || '—'} / Coil ${head.coilNumber || '—'}`;
}

function formatQueueHeadLabel(item: MaterialQueueItem): string {
  const tankSize = item.shellSize ? `${item.shellSize} gal` : '—';
  return `${tankSize} · ${formatHeadTraceability(item)} · Card ${item.cardId ?? '—'}`;
}

function formatReassemblyHeadSlot(head: HeadLotInfo | null): string {
  if (!head) return '—';
  if (head.lotNumber?.trim()) return `Lot ${head.lotNumber.trim()}`;
  return `Heat ${head.heatNumber || '—'} / Coil ${head.coilNumber || '—'}`;
}

function headChangeSignature(head: HeadLotInfo | null): string {
  if (!head) return '';
  return `${head.lotNumber?.trim() ?? ''}|${head.heatNumber?.trim() ?? ''}|${head.coilNumber?.trim() ?? ''}`;
}

function isHeadChanged(current: HeadLotInfo | null, proposed: HeadLotInfo | null): boolean {
  return headChangeSignature(current) !== headChangeSignature(proposed);
}

function isShellChanged(current: ShellSlot | undefined, proposed: ShellSlot | undefined): boolean {
  return (current?.serial ?? '') !== (proposed?.serial ?? '');
}

export function FitupScreen(props: WorkCenterProps) {
  const {
    workCenterId, assetId, productionLineId, operatorId, welders,
    showScanResult, refreshHistory, registerBarcodeHandler,
  } = props;
  const queueWCId = props.materialQueueForWCId ?? workCenterId;

  const [tankSize, setTankSize] = useState<number>(0);
  const [shells, setShells] = useState<ShellSlot[]>([]);
  const [leftHead, setLeftHead] = useState<HeadLotInfo | null>(null);
  const [rightHead, setRightHead] = useState<HeadLotInfo | null>(null);
  const [headScanCount, setHeadScanCount] = useState(0);
  const [alphaCode, setAlphaCode] = useState<string | null>(null);
  const [alphaTimer, setAlphaTimer] = useState<ReturnType<typeof setTimeout> | null>(null);
  const [headsQueue, setHeadsQueue] = useState<MaterialQueueItem[]>([]);
  const [manualSerial, setManualSerial] = useState('');
  const [nextAssemblyCode, setNextAssemblyCode] = useState<string | null>(null);

  const [reassemblyPrompt, setReassemblyPrompt] = useState<ReassemblyPromptState | null>(null);
  const [reassemblyMode, setReassemblyMode] = useState<ReassemblyModeState | null>(null);

  const refreshNextAssemblyCode = useCallback(async () => {
    try {
      const plants = await adminPlantGearApi.getAll();
      const currentPlant = plants.find((p) => p.plantId === props.plantId);
      setNextAssemblyCode(currentPlant?.nextTankAlphaCode ?? null);
    } catch {
      setNextAssemblyCode(null);
    }
  }, [props.plantId]);

  useEffect(() => {
    loadHeadsQueue();
  }, [workCenterId]);

  useEffect(() => {
    refreshNextAssemblyCode();
  }, [refreshNextAssemblyCode]);

  const loadHeadsQueue = useCallback(async () => {
    try {
      const items = await workCenterApi.getMaterialQueue(queueWCId, 'fitup', productionLineId);
      setHeadsQueue(items.filter((i) => i.status === 'queued'));
    } catch { /* keep stale */ }
  }, [queueWCId, productionLineId]);

  const resetAssembly = useCallback((options?: { preserveHeads?: boolean }) => {
    const preserveHeads = options?.preserveHeads ?? false;
    setTankSize(0);
    setShells([]);
    if (!preserveHeads) {
      setLeftHead(null);
      setRightHead(null);
      setHeadScanCount(0);
    }
    setReassemblyPrompt(null);
    setReassemblyMode(null);
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
        resetAssembly({ preserveHeads: true });
      }

      try {
        const ctx = await serialNumberApi.getContext(serial);
        if (ctx.isBlockedForProcessing) {
          const reason = ctx.blockingReasons?.join(' | ') ?? 'Open Hold Tag/NCR exists for this serial.';
          showScanResult({ type: 'error', message: `Serial ${serial} is blocked. ${reason}` });
          return;
        }

        if (ctx.existingAssembly) {
          setReassemblyPrompt({ alphaCode: ctx.existingAssembly.alphaCode, context: ctx.existingAssembly });
          return;
        }

        if (tankSize === 0) {
          const count = shellCountForSize(ctx.tankSize);
          setTankSize(ctx.tankSize);
          const newShells: ShellSlot[] = [];
          for (let i = 0; i < count; i++) {
            newShells.push(i === 0
              ? { serial, filled: true, tankSize: ctx.tankSize }
              : { serial: '', filled: false });
          }
          setShells(newShells);
          showScanResult({ type: 'success', message: `Shell ${serial} added` });
        } else {
          const idx = shells.findIndex((s) => !s.filled);
          if (idx === -1) {
            showScanResult({ type: 'error', message: 'All shell slots are filled' });
            return;
          }
          if (shells.some((s) => s.serial === serial)) {
            showScanResult({ type: 'error', message: 'This shell has already been added to this assembly' });
            return;
          }
          setShells((prev) => prev.map((s, i) => (i === idx ? { serial, filled: true, tankSize: ctx.tankSize } : s)));
          if (ctx.tankSize !== tankSize) {
            showScanResult({ type: 'error', message: `Shell is ${ctx.tankSize} gal but assembly is ${tankSize} gal` });
          } else {
            showScanResult({ type: 'success', message: `Shell ${serial} added` });
          }
        }
      } catch {
        showScanResult({ type: 'error', message: `Failed to look up shell ${serial}` });
      }
    },
    [tankSize, shells, alphaCode, resetAssembly, updateTankSize, showScanResult],
  );

  const applyHeadLot = useCallback(
    async (cardId: string) => {
      try {
        const normalizedCardId = normalizeKanbanCardId(cardId);
        const data = await materialQueueApi.getCardLookup(queueWCId, productionLineId, normalizedCardId);
        const lot: HeadLotInfo = {
          heatNumber: data.heatNumber,
          coilNumber: data.coilNumber,
          lotNumber: data.lotNumber,
          productDescription: data.productDescription,
          cardId: normalizedCardId,
          cardColor: data.cardColor,
          tankSize: data.tankSize,
        };
        if (tankSize === 0 && typeof lot.tankSize === 'number' && lot.tankSize > 0) {
          updateTankSize(lot.tankSize);
        }
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
    [headScanCount, productionLineId, queueWCId, showScanResult, tankSize, updateTankSize],
  );

  const swapHeads = useCallback(() => {
    const tmpLeft = leftHead;
    const tmpRight = rightHead;
    setLeftHead(tmpRight);
    setRightHead(tmpLeft);
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
    const hasHeadMismatch = isComponentMismatch(tankSize, leftHead?.tankSize) || isComponentMismatch(tankSize, rightHead?.tankSize);
    const hasShellMismatch = shells.some((s) => s.filled && isComponentMismatch(tankSize, s.tankSize));
    if (hasHeadMismatch || hasShellMismatch) {
      showScanResult({ type: 'error', message: 'Component tank sizes do not match — fix mismatches before saving' });
      return;
    }

    try {
      const resp = await assemblyApi.create({
        shells: shells.map((s) => s.serial),
        leftHeadLotId: leftHead.cardId ?? '',
        rightHeadLotId: rightHead?.cardId ?? leftHead.cardId ?? '',
        leftHeadHeatNumber: leftHead.heatNumber || undefined,
        leftHeadCoilNumber: leftHead.coilNumber || undefined,
        leftHeadLotNumber: leftHead.lotNumber || undefined,
        rightHeadHeatNumber: rightHead?.heatNumber || undefined,
        rightHeadCoilNumber: rightHead?.coilNumber || undefined,
        rightHeadLotNumber: rightHead?.lotNumber || undefined,
        tankSize,
        workCenterId,
        assetId: assetId || undefined,
        productionLineId,
        operatorId,
        welderIds: welders.map((w) => w.userId),
      });

      setAlphaCode(resp.alphaCode);
      showScanResult({ type: 'success', message: `Assembly ${resp.alphaCode} saved` });
      refreshHistory();
      refreshNextAssemblyCode();

      const timer = setTimeout(() => resetAssembly({ preserveHeads: true }), 30000);
      setAlphaTimer(timer);
    } catch (err: any) {
      const msg = err?.body ?? err?.message ?? 'Failed to save assembly. Please try again.';
      showScanResult({ type: 'error', message: typeof msg === 'string' ? msg : 'Failed to save assembly. Please try again.' });
    }
  }, [shells, leftHead, rightHead, tankSize, workCenterId, assetId, productionLineId, operatorId, welders, showScanResult, refreshHistory, refreshNextAssemblyCode, resetAssembly]);

  const enterReassemblyMode = useCallback((prompt: ReassemblyPromptState) => {
    const source: ReassemblyAssemblyDraft = {
      tankSize: prompt.context.tankSize,
      shells: prompt.context.shells.map((s) => ({ serial: s, filled: true, tankSize: prompt.context.tankSize })),
      leftHead: headFromExisting(prompt.context.leftHeadInfo, prompt.context.tankSize),
      rightHead: headFromExisting(prompt.context.rightHeadInfo, prompt.context.tankSize),
    };

    if (props.externalInput) {
      props.setExternalInput(false);
    }

    setReassemblyMode({
      sourceAlphaCode: prompt.alphaCode,
      source,
      primary: cloneDraft(source),
      secondary: null,
      operationType: 'replace',
      selectedField: null,
      entryValue: '',
      previousExternalInput: props.externalInput,
    });
    setReassemblyPrompt(null);
    showScanResult({ type: 'success', message: 'Reassembly mode enabled' });
  }, [props, showScanResult]);

  const exitReassemblyMode = useCallback(() => {
    setReassemblyMode((current) => {
      if (current?.previousExternalInput) {
        props.setExternalInput(true);
      }
      return null;
    });
    setReassemblyPrompt(null);
    refreshNextAssemblyCode();
  }, [props, refreshNextAssemblyCode]);

  const setSplitDraft = useCallback((mode: ReassemblyModeState, splitKind: 'two-left' | 'two-right' | 'three-12' | 'three-23') => {
    const source = cloneDraft(mode.source);
    let primaryShells: ShellSlot[] = [];
    let secondaryShells: ShellSlot[] = [];
    if (splitKind === 'two-left') {
      primaryShells = [source.shells[0]].filter(Boolean);
      secondaryShells = [source.shells[1]].filter(Boolean);
    } else if (splitKind === 'two-right') {
      primaryShells = [source.shells[1]].filter(Boolean);
      secondaryShells = [source.shells[0]].filter(Boolean);
    } else if (splitKind === 'three-12') {
      primaryShells = [source.shells[0]].filter(Boolean);
      secondaryShells = source.shells.slice(1);
    } else {
      primaryShells = source.shells.slice(0, 2);
      secondaryShells = [source.shells[2]].filter(Boolean);
    }

    const primaryTankSize = primaryShells.length >= 3 ? 1500 : primaryShells.length === 2 ? 1000 : 500;
    const secondaryTankSize = secondaryShells.length >= 3 ? 1500 : secondaryShells.length === 2 ? 1000 : 500;

    setReassemblyMode({
      ...mode,
      operationType: 'split',
      selectedField: null,
      primary: {
        tankSize: primaryTankSize,
        shells: primaryShells.map((s) => ({ ...s, tankSize: primaryTankSize })),
        leftHead: mode.source.leftHead ? { ...mode.source.leftHead, tankSize: primaryTankSize } : null,
        rightHead: mode.source.rightHead ? { ...mode.source.rightHead, tankSize: primaryTankSize } : null,
      },
      secondary: {
        tankSize: secondaryTankSize,
        shells: secondaryShells.map((s) => ({ ...s, tankSize: secondaryTankSize })),
        leftHead: mode.source.leftHead ? { ...mode.source.leftHead, tankSize: secondaryTankSize } : null,
        rightHead: mode.source.rightHead ? { ...mode.source.rightHead, tankSize: secondaryTankSize } : null,
      },
    });
  }, []);

  const selectReassemblyField = useCallback((selection: ReassemblyFieldSelection) => {
    setReassemblyMode((current) => current ? { ...current, selectedField: selection, entryValue: '' } : current);
  }, []);

  const applyHeadToSelection = useCallback(async (cardId: string) => {
    if (!reassemblyMode?.selectedField) return;
    const { selectedField } = reassemblyMode;
    if (selectedField.component !== 'leftHead' && selectedField.component !== 'rightHead') {
      showScanResult({ type: 'error', message: 'Select a head slot first' });
      return;
    }

    try {
      const normalizedCardId = normalizeKanbanCardId(cardId);
      const data = await materialQueueApi.getCardLookup(queueWCId, productionLineId, normalizedCardId);
      const lot: HeadLotInfo = {
        heatNumber: data.heatNumber,
        coilNumber: data.coilNumber,
        lotNumber: data.lotNumber,
        productDescription: data.productDescription,
        cardId: normalizedCardId,
        cardColor: data.cardColor,
        tankSize: selectedField.target === 'primary'
          ? reassemblyMode.primary.tankSize
          : reassemblyMode.secondary?.tankSize,
      };

      setReassemblyMode((current) => {
        if (!current || !current.selectedField) return current;
        const target = current.selectedField.target;
        const component = current.selectedField.component;
        if (target === 'primary') {
          return {
            ...current,
            primary: {
              ...current.primary,
              leftHead: component === 'leftHead' ? lot : current.primary.leftHead,
              rightHead: component === 'rightHead' ? lot : current.primary.rightHead,
            },
            entryValue: '',
          };
        }
        if (!current.secondary) return current;
        return {
          ...current,
          secondary: {
            ...current.secondary,
            leftHead: component === 'leftHead' ? lot : current.secondary.leftHead,
            rightHead: component === 'rightHead' ? lot : current.secondary.rightHead,
          },
          entryValue: '',
        };
      });
      showScanResult({ type: 'success', message: 'Head replacement applied' });
    } catch {
      showScanResult({ type: 'error', message: 'Kanban card not found or not associated with any queued material' });
    }
  }, [productionLineId, queueWCId, reassemblyMode, showScanResult]);

  const applyShellToSelection = useCallback(async (shellSerial: string) => {
    if (!reassemblyMode?.selectedField || reassemblyMode.selectedField.component !== 'shell') {
      showScanResult({ type: 'error', message: 'Select a shell slot first' });
      return;
    }
    const { target, shellIndex } = reassemblyMode.selectedField;
    if (shellIndex == null) return;

    try {
      const ctx = await serialNumberApi.getContext(shellSerial);
      if (ctx.isBlockedForProcessing) {
        const reason = ctx.blockingReasons?.join(' | ') ?? 'Open Hold Tag/NCR exists for this serial.';
        showScanResult({ type: 'error', message: `Serial ${shellSerial} is blocked. ${reason}` });
        return;
      }
      if (ctx.existingAssembly && ctx.existingAssembly.alphaCode !== reassemblyMode.sourceAlphaCode) {
        showScanResult({ type: 'error', message: `Shell ${shellSerial} already belongs to ${ctx.existingAssembly.alphaCode}` });
        return;
      }

      setReassemblyMode((current) => {
        if (!current || !current.selectedField) return current;
        const updateDraft = (draft: ReassemblyAssemblyDraft): ReassemblyAssemblyDraft => {
          const updatedShells = draft.shells.map((s, idx) => (
            idx === shellIndex ? { serial: shellSerial, filled: true, tankSize: ctx.tankSize || draft.tankSize } : s
          ));
          return { ...draft, shells: updatedShells };
        };
        if (target === 'primary') {
          return { ...current, primary: updateDraft(current.primary), entryValue: '' };
        }
        if (!current.secondary) return current;
        return { ...current, secondary: updateDraft(current.secondary), entryValue: '' };
      });
      showScanResult({ type: 'success', message: `Shell ${shellSerial} replacement applied` });
    } catch {
      showScanResult({ type: 'error', message: `Failed to look up shell ${shellSerial}` });
    }
  }, [reassemblyMode, showScanResult]);

  const hasReassemblyChanges = useCallback((mode: ReassemblyModeState) => {
    const compareDraft = (a: ReassemblyAssemblyDraft, b: ReassemblyAssemblyDraft) => {
      const shellsA = a.shells.map((s) => s.serial).join('|');
      const shellsB = b.shells.map((s) => s.serial).join('|');
      const leftA = `${a.leftHead?.heatNumber ?? ''}|${a.leftHead?.coilNumber ?? ''}|${a.leftHead?.lotNumber ?? ''}`;
      const leftB = `${b.leftHead?.heatNumber ?? ''}|${b.leftHead?.coilNumber ?? ''}|${b.leftHead?.lotNumber ?? ''}`;
      const rightA = `${a.rightHead?.heatNumber ?? ''}|${a.rightHead?.coilNumber ?? ''}|${a.rightHead?.lotNumber ?? ''}`;
      const rightB = `${b.rightHead?.heatNumber ?? ''}|${b.rightHead?.coilNumber ?? ''}|${b.rightHead?.lotNumber ?? ''}`;
      return shellsA !== shellsB || leftA !== leftB || rightA !== rightB || a.tankSize !== b.tankSize;
    };

    if (mode.operationType === 'replace') {
      return compareDraft(mode.primary, mode.source);
    }
    if (!mode.secondary) return false;
    return true;
  }, []);

  const validateReassemblyDraft = useCallback((draft: ReassemblyAssemblyDraft) => {
    if (draft.shells.length === 0 || draft.shells.some((s) => !s.serial)) return false;
    const hasHeadMismatch = isComponentMismatch(draft.tankSize, draft.leftHead?.tankSize)
      || isComponentMismatch(draft.tankSize, draft.rightHead?.tankSize);
    const hasShellMismatch = draft.shells.some((s) => s.filled && isComponentMismatch(draft.tankSize, s.tankSize));
    return !hasHeadMismatch && !hasShellMismatch;
  }, []);

  const saveReassembly = useCallback(async () => {
    if (!reassemblyMode) return;
    if (!hasReassemblyChanges(reassemblyMode)) {
      showScanResult({ type: 'error', message: 'No changes detected for reassembly' });
      return;
    }
    if (!validateReassemblyDraft(reassemblyMode.primary)) {
      showScanResult({ type: 'error', message: 'Primary proposed assembly is invalid' });
      return;
    }
    if (reassemblyMode.operationType === 'split' && (!reassemblyMode.secondary || !validateReassemblyDraft(reassemblyMode.secondary))) {
      showScanResult({ type: 'error', message: 'Split requires a valid secondary assembly' });
      return;
    }

    try {
      const mapHead = (head: HeadLotInfo | null) => (head ? {
        lotId: head.cardId,
        heatNumber: head.heatNumber || undefined,
        coilNumber: head.coilNumber || undefined,
        lotNumber: head.lotNumber || undefined,
      } : undefined);

      const response = await assemblyApi.reassemble(reassemblyMode.sourceAlphaCode, {
        operationType: reassemblyMode.operationType,
        primaryAssembly: {
          shells: reassemblyMode.primary.shells.map((s) => s.serial),
          tankSize: reassemblyMode.primary.tankSize,
          leftHead: mapHead(reassemblyMode.primary.leftHead),
          rightHead: mapHead(reassemblyMode.primary.rightHead),
        },
        secondaryAssembly: reassemblyMode.secondary ? {
          shells: reassemblyMode.secondary.shells.map((s) => s.serial),
          tankSize: reassemblyMode.secondary.tankSize,
          leftHead: mapHead(reassemblyMode.secondary.leftHead),
          rightHead: mapHead(reassemblyMode.secondary.rightHead),
        } : undefined,
        workCenterId,
        assetId: assetId || undefined,
        productionLineId,
        operatorId,
        welderIds: welders.map((w) => w.userId),
      });

      const first = response.createdAssemblies[0];
      setAlphaCode(first?.alphaCode ?? null);
      if (reassemblyMode.previousExternalInput) {
        props.setExternalInput(true);
      }
      setReassemblyMode(null);
      showScanResult({
        type: 'success',
        message: `Reassembly saved: ${response.createdAssemblies.map((a) => a.alphaCode).join(', ')}`,
      });
      refreshHistory();
      refreshNextAssemblyCode();
      const timer = setTimeout(() => resetAssembly({ preserveHeads: true }), 30000);
      setAlphaTimer(timer);
    } catch (err: any) {
      const msg = err?.body ?? err?.message ?? 'Failed to save reassembly. Please try again.';
      showScanResult({ type: 'error', message: typeof msg === 'string' ? msg : 'Failed to save reassembly. Please try again.' });
    }
  }, [
    reassemblyMode, hasReassemblyChanges, validateReassemblyDraft, showScanResult,
    workCenterId, assetId, productionLineId, operatorId, welders,
    refreshHistory, refreshNextAssemblyCode, resetAssembly, props,
  ]);

  const canShowSplitActions = reassemblyMode
    ? reassemblyMode.source.tankSize > 500 && reassemblyMode.source.shells.length >= 2
    : false;

  const handleBarcode = useCallback(
    (bc: ParsedBarcode | null, _raw: string) => {
      if (!bc) {
        showScanResult({ type: 'error', message: 'Unknown barcode' });
        return;
      }

      if (alphaCode) {
        resetAssembly({ preserveHeads: true });
        if (bc.prefix === 'SC') {
          const { serialNumber: serial } = parseShellLabel(bc.value);
          addShell(serial);
          return;
        }
        if (bc.prefix === 'KC') {
          applyHeadLot(bc.value);
        }
        return;
      }

      if (reassemblyPrompt) {
        if (bc.prefix === 'INP' && bc.value === '3') {
          enterReassemblyMode(reassemblyPrompt);
          return;
        }
        if (bc.prefix === 'INP' && bc.value === '4') {
          setReassemblyPrompt(null);
          resetAssembly();
          return;
        }
        return;
      }

      if (reassemblyMode) {
        if (bc.prefix === 'INP' && bc.value === '2') {
          exitReassemblyMode();
          showScanResult({ type: 'success', message: 'Reassembly cancelled' });
          return;
        }
        if (bc.prefix === 'INP' && bc.value === '3') {
          saveReassembly();
          return;
        }
        if (bc.prefix === 'KC') {
          applyHeadToSelection(bc.value);
          return;
        }
        if (bc.prefix === 'SC') {
          const { serialNumber: serial } = parseShellLabel(bc.value);
          applyShellToSelection(serial);
          return;
        }
        showScanResult({ type: 'error', message: 'Only KC/SC and Save/Cancel are valid in reassembly mode' });
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
    [
      alphaCode, reassemblyPrompt, reassemblyMode, addShell, applyHeadLot, swapHeads, resetAssembly,
      saveAssembly, updateTankSize, showScanResult, enterReassemblyMode, saveReassembly,
      exitReassemblyMode, applyHeadToSelection, applyShellToSelection,
    ],
  );

  const handleBarcodeRef = useRef(handleBarcode);
  handleBarcodeRef.current = handleBarcode;

  useEffect(() => {
    registerBarcodeHandler((bc, raw) => handleBarcodeRef.current(bc, raw));
  }, [registerBarcodeHandler]);

  const nextInstruction = (() => {
    const isActive = props.externalInput;

    if (reassemblyMode) {
      return {
        title: props.externalInput
          ? 'Reassembly mode active'
          : 'Select a component in Proposed and enter replacement',
        isActive: props.externalInput,
      };
    }

    if (reassemblyPrompt) {
      return {
        title: props.externalInput
          ? 'Scan Yes or No for reassembly'
          : 'Confirm reassembly (Yes or No)',
        isActive,
      };
    }

    if (tankSize === 0 || shells.length === 0) {
      return {
        title: props.externalInput
          ? 'Scan first shell'
          : 'Enter first shell serial and tap Add Shell',
        isActive,
      };
    }

    const missingShells = shells.filter((s) => !s.filled).length;
    if (missingShells > 0) {
      return {
        title: props.externalInput
          ? `Scan ${missingShells} more shell${missingShells > 1 ? 's' : ''}`
          : `Add ${missingShells} more shell${missingShells > 1 ? 's' : ''}`,
        isActive,
      };
    }

    if (!leftHead || !rightHead) {
      return {
        title: props.externalInput
          ? 'Scan head kanban card'
          : 'Scan or select head kanban card',
        isActive,
      };
    }

    const hasHeadMismatch = isComponentMismatch(tankSize, leftHead.tankSize)
      || isComponentMismatch(tankSize, rightHead.tankSize);
    const hasShellMismatch = shells.some((s) => s.filled && isComponentMismatch(tankSize, s.tankSize));
    if (hasHeadMismatch || hasShellMismatch) {
      return {
        title: 'Fix component size mismatch',
        isActive,
      };
    }

    return {
      title: props.externalInput
        ? 'Scan Save'
        : 'Tap Save',
      isActive,
    };
  })();

  const hasAssemblyStarted = shells.some((s) => s.filled) || !!leftHead || !!rightHead;
  const selectedReassemblyHeadItem = reassemblyMode?.entryValue
    ? headsQueue.find((q) => q.cardId === reassemblyMode.entryValue)
    : undefined;
  const selectedReassemblyHeadLabel = selectedReassemblyHeadItem
    ? formatQueueHeadLabel(selectedReassemblyHeadItem)
    : (reassemblyMode?.entryValue ? `Card ${reassemblyMode.entryValue}` : '');

  if (alphaCode) {
    return (
      <div className={styles.alphaDisplay}>
        <span className={styles.alphaValue}>
          {alphaCode}
          {shells.length > 0 && <span className={styles.alphaShells}> ({shells.map((s) => s.serial).join(', ')})</span>}
        </span>
        {!props.externalInput && (
          <Button appearance="subtle" size="large" onClick={() => resetAssembly({ preserveHeads: true })} style={{ marginTop: '1rem' }}>
            Next Assembly
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <NextStepBanner instruction={nextInstruction} />

      {reassemblyMode && (
        <div className={styles.reassemblyBadge}>Reassembly Mode</div>
      )}

      <div className={styles.tankSizeDisplay}>
        <div className={styles.metricPair}>
          <Label>Tank Size:</Label>
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
        {hasAssemblyStarted && (
          <div className={styles.metricPair}>
            <Label>Building Assembly:</Label>
            <span className={styles.tankValue}>{nextAssemblyCode ?? '—'}</span>
          </div>
        )}
      </div>

      {!props.externalInput && !reassemblyMode && (
        <div className={styles.topRow}>
          <Button appearance="secondary" size="medium" onClick={() => resetAssembly()}>Reset</Button>
          <Button
            appearance="primary" size="medium" onClick={saveAssembly}
            disabled={!shells.every((s) => s.filled) || shells.length === 0 || !leftHead || isComponentMismatch(tankSize, leftHead?.tankSize) || isComponentMismatch(tankSize, rightHead?.tankSize) || shells.some((s) => s.filled && isComponentMismatch(tankSize, s.tankSize))}
          >
            Save
          </Button>
        </div>
      )}

      {reassemblyPrompt && !reassemblyMode && (
        <div className={styles.reassemblyPrompt}>
          <p>This shell is part of assembly <strong>{reassemblyPrompt.alphaCode}</strong>. Are you reassembling?</p>
          <div className={styles.promptButtons}>
            <Button appearance="primary" onClick={() => enterReassemblyMode(reassemblyPrompt)}>Yes</Button>
            <Button appearance="secondary" onClick={() => { setReassemblyPrompt(null); resetAssembly(); }}>No</Button>
          </div>
        </div>
      )}

      {reassemblyMode && (
        <div className={styles.reassemblyPanel}>
          <div className={styles.reassemblyHeader}>
            <div>
              <strong>Current Assembly ({reassemblyMode.sourceAlphaCode})</strong>
            </div>
            <div className={styles.promptButtons}>
              {canShowSplitActions && (
                <>
                  <Button
                    appearance="secondary"
                    onClick={() => {
                      if (reassemblyMode.source.shells.length === 2) setSplitDraft(reassemblyMode, 'two-left');
                    }}
                    disabled={reassemblyMode.source.shells.length !== 2}
                  >
                    Split Keep Left
                  </Button>
                  <Button
                    appearance="secondary"
                    onClick={() => {
                      if (reassemblyMode.source.shells.length === 2) setSplitDraft(reassemblyMode, 'two-right');
                    }}
                    disabled={reassemblyMode.source.shells.length !== 2}
                  >
                    Split Keep Right
                  </Button>
                  <Button
                    appearance="secondary"
                    onClick={() => {
                      if (reassemblyMode.source.shells.length === 3) setSplitDraft(reassemblyMode, 'three-12');
                    }}
                    disabled={reassemblyMode.source.shells.length !== 3}
                  >
                    Split S1|S2
                  </Button>
                  <Button
                    appearance="secondary"
                    onClick={() => {
                      if (reassemblyMode.source.shells.length === 3) setSplitDraft(reassemblyMode, 'three-23');
                    }}
                    disabled={reassemblyMode.source.shells.length !== 3}
                  >
                    Split S2|S3
                  </Button>
                </>
              )}
              {canShowSplitActions && (
                <Button
                  appearance="subtle"
                  onClick={() => setReassemblyMode({ ...reassemblyMode, operationType: 'replace', secondary: null })}
                >
                  Replace Mode
                </Button>
              )}
            </div>
          </div>
          <div className={styles.reassemblyGuidance}>
            Tip: Click a component in Proposed to select it for replacement.
          </div>

          <div className={styles.reassemblyRowTitle}>Current</div>
          <div className={styles.assemblyDiagram}>
            <div className={styles.headSlot}>
              <span className={styles.slotLabel}>Left Head</span>
              <div className={`${styles.slotBox} ${reassemblyMode.source.leftHead ? styles.filled : ''}`}>
                {reassemblyMode.source.leftHead?.cardColor && (
                  <span
                    className={styles.headColorSwatch}
                    style={{ backgroundColor: reassemblyMode.source.leftHead.cardColor.toLowerCase() }}
                    aria-hidden="true"
                  />
                )}
                {formatReassemblyHeadSlot(reassemblyMode.source.leftHead)}
              </div>
            </div>
            {reassemblyMode.source.shells.map((shell, idx) => (
              <div key={`source-shell-${idx}`} className={styles.shellSlot}>
                <span className={styles.slotLabel}>Shell {idx + 1}</span>
                <div className={`${styles.slotBox} ${styles.filled}`}>{shell.serial}</div>
              </div>
            ))}
            <div className={styles.headSlot}>
              <span className={styles.slotLabel}>Right Head</span>
              <div className={`${styles.slotBox} ${reassemblyMode.source.rightHead ? styles.filled : ''}`}>
                {reassemblyMode.source.rightHead?.cardColor && (
                  <span
                    className={styles.headColorSwatch}
                    style={{ backgroundColor: reassemblyMode.source.rightHead.cardColor.toLowerCase() }}
                    aria-hidden="true"
                  />
                )}
                {formatReassemblyHeadSlot(reassemblyMode.source.rightHead)}
              </div>
            </div>
          </div>
          <div className={styles.reassemblyRowTitle}>Proposed A</div>
          <div className={styles.assemblyDiagram}>
            <div className={styles.headSlot}>
              <span className={styles.slotLabel}>
                Left Head
                {isHeadChanged(reassemblyMode.source.leftHead, reassemblyMode.primary.leftHead) && (
                  <span className={styles.changedTag}>Changed</span>
                )}
              </span>
              <button
                className={`${styles.slotBox} ${reassemblyMode.primary.leftHead ? styles.filled : ''} ${reassemblyMode.selectedField?.target === 'primary' && reassemblyMode.selectedField.component === 'leftHead' ? styles.selectedSlot : ''}`}
                onClick={() => selectReassemblyField({ target: 'primary', component: 'leftHead' })}
              >
                {reassemblyMode.primary.leftHead?.cardColor && (
                  <span
                    className={styles.headColorSwatch}
                    style={{ backgroundColor: reassemblyMode.primary.leftHead.cardColor.toLowerCase() }}
                    aria-hidden="true"
                  />
                )}
                {reassemblyMode.primary.leftHead ? formatReassemblyHeadSlot(reassemblyMode.primary.leftHead) : 'Select + KC'}
              </button>
            </div>
            {reassemblyMode.primary.shells.map((shell, idx) => (
              <div key={`primary-shell-${idx}`} className={styles.shellSlot}>
                <span className={styles.slotLabel}>
                  Shell {idx + 1}
                  {isShellChanged(reassemblyMode.source.shells[idx], shell) && (
                    <span className={styles.changedTag}>Changed</span>
                  )}
                </span>
                <button
                  className={`${styles.slotBox} ${shell.serial ? styles.filled : ''} ${reassemblyMode.selectedField?.target === 'primary' && reassemblyMode.selectedField.component === 'shell' && reassemblyMode.selectedField.shellIndex === idx ? styles.selectedSlot : ''}`}
                  onClick={() => selectReassemblyField({ target: 'primary', component: 'shell', shellIndex: idx })}
                >
                  {shell.serial || 'Select + shell'}
                </button>
              </div>
            ))}
            <div className={styles.headSlot}>
              <span className={styles.slotLabel}>
                Right Head
                {isHeadChanged(reassemblyMode.source.rightHead, reassemblyMode.primary.rightHead) && (
                  <span className={styles.changedTag}>Changed</span>
                )}
              </span>
              <button
                className={`${styles.slotBox} ${reassemblyMode.primary.rightHead ? styles.filled : ''} ${reassemblyMode.selectedField?.target === 'primary' && reassemblyMode.selectedField.component === 'rightHead' ? styles.selectedSlot : ''}`}
                onClick={() => selectReassemblyField({ target: 'primary', component: 'rightHead' })}
              >
                {reassemblyMode.primary.rightHead?.cardColor && (
                  <span
                    className={styles.headColorSwatch}
                    style={{ backgroundColor: reassemblyMode.primary.rightHead.cardColor.toLowerCase() }}
                    aria-hidden="true"
                  />
                )}
                {reassemblyMode.primary.rightHead ? formatReassemblyHeadSlot(reassemblyMode.primary.rightHead) : 'Select + KC'}
              </button>
            </div>
          </div>

          {reassemblyMode.secondary && (
            <>
              <div className={styles.reassemblyRowTitle}>Proposed B</div>
              <div className={styles.assemblyDiagram}>
                <div className={styles.headSlot}>
                  <span className={styles.slotLabel}>
                    Left Head
                    {isHeadChanged(reassemblyMode.source.leftHead, reassemblyMode.secondary.leftHead) && (
                      <span className={styles.changedTag}>Changed</span>
                    )}
                  </span>
                  <button
                    className={`${styles.slotBox} ${reassemblyMode.secondary.leftHead ? styles.filled : ''} ${reassemblyMode.selectedField?.target === 'secondary' && reassemblyMode.selectedField.component === 'leftHead' ? styles.selectedSlot : ''}`}
                    onClick={() => selectReassemblyField({ target: 'secondary', component: 'leftHead' })}
                  >
                    {reassemblyMode.secondary.leftHead?.cardColor && (
                      <span
                        className={styles.headColorSwatch}
                        style={{ backgroundColor: reassemblyMode.secondary.leftHead.cardColor.toLowerCase() }}
                        aria-hidden="true"
                      />
                    )}
                    {reassemblyMode.secondary.leftHead ? formatReassemblyHeadSlot(reassemblyMode.secondary.leftHead) : 'Select + KC'}
                  </button>
                </div>
                {reassemblyMode.secondary.shells.map((shell, idx) => (
                  <div key={`secondary-shell-${idx}`} className={styles.shellSlot}>
                    <span className={styles.slotLabel}>
                      Shell {idx + 1}
                      {isShellChanged(reassemblyMode.source.shells[idx], shell) && (
                        <span className={styles.changedTag}>Changed</span>
                      )}
                    </span>
                    <button
                      className={`${styles.slotBox} ${shell.serial ? styles.filled : ''} ${reassemblyMode.selectedField?.target === 'secondary' && reassemblyMode.selectedField.component === 'shell' && reassemblyMode.selectedField.shellIndex === idx ? styles.selectedSlot : ''}`}
                      onClick={() => selectReassemblyField({ target: 'secondary', component: 'shell', shellIndex: idx })}
                    >
                      {shell.serial || 'Select + shell'}
                    </button>
                  </div>
                ))}
                <div className={styles.headSlot}>
                  <span className={styles.slotLabel}>
                    Right Head
                    {isHeadChanged(reassemblyMode.source.rightHead, reassemblyMode.secondary.rightHead) && (
                      <span className={styles.changedTag}>Changed</span>
                    )}
                  </span>
                  <button
                    className={`${styles.slotBox} ${reassemblyMode.secondary.rightHead ? styles.filled : ''} ${reassemblyMode.selectedField?.target === 'secondary' && reassemblyMode.selectedField.component === 'rightHead' ? styles.selectedSlot : ''}`}
                    onClick={() => selectReassemblyField({ target: 'secondary', component: 'rightHead' })}
                  >
                    {reassemblyMode.secondary.rightHead?.cardColor && (
                      <span
                        className={styles.headColorSwatch}
                        style={{ backgroundColor: reassemblyMode.secondary.rightHead.cardColor.toLowerCase() }}
                        aria-hidden="true"
                      />
                    )}
                    {reassemblyMode.secondary.rightHead ? formatReassemblyHeadSlot(reassemblyMode.secondary.rightHead) : 'Select + KC'}
                  </button>
                </div>
              </div>
            </>
          )}

          <div className={styles.manualRow}>
            {reassemblyMode.selectedField?.component === 'shell' ? (
              <Input
                value={reassemblyMode.entryValue}
                onChange={(_, d) => setReassemblyMode((current) => current ? { ...current, entryValue: d.value } : current)}
                placeholder="Enter shell serial..."
                className={styles.serialInput}
              />
            ) : (
              <div className={styles.reassemblyHeadPicker}>
                <Dropdown
                  className={styles.serialInput}
                  value={selectedReassemblyHeadLabel}
                  selectedOptions={reassemblyMode.entryValue ? [reassemblyMode.entryValue] : []}
                  placeholder="Select queued head material..."
                  onOptionSelect={(_, d: OptionOnSelectData) => {
                    setReassemblyMode((current) => current ? { ...current, entryValue: d.optionValue ?? '' } : current);
                  }}
                  disabled={!reassemblyMode.selectedField || headsQueue.length === 0}
                >
                  {headsQueue
                    .filter((item) => item.cardId)
                    .map((item) => (
                      <Option
                        key={item.id}
                        value={item.cardId!}
                        text={formatQueueHeadLabel(item)}
                      >
                        <span className={styles.reassemblyHeadOption}>
                          <span
                            className={styles.optionColorSwatch}
                            style={{ backgroundColor: item.cardColor ? item.cardColor.toLowerCase() : '#dee2e6' }}
                            aria-hidden="true"
                          />
                          <span>{formatQueueHeadLabel(item)}</span>
                        </span>
                      </Option>
                    ))}
                </Dropdown>
                {selectedReassemblyHeadLabel && (
                  <div className={styles.reassemblySelectedHead}>
                    <span
                      className={styles.optionColorSwatch}
                      style={{
                        backgroundColor: selectedReassemblyHeadItem?.cardColor
                          ? selectedReassemblyHeadItem.cardColor.toLowerCase()
                          : '#dee2e6',
                      }}
                      aria-hidden="true"
                    />
                    <span>{selectedReassemblyHeadLabel}</span>
                  </div>
                )}
              </div>
            )}
            <Button
              appearance="primary"
              onClick={() => {
                if (!reassemblyMode.selectedField || !reassemblyMode.entryValue.trim()) return;
                if (reassemblyMode.selectedField.component === 'shell') {
                  applyShellToSelection(reassemblyMode.entryValue.trim());
                } else {
                  applyHeadToSelection(reassemblyMode.entryValue.trim());
                }
              }}
              disabled={!reassemblyMode.selectedField || !reassemblyMode.entryValue.trim()}
            >
              Apply
            </Button>
            <Button appearance="secondary" onClick={saveReassembly} disabled={!hasReassemblyChanges(reassemblyMode)}>
              Create Reassembly
            </Button>
            <Button appearance="subtle" onClick={exitReassemblyMode}>
              Cancel Reassembly
            </Button>
          </div>
        </div>
      )}

      {!reassemblyMode && <div className={styles.assemblyDiagram}>
        <div className={styles.headSlot}>
          <span className={styles.slotLabel}>Left Head</span>
          <div className={`${styles.slotBox} ${leftHead ? (isComponentMismatch(tankSize, leftHead.tankSize) ? styles.headMismatch : styles.filled) : ''}`}>
            {leftHead && (
              <span
                className={styles.headColorSwatch}
                style={{ backgroundColor: leftHead.cardColor ? leftHead.cardColor.toLowerCase() : '#dee2e6' }}
              />
            )}
            {leftHead ? (
              <span className={styles.slotInfo}>
                {leftHead.productDescription}<br />
                {leftHead.lotNumber
                  ? `Lot: ${leftHead.lotNumber}`
                  : `H: ${leftHead.heatNumber || '—'} / C: ${leftHead.coilNumber || '—'}`}
              </span>
            ) : (
              <span className={styles.slotPlaceholder}>Scan KC</span>
            )}
          </div>
        </div>

        {shells.map((shell, idx) => (
          <div key={idx} className={styles.shellSlot}>
            <span className={styles.slotLabel}>Shell {idx + 1}</span>
            <div className={`${styles.slotBox} ${shell.filled ? (isComponentMismatch(tankSize, shell.tankSize) ? styles.headMismatch : styles.filled) : ''}`}>
              {shell.filled && !isComponentMismatch(tankSize, shell.tankSize) && (
                <span className={styles.goodCheckMark} aria-label="Shell good">✓</span>
              )}
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
          <div className={`${styles.slotBox} ${rightHead ? (isComponentMismatch(tankSize, rightHead.tankSize) ? styles.headMismatch : styles.filled) : ''}`}>
            {rightHead && (
              <span
                className={styles.headColorSwatch}
                style={{ backgroundColor: rightHead.cardColor ? rightHead.cardColor.toLowerCase() : '#dee2e6' }}
              />
            )}
            {rightHead ? (
              <span className={styles.slotInfo}>
                {rightHead.productDescription}<br />
                {rightHead.lotNumber
                  ? `Lot: ${rightHead.lotNumber}`
                  : `H: ${rightHead.heatNumber || '—'} / C: ${rightHead.coilNumber || '—'}`}
              </span>
            ) : (
              <span className={styles.slotPlaceholder}>Scan KC</span>
            )}
          </div>
        </div>
      </div>}

      {!props.externalInput && !reassemblyMode && (
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

      {!reassemblyMode && <div className={styles.queueSection}>
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
              <span
                className={styles.cardColorSwatch}
                style={{ backgroundColor: item.cardColor ? item.cardColor.toLowerCase() : '#dee2e6' }}
              />
              <div className={styles.queueCardInfo}>
                <span>{item.shellSize ? `(${item.shellSize}) ` : ''}{item.productDescription}</span>
                <span className={styles.queueCardDetail}>
                  {item.lotNumber
                    ? `Card ${item.cardId ?? '—'} · Lot ${item.lotNumber}`
                    : `Card ${item.cardId ?? '—'} · Heat ${item.heatNumber || '—'} / Coil ${item.coilNumber || '—'}`}
                </span>
              </div>
            </button>
          ))
        )}
      </div>}
    </div>
  );
}
