import { useState, useCallback, useEffect, useRef, useMemo, lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';
import { Button, Input, Label, Spinner } from '@fluentui/react-components';
import { DismissRegular } from '@fluentui/react-icons';
import { useAuth } from '../../auth/AuthContext.tsx';
import { isOperatorKioskRole } from '../../auth/kioskPolicy.ts';
import { getTabletCache } from '../../hooks/useLocalStorage.ts';
import { useBarcode } from '../../hooks/useBarcode.ts';
import { useHeartbeat } from '../../hooks/useHeartbeat.ts';
import { useInactivityTracker } from '../../hooks/useInactivityTracker.ts';
import { parseBarcode, type ParsedBarcode } from '../../types/barcode.ts';
import type { Welder, WCHistoryData, QueueTransaction, DowntimeConfig, HourlyCount } from '../../types/domain.ts';
import { workCenterApi, adminWorkCenterApi, adminPlantGearApi, downtimeConfigApi, downtimeEventApi, supervisorDashboardApi } from '../../api/endpoints.ts';
import { TopBar } from './TopBar.tsx';
import { BottomBar } from './BottomBar.tsx';
import { LeftPanel } from './LeftPanel.tsx';
import { WCHistory } from './WCHistory.tsx';
import { QueueHistory } from './QueueHistory.tsx';
import { ScanOverlay, type ScanResult } from './ScanOverlay.tsx';
import { DowntimeOverlay } from './DowntimeOverlay.tsx';
import { DemoScanOverlay } from '../scanMenu/DemoScanOverlay.tsx';
import { useCurrentHelpArticle } from '../../help/useCurrentHelpArticle.ts';
import { reportException, reportTelemetry } from '../../telemetry/telemetryClient.ts';
import { normalizeTimeZoneId } from '../../utils/dateFormat.ts';
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
const ChecklistScreen = namedLazy(() => import('../../features/checklists/ChecklistScreen.tsx'), 'ChecklistScreen');

type OperatorLogCta = {
  label: string;
  logType: string;
};

const DATA_ENTRY_TO_LOG_CTA: Record<string, OperatorLogCta> = {
  Rolls: { label: 'View Rolls Log', logType: 'rolls' },
  Fitup: { label: 'View Fitup Log', logType: 'fitup' },
  Hydro: { label: 'View Hydro Log', logType: 'hydro' },
};

function dataEntryTypeToLogCta(dataEntryType: string): OperatorLogCta | undefined {
  return DATA_ENTRY_TO_LOG_CTA[dataEntryType];
}

type HourlySummaryRow = {
  hour: number;
  planned: number | null;
  actual: number;
};
const TARGET_TOLERANCE = 1;

function toHour24(rawHour: number): number {
  return ((rawHour % 24) + 24) % 24;
}

function getCurrentHourInTimeZone(timeZoneId?: string): number {
  const normalizedTimeZoneId = normalizeTimeZoneId(timeZoneId);
  try {
    const hourPart = new Intl.DateTimeFormat('en-US', {
      hour: '2-digit',
      hour12: false,
      ...(normalizedTimeZoneId ? { timeZone: normalizedTimeZoneId } : {}),
    }).format(new Date());
    const parsed = Number.parseInt(hourPart, 10);
    return Number.isNaN(parsed) ? new Date().getHours() : toHour24(parsed);
  } catch {
    return new Date().getHours();
  }
}

function formatHourLabel(hour: number): string {
  const normalized = toHour24(hour);
  const displayHour = normalized % 12 || 12;
  const suffix = normalized < 12 ? 'AM' : 'PM';
  return `${displayHour} ${suffix}`;
}

function getDateIsoInTimeZone(timeZoneId?: string): string {
  const normalizedTimeZoneId = normalizeTimeZoneId(timeZoneId);
  try {
    const date = new Date();
    const formatter = new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      ...(normalizedTimeZoneId ? { timeZone: normalizedTimeZoneId } : {}),
    });
    const parts = formatter.formatToParts(date);
    const year = parts.find((p) => p.type === 'year')?.value ?? String(date.getFullYear());
    const month = parts.find((p) => p.type === 'month')?.value ?? String(date.getMonth() + 1).padStart(2, '0');
    const day = parts.find((p) => p.type === 'day')?.value ?? String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  } catch {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}

export function OperatorLayout() {
  const { user, logout } = useAuth();
  const kioskMode = isOperatorKioskRole(user?.roleTier);
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
  const [downtimeConfigError, setDowntimeConfigError] = useState<string | null>(null);
  const [showDowntimeOverlay, setShowDowntimeOverlay] = useState(false);
  const [showDemoScanOverlay, setShowDemoScanOverlay] = useState(false);
  const [hideDemoScanOverlayTemporarily, setHideDemoScanOverlayTemporarily] = useState(false);
  const hideDemoScanOverlayTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pendingDemoOverlayPulseRef = useRef(false);
  const previousScanResultRef = useRef<ScanResult | null>(null);
  const prevExternalInputRef = useRef(false);
  const [downtimeLastActivity, setDowntimeLastActivity] = useState(0);
  const [showChecklistOverlay, setShowChecklistOverlay] = useState(false);
  const [selectedChecklistType, setSelectedChecklistType] = useState<string | null>(null);
  const [checklistConfig, setChecklistConfig] = useState({
    enableWorkCenterChecklist: false,
    enableSafetyChecklist: false,
  });
  const autoLogoutTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const AUTO_LOGOUT_MINUTES = 60;
  const DOWNTIME_CONFIG_FETCH_ERROR = 'Unable to load downtime reasons for this station. Check the connection and try again, then contact a Quality Manager if it persists.';
  const dataEntryType = cache?.cachedDataEntryType ?? '';
  const showScheduleButton = dataEntryType.trim().toLowerCase() === 'rolls';
  const isQueueScreen = dataEntryType === 'MatQueue-Material' || dataEntryType === 'MatQueue-Fitup';
  const historyLogCta = dataEntryType === 'MatQueue-Shell'
    ? undefined
    : dataEntryTypeToLogCta(dataEntryType);
  const [plannedData, setPlannedData] = useState<{
    hourMap: Record<number, number | null>;
    defaultPlan: number | null;
    metricsHourlyCounts: HourlyCount[];
  }>({ hourMap: {}, defaultPlan: null, metricsHourlyCounts: [] });

  const hasSummaryCard = !isQueueScreen && dataEntryType !== 'MatQueue-Shell';
  const checklistEnabled = checklistConfig.enableSafetyChecklist || checklistConfig.enableWorkCenterChecklist;

  const summaryDeltaClass = (planned: number | null, actual: number): string => {
    if (planned === null) return styles.summaryDeltaNeutral;
    const delta = actual - planned;
    if (delta === 0) return styles.summaryDeltaOnTarget;
    if (Math.abs(delta) <= TARGET_TOLERANCE) return styles.summaryDeltaNeutral;
    if (delta > 0) return styles.summaryDeltaOverTarget;
    return styles.summaryDeltaUnderTarget;
  };

  const currentHour = getCurrentHourInTimeZone(user?.plantTimeZoneId);

  const summaryRows = useMemo(() => {
    const rows: HourlySummaryRow[] = [];
    const hourlyCounts = (historyData.hourlyCounts?.length ?? 0) > 0
      ? historyData.hourlyCounts!
      : plannedData.metricsHourlyCounts;
    const actualByHour = new Map<number, number>();
    for (const h of hourlyCounts) {
      actualByHour.set(h.hour, h.count);
    }
    for (let offset = 4; offset >= 0; offset -= 1) {
      const hour = toHour24(currentHour - offset);
      rows.push({
        hour,
        planned: plannedData.hourMap[hour] ?? plannedData.defaultPlan,
        actual: actualByHour.get(hour) ?? 0,
      });
    }
    return rows;
  }, [currentHour, historyData.hourlyCounts, plannedData]);

  const summaryStatus = useMemo(() => {
    const comparableRows = summaryRows.filter((row) => row.planned !== null);
    if (comparableRows.length === 0) {
      return { label: 'In Progress', className: styles.summaryStatusNeutral };
    }

    const totals = comparableRows.reduce(
      (acc, row) => ({
        planned: acc.planned + (row.planned ?? 0),
        actual: acc.actual + row.actual,
      }),
      { planned: 0, actual: 0 },
    );

    const delta = totals.actual - totals.planned;
    if (delta === 0) {
      return { label: 'On Target', className: styles.summaryStatusOnTarget };
    }
    if (Math.abs(delta) <= TARGET_TOLERANCE) {
      return { label: 'Within Tolerance', className: styles.summaryStatusNeutral };
    }
    if (delta > 0) {
      return { label: 'Over Target', className: styles.summaryStatusOverTarget };
    }
    return { label: 'Under Target', className: styles.summaryStatusUnderTarget };
  }, [summaryRows]);

  const hasCapacityTarget = useMemo(
    () => summaryRows.some((row) => row.planned !== null),
    [summaryRows],
  );

  const loadPlannedData = useCallback(async () => {
    if (!cache?.cachedWorkCenterId || !user?.defaultSiteId) return;
    try {
      const date = getDateIsoInTimeZone(user.plantTimeZoneId);
      const [metrics, perf] = await Promise.all([
        supervisorDashboardApi.getMetrics(cache.cachedWorkCenterId, user.defaultSiteId, date).catch(() => null),
        supervisorDashboardApi.getPerformanceTable(
          cache.cachedWorkCenterId,
          user.defaultSiteId,
          'day',
          date,
        ).catch(() => null),
      ]);

      const hourMap: Record<number, number | null> = {};
      let defaultPlan: number | null = null;
      for (const row of perf?.rows ?? []) {
        const match = row.label.match(/^(\d{1,2}):/);
        if (!match) continue;
        const hour = toHour24(Number.parseInt(match[1], 10));
        hourMap[hour] = row.planned === null ? null : Math.floor(row.planned);
        if (defaultPlan === null && row.planned !== null) {
          defaultPlan = Math.floor(row.planned);
        }
      }
      setPlannedData({
        hourMap,
        defaultPlan,
        metricsHourlyCounts: metrics?.hourlyCounts ?? [],
      });
    } catch { /* keep stale planned data */ }
  }, [cache?.cachedWorkCenterId, user?.defaultSiteId, user?.plantTimeZoneId]);

  useEffect(() => {
    void loadPlannedData();
  }, [loadPlannedData]);

  const helpArticle = useCurrentHelpArticle(dataEntryType);
  const hideHistory = dataEntryType === 'Spot';
  const weldersSatisfied = welders.length >= numberOfWelders;
  const showWelderGate = numberOfWelders > 0 && !weldersSatisfied;

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
      const data = await workCenterApi.getHistory(
        cache.cachedWorkCenterId,
        '',
        user.defaultSiteId,
        cache.cachedProductionLineId,
        cache.cachedAssetId || undefined,
      );
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
          setChecklistConfig({
            enableWorkCenterChecklist: !!plConfig.enableWorkCenterChecklist,
            enableSafetyChecklist: !!plConfig.enableSafetyChecklist,
          });
        } catch {
          // No per-line config — keep base WorkCenter default
          setChecklistConfig({
            enableWorkCenterChecklist: false,
            enableSafetyChecklist: false,
          });
        }
      } else {
        setChecklistConfig({
          enableWorkCenterChecklist: false,
          enableSafetyChecklist: false,
        });
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

  const refreshDowntimeConfig = useCallback(async () => {
    if (!cache?.cachedWorkCenterId || !cache?.cachedProductionLineId) {
      setDowntimeConfig(null);
      setDowntimeConfigError(null);
      return { config: null, fetchFailed: false };
    }
    try {
      const config = await downtimeConfigApi.get(cache.cachedWorkCenterId, cache.cachedProductionLineId);
      setDowntimeConfig(config);
      setDowntimeConfigError(null);
      return { config, fetchFailed: false };
    } catch {
      setDowntimeConfig(null);
      setDowntimeConfigError(DOWNTIME_CONFIG_FETCH_ERROR);
      return { config: null, fetchFailed: true };
    }
  }, [cache?.cachedWorkCenterId, cache?.cachedProductionLineId, DOWNTIME_CONFIG_FETCH_ERROR]);

  useEffect(() => {
    void refreshDowntimeConfig();
  }, [refreshDowntimeConfig]);

  const handleInactivityDetected = useCallback((lastActivityTimestamp: number) => {
    void (async () => {
      setDowntimeLastActivity(lastActivityTimestamp);
      const { config: latestConfig, fetchFailed } = await refreshDowntimeConfig();
      const effectiveConfig = latestConfig ?? downtimeConfig;

      if (fetchFailed) {
        setShowDowntimeOverlay(true);
        return;
      }

      if (effectiveConfig && !effectiveConfig.downtimeTrackingEnabled) return;

      setShowDowntimeOverlay(true);

      if (autoLogoutTimerRef.current) clearTimeout(autoLogoutTimerRef.current);
      const thresholdMs = (effectiveConfig?.downtimeThresholdMinutes ?? 5) * 60 * 1000;
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
    })();
  }, [refreshDowntimeConfig, downtimeConfig, cache?.cachedWorkCenterId, cache?.cachedProductionLineId, user?.id, logout]);

  const { resetTimer: resetInactivityTimer } = useInactivityTracker({
    enabled: downtimeConfig?.downtimeTrackingEnabled ?? false,
    thresholdMinutes: downtimeConfig?.downtimeThresholdMinutes ?? 5,
    onInactivityDetected: handleInactivityDetected,
  });

  const handleDowntimeDismiss = useCallback(() => {
    setShowDowntimeOverlay(false);
    setDowntimeConfigError(null);
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
    if (result.type === 'error') {
      reportTelemetry({
        category: 'scan_feedback',
        source: 'operator_scan_overlay',
        severity: 'error',
        isReactRuntimeOverlayCandidate: false,
        message: result.message ?? 'Operator scan rejected',
        metadataJson: JSON.stringify({
          resultType: result.type,
          dataEntryType,
          externalInput,
          kioskMode,
        }),
      });
    }
    const delay = result.type === 'success' ? 1000 : 5000;
    scanTimerRef.current = setTimeout(() => { scanTimerRef.current = null; setScanResult(null); }, delay);
  }, [dataEntryType, externalInput, kioskMode]);

  const refreshHistory = useCallback(() => {
    void loadPlannedData();
    if (isQueueScreen) {
      loadQueueTransactions();
    } else {
      loadHistory();
    }
  }, [isQueueScreen, loadHistory, loadQueueTransactions, loadPlannedData]);

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
          kioskMode={kioskMode}
          showScheduleButton={showScheduleButton}
          showChecklistButton={checklistEnabled}
          onChecklistClick={() => {
            setSelectedChecklistType(null);
            setShowChecklistOverlay(true);
          }}
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
              <WCHistory
                data={historyData}
                logCta={historyLogCta}
                operatorId={user?.id}
                externalInput={externalInput}
                onAnnotationCreated={refreshHistory}
              />
            )}
          </aside>
        )}

        {hasSummaryCard && (
          <section className={styles.summaryFloatingCard} aria-label="Operator capacity indicator">
            <div className={styles.summaryTop}>
              <div className={styles.summaryTopLeft}>
                <span className={styles.summaryTopLabel}>Total Count</span>
                <span className={styles.summaryTopValue}>{historyData.dayCount}</span>
              </div>
              <span className={`${styles.summaryStatusBadge} ${summaryStatus.className}`}>
                {summaryStatus.label}
              </span>
            </div>

            <div className={styles.summaryBody}>
              <div className={styles.summaryHoursRow}>
                <span className={styles.summaryMetricLabel}>Hour</span>
                {summaryRows.map((row) => (
                  <span key={`hour-${row.hour}`}>{formatHourLabel(row.hour)}</span>
                ))}
              </div>

              {hasCapacityTarget && (
                <div className={styles.summaryMetricRow}>
                  <span className={styles.summaryMetricLabel}>Plan</span>
                  {summaryRows.map((row) => (
                    <span key={`plan-${row.hour}`}>{row.planned === null ? '--' : row.planned}</span>
                  ))}
                </div>
              )}

              <div className={`${styles.summaryMetricRow} ${styles.summaryActualRow}`}>
                <span className={styles.summaryMetricLabel}>Actual</span>
                {summaryRows.map((row) => (
                  <span key={`actual-${row.hour}`} className={summaryDeltaClass(row.planned, row.actual)}>{row.actual}</span>
                ))}
              </div>
            </div>
          </section>
        )}
      </div>

      <BottomBar
        plantCode={user?.plantCode ?? ''}
        plantTimeZoneId={user?.plantTimeZoneId}
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

      {showDowntimeOverlay && (
        <DowntimeOverlay
          lastActivityTimestamp={downtimeLastActivity}
          reasons={downtimeConfig?.applicableReasons ?? []}
          workCenterProductionLineId={wcplId}
          operatorUserId={user?.id ?? ''}
          configurationError={downtimeConfigError ?? undefined}
          onDismiss={handleDowntimeDismiss}
        />
      )}

      {showChecklistOverlay && (
        <div className={styles.checklistOverlay}>
          <div className={styles.checklistPanel}>
            <div className={styles.checklistPanelHeader}>
              <h2>Checklist</h2>
              <Button
                appearance="subtle"
                icon={<DismissRegular />}
                onClick={() => setShowChecklistOverlay(false)}
                aria-label="Close checklist"
              />
            </div>
            <div className={styles.checklistTypePicker}>
              <Button
                appearance={selectedChecklistType === 'OpsPreShift' ? 'primary' : 'secondary'}
                className={`${styles.checklistTypeButton} ${selectedChecklistType === 'OpsPreShift' ? styles.checklistTypeButtonActive : ''}`}
                onClick={() => setSelectedChecklistType('OpsPreShift')}
              >
                PreShift
              </Button>
              <Button
                appearance={selectedChecklistType === 'SafetyPreShift' ? 'primary' : 'secondary'}
                className={`${styles.checklistTypeButton} ${selectedChecklistType === 'SafetyPreShift' ? styles.checklistTypeButtonActive : ''}`}
                onClick={() => setSelectedChecklistType('SafetyPreShift')}
              >
                Safety
              </Button>
              <Button
                appearance={selectedChecklistType === 'OpsChangeover' ? 'primary' : 'secondary'}
                className={`${styles.checklistTypeButton} ${selectedChecklistType === 'OpsChangeover' ? styles.checklistTypeButtonActive : ''}`}
                onClick={() => setSelectedChecklistType('OpsChangeover')}
              >
                PostShift
              </Button>
            </div>
            <div className={styles.checklistPaper}>
              {selectedChecklistType ? (
                <ChecklistScreen
                  {...wcProps}
                  checklistType={selectedChecklistType}
                  onChecklistCompleted={() => setShowChecklistOverlay(false)}
                />
              ) : (
                <div className={styles.checklistPickPrompt}>Select checklist type to begin.</div>
              )}
            </div>
          </div>
        </div>
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
