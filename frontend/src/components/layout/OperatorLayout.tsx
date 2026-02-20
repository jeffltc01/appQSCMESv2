import { useState, useCallback, useEffect, useRef } from 'react';
import { Routes, Route, useNavigate } from 'react-router-dom';
import { Button, Input, Label } from '@fluentui/react-components';
import { DismissRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { getTabletCache } from '../../hooks/useLocalStorage.ts';
import { useBarcode } from '../../hooks/useBarcode.ts';
import type { ParsedBarcode } from '../../types/barcode.ts';
import type { Welder, WCHistoryData } from '../../types/domain.ts';
import { workCenterApi } from '../../api/endpoints.ts';
import { TopBar } from './TopBar.tsx';
import { BottomBar } from './BottomBar.tsx';
import { LeftPanel } from './LeftPanel.tsx';
import { WCHistory } from './WCHistory.tsx';
import { ScanOverlay, type ScanResult } from './ScanOverlay.tsx';
import { RollsScreen } from '../../features/rolls/RollsScreen.tsx';
import { RollsMaterialScreen } from '../../features/rollsMaterial/RollsMaterialScreen.tsx';
import { LongSeamScreen } from '../../features/longSeam/LongSeamScreen.tsx';
import { LongSeamInspScreen } from '../../features/longSeamInsp/LongSeamInspScreen.tsx';
import { FitupScreen } from '../../features/fitup/FitupScreen.tsx';
import { FitupQueueScreen } from '../../features/fitupQueue/FitupQueueScreen.tsx';
import { RoundSeamScreen } from '../../features/roundSeam/RoundSeamScreen.tsx';
import { RoundSeamInspScreen } from '../../features/roundSeamInsp/RoundSeamInspScreen.tsx';
import { RtXrayQueueScreen } from '../../features/rtXrayQueue/RtXrayQueueScreen.tsx';
import { SpotXrayScreen } from '../../features/spotXray/SpotXrayScreen.tsx';
import { NameplateScreen } from '../../features/nameplate/NameplateScreen.tsx';
import { HydroScreen } from '../../features/hydro/HydroScreen.tsx';
import styles from './OperatorLayout.module.css';

export function OperatorLayout() {
  const { user, isWelder } = useAuth();
  const navigate = useNavigate();
  const cache = getTabletCache();

  const numberOfWelders = cache?.cachedNumberOfWelders ?? 0;

  const [externalInput, setExternalInput] = useState(false);
  const [welders, setWelders] = useState<Welder[]>([]);
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);
  const barcodeHandlerRef = useRef<((bc: ParsedBarcode | null, raw: string) => void) | null>(null);
  const [historyData, setHistoryData] = useState<WCHistoryData>({ dayCount: 0, recentRecords: [] });
  const [welderGateEmpNo, setWelderGateEmpNo] = useState('');
  const [welderGateError, setWelderGateError] = useState('');
  const [welderGateLoading, setWelderGateLoading] = useState(false);

  const weldersSatisfied = welders.length >= numberOfWelders;
  const showWelderGate = numberOfWelders > 0 && !weldersSatisfied;

  useEffect(() => {
    if (!cache?.cachedWorkCenterId) return;
    loadWelders();
    loadHistory();
  }, [cache?.cachedWorkCenterId]);

  useEffect(() => {
    if (user && isWelder) {
      setWelders((prev) => {
        if (prev.some((w) => w.userId === user.id)) return prev;
        return [
          ...prev,
          {
            userId: user.id,
            displayName: user.displayName,
            employeeNumber: user.employeeNumber,
          },
        ];
      });
    }
  }, [user, isWelder]);

  const loadWelders = useCallback(async () => {
    if (!cache?.cachedWorkCenterId) return;
    try {
      const w = await workCenterApi.getWelders(cache.cachedWorkCenterId);
      setWelders(w);
    } catch {
      // Keep local state
    }
  }, [cache?.cachedWorkCenterId]);

  const loadHistory = useCallback(async () => {
    if (!cache?.cachedWorkCenterId) return;
    try {
      const today = new Date().toISOString().split('T')[0];
      const data = await workCenterApi.getHistory(cache.cachedWorkCenterId, today);
      setHistoryData(data);
    } catch {
      // Keep stale data
    }
  }, [cache?.cachedWorkCenterId]);

  const showScanResult = useCallback((result: ScanResult) => {
    setScanResult(result);
    setTimeout(() => setScanResult(null), 1800);
  }, []);

  const refreshHistory = useCallback(() => {
    loadHistory();
  }, [loadHistory]);

  const handleScan = useCallback(
    (bc: ParsedBarcode | null, raw: string) => {
      if (barcodeHandlerRef.current) {
        barcodeHandlerRef.current(bc, raw);
      } else if (!bc) {
        showScanResult({ type: 'error', message: 'Unknown barcode' });
      }
    },
    [showScanResult],
  );

  const { inputRef, handleKeyDown } = useBarcode({
    enabled: externalInput,
    onScan: handleScan,
  });

  const registerBarcodeHandler = useCallback(
    (handler: (bc: ParsedBarcode | null, raw: string) => void) => {
      barcodeHandlerRef.current = handler;
    },
    [],
  );

  const addWelder = useCallback(
    async (employeeNumber: string) => {
      if (!cache?.cachedWorkCenterId) return;
      try {
        const w = await workCenterApi.addWelder(cache.cachedWorkCenterId, employeeNumber);
        setWelders((prev) => [...prev, w]);
      } catch {
        showScanResult({ type: 'error', message: 'Failed to add welder' });
      }
    },
    [cache?.cachedWorkCenterId, showScanResult],
  );

  const removeWelder = useCallback(
    async (userId: string) => {
      if (!cache?.cachedWorkCenterId) return;
      try {
        await workCenterApi.removeWelder(cache.cachedWorkCenterId, userId);
        setWelders((prev) => prev.filter((w) => w.userId !== userId));
      } catch {
        showScanResult({ type: 'error', message: 'Failed to remove welder' });
      }
    },
    [cache?.cachedWorkCenterId, showScanResult],
  );

  const handleWelderGateAdd = useCallback(async () => {
    if (!welderGateEmpNo.trim() || !cache?.cachedWorkCenterId) return;
    setWelderGateError('');
    setWelderGateLoading(true);
    try {
      const w = await workCenterApi.addWelder(cache.cachedWorkCenterId, welderGateEmpNo.trim());
      setWelders((prev) => {
        if (prev.some((existing) => existing.userId === w.userId)) return prev;
        return [...prev, w];
      });
      setWelderGateEmpNo('');
    } catch {
      setWelderGateError('Employee not found or not a certified welder');
    } finally {
      setWelderGateLoading(false);
    }
  }, [welderGateEmpNo, cache?.cachedWorkCenterId]);

  const handleWelderGateCancel = useCallback(() => {
    navigate('/tablet-setup');
  }, [navigate]);

  const wcProps = {
    workCenterId: cache?.cachedWorkCenterId ?? '',
    assetId: cache?.cachedAssetId ?? '',
    productionLineId: cache?.cachedProductionLineId ?? '',
    operatorId: user?.id ?? '',
    welders,
    numberOfWelders,
    externalInput,
    materialQueueForWCId: cache?.cachedMaterialQueueForWCId,
    showScanResult,
    refreshHistory,
    registerBarcodeHandler,
  };

  return (
    <div className={styles.shell}>
      <TopBar
        workCenterName={cache?.cachedWorkCenterName ?? ''}
        productionLineName={cache?.cachedProductionLineName ?? ''}
        assetName={cache?.cachedAssetName ?? ''}
        operatorName={user?.displayName ?? ''}
        welders={welders}
        onAddWelder={addWelder}
        onRemoveWelder={removeWelder}
        externalInput={externalInput}
      />

      <div className={styles.middle}>
        <LeftPanel externalInput={externalInput} />

        <main
          className={styles.content}
          style={{ pointerEvents: externalInput ? 'none' : 'auto' }}
        >
          <Routes>
            <Route index element={<WorkCenterRouter {...wcProps} />} />
          </Routes>
        </main>

        <aside className={styles.rightPanel}>
          <WCHistory data={historyData} />
        </aside>
      </div>

      <BottomBar
        plantCode={user?.plantCode ?? ''}
        externalInput={externalInput}
        onToggleExternalInput={() => setExternalInput((v) => !v)}
      />

      {externalInput && (
        <input
          ref={inputRef}
          onKeyDown={handleKeyDown}
          className={styles.hiddenInput}
          aria-hidden="true"
          tabIndex={-1}
        />
      )}

      {scanResult && (
        <ScanOverlay result={scanResult} onDismiss={() => setScanResult(null)} />
      )}

      {showWelderGate && (
        <div className={styles.welderGateOverlay}>
          <div className={styles.welderGateDialog}>
            <h2 className={styles.welderGateTitle}>Welder Sign-In Required</h2>
            <p className={styles.welderGateStatus}>
              This work center requires {numberOfWelders} welder{numberOfWelders > 1 ? 's' : ''}.
              {welders.length > 0
                ? ` ${welders.length} of ${numberOfWelders} signed in.`
                : ' Add a welder to continue.'}
            </p>

            {welders.length > 0 && (
              <div className={styles.welderGateList}>
                {welders.map((w) => (
                  <div key={w.userId} className={styles.welderGateChip}>
                    <span>{w.displayName} ({w.employeeNumber})</span>
                    <button onClick={() => removeWelder(w.userId)} aria-label={`Remove ${w.displayName}`}>
                      <DismissRegular fontSize={14} />
                    </button>
                  </div>
                ))}
              </div>
            )}

            <div className={styles.welderGateForm}>
              <div style={{ flex: 1 }}>
                <Label>Employee Number</Label>
                <Input
                  value={welderGateEmpNo}
                  onChange={(_, d) => { setWelderGateEmpNo(d.value); setWelderGateError(''); }}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleWelderGateAdd(); }}
                  placeholder="Enter employee number..."
                  size="large"
                  inputMode="numeric"
                  disabled={welderGateLoading}
                />
              </div>
              <Button
                appearance="primary"
                size="large"
                onClick={handleWelderGateAdd}
                disabled={!welderGateEmpNo.trim() || welderGateLoading}
                style={{ marginTop: 22 }}
              >
                Add Welder
              </Button>
            </div>

            {welderGateError && (
              <div style={{ color: '#dc3545', fontSize: 13 }}>{welderGateError}</div>
            )}

            <div className={styles.welderGateActions}>
              <Button appearance="secondary" size="large" onClick={handleWelderGateCancel}>
                Cancel
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export interface WorkCenterProps {
  workCenterId: string;
  assetId: string;
  productionLineId: string;
  operatorId: string;
  welders: Welder[];
  numberOfWelders: number;
  externalInput: boolean;
  materialQueueForWCId?: string;
  showScanResult: (result: ScanResult) => void;
  refreshHistory: () => void;
  registerBarcodeHandler: (handler: (bc: ParsedBarcode | null, raw: string) => void) => void;
}

function WorkCenterRouter(props: WorkCenterProps) {
  const cache = getTabletCache();
  const wcName = cache?.cachedWorkCenterName?.toLowerCase() ?? '';

  if (wcName.includes('rolls material') || wcName.includes('rolls mat'))
    return <RollsMaterialScreen {...props} />;
  if (wcName.includes('rolls')) return <RollsScreen {...props} />;
  if (wcName.includes('long seam insp')) return <LongSeamInspScreen {...props} />;
  if (wcName.includes('long seam')) return <LongSeamScreen {...props} />;
  if (wcName.includes('fitup queue') || wcName.includes('fit-up queue') || wcName.includes('fit up queue'))
    return <FitupQueueScreen {...props} />;
  if (wcName.includes('fitup') || wcName.includes('fit-up') || wcName.includes('fit up'))
    return <FitupScreen {...props} />;
  if (wcName.includes('round seam insp'))
    return <RoundSeamInspScreen {...props} />;
  if (wcName.includes('round seam'))
    return <RoundSeamScreen {...props} />;
  if (wcName.includes('rt') && wcName.includes('xray') || wcName.includes('rt x-ray') || wcName.includes('real time'))
    return <RtXrayQueueScreen {...props} />;
  if (wcName.includes('spot') && wcName.includes('xray') || wcName.includes('spot x-ray'))
    return <SpotXrayScreen {...props} />;
  if (wcName.includes('nameplate') || wcName.includes('data plate'))
    return <NameplateScreen {...props} />;
  if (wcName.includes('hydro'))
    return <HydroScreen {...props} />;

  return (
    <div style={{ padding: 40, textAlign: 'center', color: '#868686' }}>
      <h2>Work Center: {cache?.cachedWorkCenterName ?? 'Unknown'}</h2>
      <p>No specific screen configured for this work center type.</p>
    </div>
  );
}
