import { useState, useCallback, useEffect, useRef, useMemo } from 'react';
import { Routes, Route, useNavigate } from 'react-router-dom';
import { Button, Input, Label } from '@fluentui/react-components';
import { DismissRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { getTabletCache } from '../../hooks/useLocalStorage.ts';
import { useBarcode } from '../../hooks/useBarcode.ts';
import { useHeartbeat } from '../../hooks/useHeartbeat.ts';
import type { ParsedBarcode } from '../../types/barcode.ts';
import type { Welder, WCHistoryData, QueueTransaction } from '../../types/domain.ts';
import { workCenterApi } from '../../api/endpoints.ts';
import { TopBar } from './TopBar.tsx';
import { BottomBar } from './BottomBar.tsx';
import { LeftPanel } from './LeftPanel.tsx';
import { WCHistory } from './WCHistory.tsx';
import { QueueHistory } from './QueueHistory.tsx';
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

  const sessionData = useMemo(() => {
    if (!cache?.cachedWorkCenterId || !cache.cachedProductionLineId || !user?.defaultSiteId) return undefined;
    return {
      workCenterId: cache.cachedWorkCenterId,
      productionLineId: cache.cachedProductionLineId,
      assetId: cache.cachedAssetId || undefined,
      plantId: user.defaultSiteId,
    };
  }, [cache?.cachedWorkCenterId, cache?.cachedProductionLineId, cache?.cachedAssetId, user?.defaultSiteId]);

  useHeartbeat(!!cache?.cachedWorkCenterId, sessionData);

  const [numberOfWelders, setNumberOfWelders] = useState(cache?.cachedNumberOfWelders ?? 0);

  const [externalInput, setExternalInput] = useState(false);
  const [welders, setWelders] = useState<Welder[]>([]);
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);
  const scanTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const barcodeHandlerRef = useRef<((bc: ParsedBarcode | null, raw: string) => void) | null>(null);
  const [historyData, setHistoryData] = useState<WCHistoryData>({ dayCount: 0, recentRecords: [] });
  const [queueTransactions, setQueueTransactions] = useState<QueueTransaction[]>([]);
  const [welderGateEmpNo, setWelderGateEmpNo] = useState('');
  const [welderGateError, setWelderGateError] = useState('');
  const [welderGateLoading, setWelderGateLoading] = useState(false);
  const [welderGateLookupName, setWelderGateLookupName] = useState<string | null>(null);
  const welderGateDebounceRef = useRef<ReturnType<typeof setTimeout>>();

  const dataEntryType = cache?.cachedDataEntryType ?? '';
  const weldersSatisfied = welders.length >= numberOfWelders;
  const showWelderGate = numberOfWelders > 0 && !weldersSatisfied;

  const isQueueScreen = dataEntryType === 'MatQueue-Material' || dataEntryType === 'MatQueue-Fitup';

  const queueTxnWCId = cache?.cachedMaterialQueueForWCId ?? cache?.cachedWorkCenterId;

  const loadQueueTransactions = useCallback(async () => {
    if (!queueTxnWCId) return;
    try {
      const txns = await workCenterApi.getQueueTransactions(queueTxnWCId);
      setQueueTransactions(txns);
    } catch { /* keep stale */ }
  }, [queueTxnWCId]);

  useEffect(() => {
    if (!cache?.cachedWorkCenterId) return;
    loadWelders();
    if (isQueueScreen) {
      loadQueueTransactions();
    } else {
      loadHistory();
    }
  }, [cache?.cachedWorkCenterId, isQueueScreen]);

  useEffect(() => {
    if (!cache?.cachedWorkCenterId) return;
    loadNumberOfWelders();
  }, [cache?.cachedWorkCenterId]);

  useEffect(() => {
    if (user && isWelder && cache?.cachedWorkCenterId) {
      workCenterApi.addWelder(cache.cachedWorkCenterId, user.employeeNumber)
        .then((w) => {
          setWelders((prev) => {
            if (prev.some((existing) => existing.userId === w.userId)) return prev;
            return [...prev, w];
          });
        })
        .catch(() => {
          setWelders((prev) => {
            if (prev.some((existing) => existing.userId === user.id)) return prev;
            return [
              ...prev,
              {
                userId: user.id,
                displayName: user.displayName,
                employeeNumber: user.employeeNumber,
              },
            ];
          });
        });
    }
  }, [user, isWelder, cache?.cachedWorkCenterId]);

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
    if (!cache?.cachedWorkCenterId || !user?.defaultSiteId) return;
    try {
      const now = new Date();
      const today = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
      const data = await workCenterApi.getHistory(cache.cachedWorkCenterId, today, user.defaultSiteId);
      setHistoryData(data);
    } catch {
      // Keep stale data
    }
  }, [cache?.cachedWorkCenterId, user?.defaultSiteId]);

  const loadNumberOfWelders = useCallback(async () => {
    if (!cache?.cachedWorkCenterId) return;
    try {
      const wcs = await workCenterApi.getWorkCenters();
      const wc = wcs.find((w) => w.id === cache.cachedWorkCenterId);
      if (wc) {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const raw = wc as any;
        const count = typeof raw.numberOfWelders === 'number'
          ? raw.numberOfWelders
          : raw.requiresWelder ? 1 : 0;
        setNumberOfWelders(count);
        localStorage.setItem('cachedNumberOfWelders', String(count));
      }
    } catch {
      // API failed — derive from work center name as fallback
      const wcName = (cache.cachedWorkCenterName ?? '').toLowerCase();
      const welderRequired = ['rolls', 'long seam', 'fitup', 'round seam']
        .some((n) => wcName.includes(n))
        && !wcName.includes('material') && !wcName.includes('queue')
        && !wcName.includes('inspection') && !wcName.includes('insp');
      if (welderRequired) {
        setNumberOfWelders(1);
        localStorage.setItem('cachedNumberOfWelders', '1');
      }
    }
  }, [cache?.cachedWorkCenterId, cache?.cachedWorkCenterName]);

  const dismissScanResult = useCallback(() => {
    if (scanTimerRef.current) { clearTimeout(scanTimerRef.current); scanTimerRef.current = null; }
    setScanResult(null);
  }, []);

  const showScanResult = useCallback((result: ScanResult) => {
    if (scanTimerRef.current) clearTimeout(scanTimerRef.current);
    setScanResult(result);
    const delay = result.type === 'success' ? 1000 : 5000;
    scanTimerRef.current = setTimeout(() => { scanTimerRef.current = null; setScanResult(null); }, delay);
  }, []);

  const refreshHistory = useCallback(() => {
    if (isQueueScreen) {
      loadQueueTransactions();
    } else {
      loadHistory();
    }
  }, [isQueueScreen, loadHistory, loadQueueTransactions]);

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
      setWelderGateLookupName(null);
    } catch {
      setWelderGateError('Employee not found or not a certified welder');
    } finally {
      setWelderGateLoading(false);
    }
  }, [welderGateEmpNo, cache?.cachedWorkCenterId]);

  const handleWelderGateCancel = useCallback(() => {
    navigate('/tablet-setup');
  }, [navigate]);

  const supportsExternalInput = !(
    dataEntryType === 'MatQueue-Material' ||
    dataEntryType === 'MatQueue-Fitup' ||
    dataEntryType === 'DataPlate' ||
    dataEntryType === 'Spot'
  );

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
        workCenterName={cache?.cachedWorkCenterDisplayName || cache?.cachedWorkCenterName || ''}
        workCenterId={cache?.cachedWorkCenterId ?? ''}
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
          {isQueueScreen ? (
            <QueueHistory transactions={queueTransactions} />
          ) : (
            <WCHistory data={historyData} />
          )}
        </aside>
      </div>

      <BottomBar
        plantCode={user?.plantCode ?? ''}
        externalInput={externalInput}
        onToggleExternalInput={() => setExternalInput((v) => !v)}
        showToggle={supportsExternalInput}
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
        <ScanOverlay
          result={scanResult}
          onDismiss={dismissScanResult}
          autoCloseMs={scanResult.type === 'error' ? 5000 : undefined}
        />
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
                  onChange={(_, d) => {
                    setWelderGateEmpNo(d.value);
                    setWelderGateError('');
                    setWelderGateLookupName(null);
                    if (welderGateDebounceRef.current) clearTimeout(welderGateDebounceRef.current);
                    const val = d.value.trim();
                    if (val && cache?.cachedWorkCenterId) {
                      welderGateDebounceRef.current = setTimeout(() => {
                        workCenterApi.lookupWelder(cache.cachedWorkCenterId!, val)
                          .then(w => setWelderGateLookupName(w.displayName))
                          .catch(() => setWelderGateLookupName('Not found'));
                      }, 500);
                    }
                  }}
                  onKeyDown={(e) => { if (e.key === 'Enter') handleWelderGateAdd(); }}
                  placeholder="Enter employee number..."
                  size="large"
                  inputMode="numeric"
                  disabled={welderGateLoading}
                />
                {welderGateLookupName && (
                  <div style={{ fontSize: 14, color: welderGateLookupName === 'Not found' ? '#c4314b' : '#2b8a3e', marginTop: 4 }}>
                    {welderGateLookupName}
                  </div>
                )}
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
  const dataEntryType = cache?.cachedDataEntryType ?? '';

  switch (dataEntryType) {
    case 'Rolls':              return <RollsScreen {...props} />;
    case 'Fitup':              return <FitupScreen {...props} />;
    case 'Hydro':              return <HydroScreen {...props} />;
    case 'Spot':               return <SpotXrayScreen {...props} />;
    case 'DataPlate':          return <NameplateScreen {...props} />;
    case 'RealTimeXray':       return <RtXrayQueueScreen {...props} />;
    case 'MatQueue-Material':  return <RollsMaterialScreen {...props} />;
    case 'MatQueue-Fitup':     return <FitupQueueScreen {...props} />;
    case 'MatQueue-Shell':     return <RtXrayQueueScreen {...props} />;
    case 'Barcode-LongSeam':     return <LongSeamScreen {...props} />;
    case 'Barcode-LongSeamInsp': return <LongSeamInspScreen {...props} />;
    case 'Barcode-RoundSeam':    return <RoundSeamScreen {...props} />;
    case 'Barcode-RoundSeamInsp': return <RoundSeamInspScreen {...props} />;
  }

  return (
    <div style={{ padding: 40, textAlign: 'center', color: '#868686' }}>
      <h2>Work Center: {cache?.cachedWorkCenterDisplayName || cache?.cachedWorkCenterName || 'Unknown'}</h2>
      <p>No specific screen configured for this work center type.</p>
    </div>
  );
}
