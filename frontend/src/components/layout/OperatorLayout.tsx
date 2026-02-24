import { useState, useCallback, useEffect, useRef, useMemo, lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';
import { Button, Input, Label, Spinner } from '@fluentui/react-components';
import { DismissRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { getTabletCache } from '../../hooks/useLocalStorage.ts';
import { useBarcode } from '../../hooks/useBarcode.ts';
import { useHeartbeat } from '../../hooks/useHeartbeat.ts';
import { useInactivityTracker } from '../../hooks/useInactivityTracker.ts';
import { parseBarcode, type ParsedBarcode } from '../../types/barcode.ts';
import type { Welder, WCHistoryData, QueueTransaction, DowntimeConfig } from '../../types/domain.ts';
import { workCenterApi, adminWorkCenterApi, adminPlantGearApi, downtimeConfigApi, downtimeEventApi } from '../../api/endpoints.ts';
import { TopBar } from './TopBar.tsx';
import { BottomBar } from './BottomBar.tsx';
import { LeftPanel } from './LeftPanel.tsx';
import { WCHistory } from './WCHistory.tsx';
import { QueueHistory } from './QueueHistory.tsx';
import { ScanOverlay, type ScanResult } from './ScanOverlay.tsx';
import { DowntimeOverlay } from './DowntimeOverlay.tsx';
import { DemoScanOverlay } from '../scanMenu/DemoScanOverlay.tsx';
import { useCurrentHelpArticle } from '../../help/useCurrentHelpArticle.ts';
import { reportException } from '../../telemetry/telemetryClient.ts';
import styles from './OperatorLayout.module.css';

const namedLazy = <T extends Record<string, React.ComponentType<any>>>(
  loader: () => Promise<T>,
  name: keyof T,
) => lazy(() => loader().then((m) => ({ default: m[name] as React.ComponentType<any> })));

const RollsScreen = namedLazy(() => import('../../features/rolls/RollsScreen.tsx'), 'RollsScreen');
const RollsMaterialScreen = namedLazy(() => import('../../features/rollsMaterial/RollsMaterialScreen.tsx'), 'RollsMaterialScreen');
const LongSeamScreen = namedLazy(() => import('../../features/longSeam/LongSeamScreen.tsx'), 'LongSeamScreen');
const LongSeamInspScreen = namedLazy(() => import('../../features/longSeamInsp/LongSeamInspScreen.tsx'), 'LongSeamInspScreen');
const FitupScreen = namedLazy(() => import('../../features/fitup/FitupScreen.tsx'), 'FitupScreen');
const FitupQueueScreen = namedLazy(() => import('../../features/fitupQueue/FitupQueueScreen.tsx'), 'FitupQueueScreen');
const RoundSeamScreen = namedLazy(() => import('../../features/roundSeam/RoundSeamScreen.tsx'), 'RoundSeamScreen');
const RoundSeamInspScreen = namedLazy(() => import('../../features/roundSeamInsp/RoundSeamInspScreen.tsx'), 'RoundSeamInspScreen');
const RtXrayQueueScreen = namedLazy(() => import('../../features/rtXrayQueue/RtXrayQueueScreen.tsx'), 'RtXrayQueueScreen');
const SpotXrayScreen = namedLazy(() => import('../../features/spotXray/SpotXrayScreen.tsx'), 'SpotXrayScreen');
const NameplateScreen = namedLazy(() => import('../../features/nameplate/NameplateScreen.tsx'), 'NameplateScreen');
const HydroScreen = namedLazy(() => import('../../features/hydro/HydroScreen.tsx'), 'HydroScreen');

const DATA_ENTRY_TO_LOG_TYPE: Record<string, string> = {
  Rolls: 'rolls',
  Fitup: 'fitup',
  Hydro: 'hydro',
  'MatQueue-Shell': 'rt-xray',
  Spot: 'spot-xray',
};

function dataEntryTypeToLogType(dataEntryType: string): string | undefined {
  return DATA_ENTRY_TO_LOG_TYPE[dataEntryType];
}

export function OperatorLayout() {
  const { user, logout } = useAuth();
  const [cache] = useState(getTabletCache);

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
  const [welderCountLoaded, setWelderCountLoaded] = useState(cache?.cachedNumberOfWelders != null);

  const [externalInput, setExternalInput] = useState(false);
  const [welders, setWelders] = useState<Welder[]>([]);
  const [scanResult, setScanResult] = useState<ScanResult | null>(null);
  const scanTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const barcodeHandlerRef = useRef<((bc: ParsedBarcode | null, raw: string) => void) | null>(null);
  const [historyData, setHistoryData] = useState<WCHistoryData>({ dayCount: 0, recentRecords: [] });
  const [queueTransactions, setQueueTransactions] = useState<QueueTransaction[]>([]);
  const [currentGearLevel, setCurrentGearLevel] = useState<number | null>(null);
  const [welderGateEmpNo, setWelderGateEmpNo] = useState('');
  const [welderGateError, setWelderGateError] = useState('');
  const [welderGateLoading, setWelderGateLoading] = useState(false);
  const [welderGateLookupName, setWelderGateLookupName] = useState<string | null>(null);
  const welderGateDebounceRef = useRef<ReturnType<typeof setTimeout>>(undefined);

  const [downtimeConfig, setDowntimeConfig] = useState<DowntimeConfig | null>(null);
  const [showDowntimeOverlay, setShowDowntimeOverlay] = useState(false);
  const [showDemoScanOverlay, setShowDemoScanOverlay] = useState(false);
  const [hideDemoScanOverlayTemporarily, setHideDemoScanOverlayTemporarily] = useState(false);
  const hideDemoScanOverlayTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pendingDemoOverlayPulseRef = useRef(false);
  const previousScanResultRef = useRef<ScanResult | null>(null);
  const prevExternalInputRef = useRef(false);
  const [downtimeLastActivity, setDowntimeLastActivity] = useState(0);
  const autoLogoutTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const AUTO_LOGOUT_MINUTES = 60;

  const dataEntryType = cache?.cachedDataEntryType ?? '';
  const helpArticle = useCurrentHelpArticle(dataEntryType);
  const hideHistory = dataEntryType === 'Spot';
  const weldersSatisfied = welders.length >= numberOfWelders;
  const showWelderGate = numberOfWelders > 0 && !weldersSatisfied;

  const isQueueScreen = dataEntryType === 'MatQueue-Material' || dataEntryType === 'MatQueue-Fitup';

  const queueTxnWCId = cache?.cachedMaterialQueueForWCId ?? cache?.cachedWorkCenterId;

  const loadQueueTransactions = useCallback(async () => {
    if (!queueTxnWCId) return;
    try {
      const txns = await workCenterApi.getQueueTransactions(queueTxnWCId, user?.defaultSiteId, 5, 'added');
      setQueueTransactions(txns);
    } catch { /* keep stale */ }
  }, [queueTxnWCId, user?.defaultSiteId]);

  const loadHistory = useCallback(async () => {
    if (!cache?.cachedWorkCenterId || !user?.defaultSiteId) return;
    try {
      const data = await workCenterApi.getHistory(cache.cachedWorkCenterId, '', user.defaultSiteId, cache.cachedAssetId || undefined);
      setHistoryData(data);
    } catch {
      // Keep stale data
    }
  }, [cache?.cachedWorkCenterId, user?.defaultSiteId, cache?.cachedAssetId]);

  const loadNumberOfWelders = useCallback(async () => {
    if (!cache?.cachedWorkCenterId) return;
    try {
      const wcs = await workCenterApi.getWorkCenters();
      const wc = wcs.find((w) => w.id === cache.cachedWorkCenterId);
      if (!wc) return;

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const raw = wc as any;
      let count = typeof raw.numberOfWelders === 'number'
        ? raw.numberOfWelders
        : raw.requiresWelder ? 1 : 0;

      if (cache.cachedProductionLineId) {
        try {
          const plConfig = await adminWorkCenterApi.getProductionLineConfig(
            cache.cachedWorkCenterId, cache.cachedProductionLineId);
          count = plConfig.numberOfWelders;
        } catch {
          // No per-line config — keep base WorkCenter default
        }
      }

      setNumberOfWelders(count);
      localStorage.setItem('cachedNumberOfWelders', String(count));
    } catch {
      const wcName = (cache.cachedWorkCenterName ?? '').toLowerCase();
      const welderRequired = ['rolls', 'long seam', 'fitup', 'round seam']
        .some((n) => wcName.includes(n))
        && !wcName.includes('material') && !wcName.includes('queue')
        && !wcName.includes('inspection') && !wcName.includes('insp');
      if (welderRequired) {
        setNumberOfWelders(1);
        localStorage.setItem('cachedNumberOfWelders', '1');
      }
    } finally {
      setWelderCountLoaded(true);
    }
  }, [cache?.cachedWorkCenterId, cache?.cachedWorkCenterName, cache?.cachedProductionLineId]);

  useEffect(() => {
    if (user) {
      setWelders([{
        userId: user.id,
        displayName: user.displayName,
        employeeNumber: user.employeeNumber,
      }]);
    }
  }, [user]);

  useEffect(() => {
    if (!cache?.cachedWorkCenterId) return;
    if (isQueueScreen) {
      loadQueueTransactions();
    } else {
      loadHistory();
    }
  }, [cache?.cachedWorkCenterId, isQueueScreen, loadQueueTransactions, loadHistory]);

  useEffect(() => {
    if (!cache?.cachedWorkCenterId) return;
    loadNumberOfWelders();
  }, [cache?.cachedWorkCenterId, loadNumberOfWelders]);

  useEffect(() => {
    if (!user?.defaultSiteId) return;
    adminPlantGearApi.getAll()
      .then((plants) => {
        const mine = plants.find((p) => p.plantId === user.defaultSiteId);
        setCurrentGearLevel(mine?.currentGearLevel ?? null);
      })
      .catch(() => { /* keep null */ });
  }, [user?.defaultSiteId]);

  // ---- Downtime tracking ----

  useEffect(() => {
    if (!cache?.cachedWorkCenterId || !cache?.cachedProductionLineId) return;
    downtimeConfigApi.get(cache.cachedWorkCenterId, cache.cachedProductionLineId)
      .then(setDowntimeConfig)
      .catch(() => setDowntimeConfig(null));
  }, [cache?.cachedWorkCenterId, cache?.cachedProductionLineId]);

  const handleInactivityDetected = useCallback((lastActivityTimestamp: number) => {
    setDowntimeLastActivity(lastActivityTimestamp);
    setShowDowntimeOverlay(true);

    if (autoLogoutTimerRef.current) clearTimeout(autoLogoutTimerRef.current);
    const thresholdMs = (downtimeConfig?.downtimeThresholdMinutes ?? 5) * 60 * 1000;
    const remainingMs = AUTO_LOGOUT_MINUTES * 60 * 1000 - thresholdMs;
    autoLogoutTimerRef.current = setTimeout(async () => {
      if (!cache?.cachedProductionLineId || !user?.id) return;

      try {
        const wcpls = await adminWorkCenterApi.getProductionLineConfigs(cache.cachedWorkCenterId!);
        const wcpl = wcpls.find(w => w.productionLineId === cache.cachedProductionLineId);
        if (wcpl) {
          await downtimeEventApi.create({
            workCenterProductionLineId: wcpl.id,
            operatorUserId: user.id,
            startedAt: new Date(lastActivityTimestamp).toISOString(),
            endedAt: new Date().toISOString(),
            isAutoGenerated: true,
          });
        }
      } catch { /* best effort */ }
      logout();
    }, Math.max(remainingMs, 0));
  }, [downtimeConfig, cache?.cachedWorkCenterId, cache?.cachedProductionLineId, user?.id, logout]);

  const { resetTimer: resetInactivityTimer } = useInactivityTracker({
    enabled: downtimeConfig?.downtimeTrackingEnabled ?? false,
    thresholdMinutes: downtimeConfig?.downtimeThresholdMinutes ?? 5,
    onInactivityDetected: handleInactivityDetected,
  });

  const handleDowntimeDismiss = useCallback(() => {
    setShowDowntimeOverlay(false);
    if (autoLogoutTimerRef.current) {
      clearTimeout(autoLogoutTimerRef.current);
      autoLogoutTimerRef.current = null;
    }
    resetInactivityTimer();
  }, [resetInactivityTimer]);

  useEffect(() => {
    return () => {
      if (autoLogoutTimerRef.current) clearTimeout(autoLogoutTimerRef.current);
    };
  }, []);

  // Resolve WCPL ID for overlay
  const [wcplId, setWcplId] = useState('');
  useEffect(() => {
    if (!cache?.cachedWorkCenterId || !cache?.cachedProductionLineId) return;
    adminWorkCenterApi.getProductionLineConfigs(cache.cachedWorkCenterId)
      .then(configs => {
        const match = configs.find(c => c.productionLineId === cache.cachedProductionLineId);
        if (match) setWcplId(match.id);
      })
      .catch(() => {});
  }, [cache?.cachedWorkCenterId, cache?.cachedProductionLineId]);

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

  const pulseHideDemoOverlay = useCallback(() => {
    if (!showDemoScanOverlay) return;
    if (hideDemoScanOverlayTimerRef.current) clearTimeout(hideDemoScanOverlayTimerRef.current);
    setHideDemoScanOverlayTemporarily(true);
    hideDemoScanOverlayTimerRef.current = setTimeout(() => {
      hideDemoScanOverlayTimerRef.current = null;
      setHideDemoScanOverlayTemporarily(false);
    }, 2000);
  }, [showDemoScanOverlay]);

  const requestDemoOverlayPulseAfterScanOverlay = useCallback(() => {
    if (!showDemoScanOverlay) return;
    pendingDemoOverlayPulseRef.current = true;
  }, [showDemoScanOverlay]);

  const handleScan = useCallback(
    (bc: ParsedBarcode | null, raw: string) => {
      try {
        requestDemoOverlayPulseAfterScanOverlay();
        if (barcodeHandlerRef.current) {
          barcodeHandlerRef.current(bc, raw);
        } else if (!bc) {
          showScanResult({ type: 'error', message: 'Unknown barcode' });
        }
      } catch (error) {
        reportException(error, {
          category: 'scanner_error',
          source: 'operator_layout_scan_dispatch',
          severity: 'error',
          isReactRuntimeOverlayCandidate: false,
          message: 'Unhandled scanner dispatch exception',
          metadataJson: JSON.stringify({ raw }),
        });
      }
    },
    [requestDemoOverlayPulseAfterScanOverlay, showScanResult],
  );

  const handleDemoBarcodeClick = useCallback((raw: string) => {
    handleScan(parseBarcode(raw), raw);
  }, [handleScan]);

  const { inputRef, handleKeyDown, focusLost } = useBarcode({
    enabled: externalInput && !showDowntimeOverlay,
    onScan: handleScan,
  });

  const handleFocusRestore = useCallback(() => {
    inputRef.current?.focus();
    window.focus();
  }, [inputRef]);

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
        const w = await workCenterApi.lookupWelder(cache.cachedWorkCenterId, employeeNumber);
        setWelders((prev) => {
          if (prev.some((existing) => existing.userId === w.userId)) return prev;
          return [...prev, w];
        });
      } catch {
        showScanResult({ type: 'error', message: 'Employee not found' });
      }
    },
    [cache?.cachedWorkCenterId, showScanResult],
  );

  const removeWelder = useCallback(
    (userId: string) => {
      setWelders((prev) => prev.filter((w) => w.userId !== userId));
    },
    [],
  );

  const handleWelderGateAdd = useCallback(async () => {
    if (!welderGateEmpNo.trim() || !cache?.cachedWorkCenterId) return;
    setWelderGateError('');
    setWelderGateLoading(true);
    try {
      const w = await workCenterApi.lookupWelder(cache.cachedWorkCenterId, welderGateEmpNo.trim());
      setWelders((prev) => {
        if (prev.some((existing) => existing.userId === w.userId)) return prev;
        return [...prev, w];
      });
      setWelderGateEmpNo('');
      setWelderGateLookupName(null);
    } catch {
      setWelderGateError('Employee not found');
    } finally {
      setWelderGateLoading(false);
    }
  }, [welderGateEmpNo, cache?.cachedWorkCenterId]);

  const handleWelderGateCancel = useCallback(() => {
    logout();
  }, [logout]);

  const supportsExternalInput = !(
    dataEntryType === 'MatQueue-Material' ||
    dataEntryType === 'MatQueue-Fitup' ||
    dataEntryType === 'DataPlate' ||
    dataEntryType === 'Spot'
  );
  useEffect(() => {
    const wasExternalInput = prevExternalInputRef.current;
    const turnedOn = externalInput && !wasExternalInput;

    if (turnedOn && user?.demoMode) {
      setHideDemoScanOverlayTemporarily(false);
      pendingDemoOverlayPulseRef.current = false;
      setShowDemoScanOverlay(true);
    }
    if (!externalInput) {
      setShowDemoScanOverlay(false);
      setHideDemoScanOverlayTemporarily(false);
      pendingDemoOverlayPulseRef.current = false;
    }

    prevExternalInputRef.current = externalInput;
  }, [externalInput, user?.demoMode]);

  useEffect(() => {
    const previous = previousScanResultRef.current;
    const overlayJustClosed = previous !== null && scanResult === null;

    if (overlayJustClosed && pendingDemoOverlayPulseRef.current) {
      pendingDemoOverlayPulseRef.current = false;
      pulseHideDemoOverlay();
    }

    previousScanResultRef.current = scanResult;
  }, [scanResult, pulseHideDemoOverlay]);

  useEffect(() => {
    return () => {
      if (hideDemoScanOverlayTimerRef.current) clearTimeout(hideDemoScanOverlayTimerRef.current);
    };
  }, []);

  const wcProps = {
    workCenterId: cache?.cachedWorkCenterId ?? '',
    assetId: cache?.cachedAssetId ?? '',
    productionLineId: cache?.cachedProductionLineId ?? '',
    operatorId: user?.id ?? '',
    plantId: user?.defaultSiteId ?? '',
    welders,
    numberOfWelders,
    welderCountLoaded,
    externalInput,
    setExternalInput,
    materialQueueForWCId: cache?.cachedMaterialQueueForWCId,
    demoModeEnabled: !!user?.demoMode,
    showScanResult,
    refreshHistory,
    registerBarcodeHandler,
  };

  return (
    <div className={styles.shell}>
      {externalInput && focusLost && (
        <div
          className={styles.focusLostBanner}
          onClick={handleFocusRestore}
          role="alert"
        >
          <span className={styles.focusLostIcon}>&#9888;</span>
          Scanner inactive &mdash; tap here to restore
        </div>
      )}

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
        helpArticle={helpArticle}
      />

      <div className={styles.middle}>
        <LeftPanel
          externalInput={externalInput}
          currentGearLevel={currentGearLevel}
        />

        <main
          className={styles.content}
          style={{
            pointerEvents: externalInput ? 'none' : 'auto',
            ...(hideHistory ? { flex: 1, maxWidth: '100%' } : {}),
          }}
        >
          <Suspense fallback={<Spinner size="medium" style={{ padding: 40 }} />}>
            <Routes>
              <Route index element={<WorkCenterRouter {...wcProps} />} />
            </Routes>
          </Suspense>
        </main>

        {!hideHistory && (
          <aside className={styles.rightPanel}>
            {isQueueScreen ? (
              <QueueHistory transactions={queueTransactions} />
            ) : (
              <WCHistory data={historyData} logType={dataEntryTypeToLogType(dataEntryType)} operatorId={user?.id} onAnnotationCreated={refreshHistory} />
            )}
          </aside>
        )}
      </div>

      <BottomBar
        plantCode={user?.plantCode ?? ''}
        externalInput={externalInput}
        onToggleExternalInput={() => setExternalInput((v) => !v)}
        showToggle={supportsExternalInput}
        scannerReady={externalInput && !focusLost}
      />

      {externalInput && !showDowntimeOverlay && (
        <input
          ref={inputRef}
          onKeyDown={handleKeyDown}
          className={styles.hiddenInput}
          aria-hidden="true"
          tabIndex={-1}
        />
      )}

      {showDowntimeOverlay && downtimeConfig && wcplId && (
        <DowntimeOverlay
          lastActivityTimestamp={downtimeLastActivity}
          reasons={downtimeConfig.applicableReasons}
          workCenterProductionLineId={wcplId}
          operatorUserId={user?.id ?? ''}
          onDismiss={handleDowntimeDismiss}
        />
      )}

      <DemoScanOverlay
        open={showDemoScanOverlay && !hideDemoScanOverlayTemporarily}
        workCenterId={cache?.cachedWorkCenterId ?? ''}
        dataEntryType={dataEntryType}
        demoModeEnabled={!!user?.demoMode}
        onClose={() => setShowDemoScanOverlay(false)}
        onMessage={showScanResult}
        onBarcodeClick={handleDemoBarcodeClick}
        onInteraction={requestDemoOverlayPulseAfterScanOverlay}
      />

      {scanResult && (
        <ScanOverlay
          result={scanResult}
          onDismiss={dismissScanResult}
          autoCloseMs={scanResult.type !== 'success' ? 5000 : undefined}
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
  plantId: string;
  welders: Welder[];
  numberOfWelders: number;
  welderCountLoaded: boolean;
  externalInput: boolean;
  setExternalInput: (value: boolean) => void;
  materialQueueForWCId?: string;
  demoModeEnabled?: boolean;
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
