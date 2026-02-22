import { useState, useEffect, useCallback, useRef } from 'react';
import { Dropdown, Option, Spinner } from '@fluentui/react-components';
import { ArrowUpRegular, ArrowDownRegular } from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, productionLineApi, digitalTwinApi } from '../../api/endpoints.ts';
import type {
  Plant,
  ProductionLine,
  DigitalTwinSnapshot,
  StationStatus,
} from '../../types/domain.ts';
import styles from './DigitalTwinScreen.module.css';

const REFRESH_INTERVAL = 30_000;

const STATION_BOX_CLASSES: Record<string, string> = {
  active: styles.stationBoxActive,
  slow: styles.stationBoxSlow,
  idle: styles.stationBoxIdle,
  down: styles.stationBoxDown,
  bottleneck: styles.stationBoxBottleneck,
};

const WIP_BADGE_CLASSES: Record<string, string> = {
  bottleneck: styles.wipBadgeBottleneck,
  idle: styles.wipBadgeIdle,
  slow: styles.wipBadgeSlow,
  down: styles.wipBadgeDown,
};

const STATUS_DOT_CLASSES: Record<string, string> = {
  active: styles.statusDotGreen,
  slow: styles.statusDotYellow,
  idle: styles.statusDotGray,
  down: styles.statusDotRed,
};

const STATUS_CIRCLE_COLORS: Record<string, string> = {
  active: 'var(--qs-success)',
  slow: 'var(--qs-warning)',
  idle: 'var(--qs-gray)',
  down: 'var(--qs-danger)',
  bottleneck: 'var(--qs-secondary)',
};

function formatTime(isoString: string): string {
  const d = new Date(isoString);
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatCycleTime(minutes: number | undefined | null): string {
  if (minutes == null || minutes === 0) return '-';
  if (minutes < 1) return `${Math.round(minutes * 60)}s`;
  return `${minutes.toFixed(1)} min`;
}

export function DigitalTwinScreen() {
  const { user } = useAuth();

  const [plants, setPlants] = useState<Plant[]>([]);
  const [selectedPlantId, setSelectedPlantId] = useState<string>('');
  const [productionLines, setProductionLines] = useState<ProductionLine[]>([]);
  const [selectedLineId, setSelectedLineId] = useState<string>('');
  const [snapshot, setSnapshot] = useState<DigitalTwinSnapshot | null>(null);
  const [loading, setLoading] = useState(false);
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    siteApi.getSites().then((sites) => {
      setPlants(sites);
      const defaultSite = sites.find((s) => s.id === user?.defaultSiteId) ?? sites[0];
      if (defaultSite) setSelectedPlantId(defaultSite.id);
    });
  }, [user?.defaultSiteId]);

  useEffect(() => {
    if (!selectedPlantId) return;
    productionLineApi.getProductionLines(selectedPlantId).then((lines) => {
      setProductionLines(lines);
      if (lines.length > 0) setSelectedLineId(lines[0].id);
      else setSelectedLineId('');
    });
  }, [selectedPlantId]);

  const fetchSnapshot = useCallback(async () => {
    if (!selectedLineId || !selectedPlantId) return;
    setLoading(true);
    try {
      const data = await digitalTwinApi.getSnapshot(selectedLineId, selectedPlantId);
      setSnapshot(data);
      setLastRefresh(new Date());
    } catch {
      /* silently retry on next interval */
    } finally {
      setLoading(false);
    }
  }, [selectedLineId, selectedPlantId]);

  useEffect(() => {
    fetchSnapshot();
    if (intervalRef.current) clearInterval(intervalRef.current);
    intervalRef.current = setInterval(fetchSnapshot, REFRESH_INTERVAL);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [fetchSnapshot]);

  const selectedPlant = plants.find((p) => p.id === selectedPlantId);
  const selectedLine = productionLines.find((l) => l.id === selectedLineId);

  return (
    <AdminLayout title={`Digital Twin${selectedLine ? ` — ${selectedLine.name}` : ''}`}>
      <div className={styles.container}>
        {/* ---- Toolbar ---- */}
        <div className={styles.toolbar}>
          <label>Plant</label>
          <Dropdown
            value={selectedPlant?.name ?? ''}
            selectedOptions={selectedPlantId ? [selectedPlantId] : []}
            onOptionSelect={(_, d) => setSelectedPlantId(d.optionValue as string)}
            style={{ minWidth: 160 }}
          >
            {plants.map((p) => (
              <Option key={p.id} value={p.id}>
                {p.name} ({p.code})
              </Option>
            ))}
          </Dropdown>

          <label>Production Line</label>
          <Dropdown
            value={selectedLine?.name ?? ''}
            selectedOptions={selectedLineId ? [selectedLineId] : []}
            onOptionSelect={(_, d) => setSelectedLineId(d.optionValue as string)}
            style={{ minWidth: 180 }}
            disabled={productionLines.length === 0}
          >
            {productionLines.map((l) => (
              <Option key={l.id} value={l.id}>
                {l.name}
              </Option>
            ))}
          </Dropdown>

          <div className={styles.refreshIndicator}>
            <span className={styles.refreshDot} />
            {lastRefresh
              ? `Updated ${lastRefresh.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}`
              : 'Loading...'}
          </div>
        </div>

        {/* ---- Main Content ---- */}
        {loading && !snapshot ? (
          <div className={styles.loadingContainer}>
            <Spinner size="large" />
            <span>Loading production line data...</span>
          </div>
        ) : !snapshot ? (
          <div className={styles.emptyState}>
            Select a plant and production line to view the Digital Twin.
          </div>
        ) : (
          <>
            <PipelineSection snapshot={snapshot} />
            <KpiCards snapshot={snapshot} />
            <div className={styles.bottomRow}>
              <StationDetailTable stations={snapshot.stations} />
              <UnitTracker snapshot={snapshot} />
            </div>
          </>
        )}
      </div>
    </AdminLayout>
  );
}

/* ===========================================================
   Pipeline Visualization
   =========================================================== */

function PipelineSection({ snapshot }: { snapshot: DigitalTwinSnapshot }) {
  const feedByStation: Record<string, typeof snapshot.materialFeeds[0]> = {};
  for (const feed of snapshot.materialFeeds) {
    feedByStation[feed.feedsIntoStation] = feed;
  }

  return (
    <div className={styles.pipelineSection}>
      <div className={styles.pipelineWrapper}>
        {snapshot.stations.map((station, idx) => (
          <StationNodeWithArrow
            key={station.workCenterId}
            station={station}
            isLast={idx === snapshot.stations.length - 1}
            materialFeed={feedByStation[station.name]}
          />
        ))}
      </div>
    </div>
  );
}

function StationNodeWithArrow({
  station,
  isLast,
  materialFeed,
}: {
  station: StationStatus;
  isLast: boolean;
  materialFeed?: { workCenterName: string; queueLabel: string; itemCount: number };
}) {
  const statusKey = station.isBottleneck ? 'bottleneck' : station.status.toLowerCase();
  const boxClass = STATION_BOX_CLASSES[statusKey] ?? '';
  const wipClass = WIP_BADGE_CLASSES[statusKey] ?? '';
  const circleColor = STATUS_CIRCLE_COLORS[statusKey] ?? 'var(--qs-gray)';
  const dotCount = statusKey === 'active' || statusKey === 'bottleneck' ? 3 : statusKey === 'slow' ? 2 : 0;

  return (
    <>
      <div className={styles.stationNode}>
        <div className={styles.stationStatusDots}>
          {Array.from({ length: dotCount }).map((_, i) => (
            <div
              key={i}
              className={styles.stationStatusCircle}
              style={{ background: circleColor }}
            />
          ))}
        </div>

        <div className={`${styles.stationBox} ${boxClass}`}>
          <div className={styles.stationName}>{station.name}</div>
          <div className={`${styles.wipBadge} ${wipClass}`}>
            {station.wipCount} WIP
          </div>
          {station.isGateCheck && (
            <div className={styles.gateCheckIcon} title="Gate Check">
              &#x2713;
            </div>
          )}
        </div>

        {materialFeed ? (
          <div className={styles.materialFeedContainer}>
            <div className={styles.feedArrowUp}>
              <div className={styles.feedArrowHead} />
              <div className={styles.feedArrowShaft} />
            </div>
            <div className={styles.materialFeed}>
              <div className={styles.materialFeedIcon}>
                {materialFeed.workCenterName.includes('Heads') ? '\u2B24' : '\u25A3'}
              </div>
              <div className={styles.materialFeedName}>{materialFeed.workCenterName}</div>
              <div className={styles.materialFeedCount}>{materialFeed.queueLabel}</div>
            </div>
          </div>
        ) : (
          <div className={styles.materialFeedPlaceholder} />
        )}
      </div>
      {!isLast && (
        <div className={styles.stationArrow}>
          <div className={styles.arrowLine} />
          <div className={styles.arrowDots}>
            <div className={styles.arrowDot} />
            <div className={styles.arrowDot} />
            <div className={styles.arrowDot} />
          </div>
          <div className={styles.arrowHead} />
        </div>
      )}
    </>
  );
}

/* ===========================================================
   KPI Cards
   =========================================================== */

function KpiCards({ snapshot }: { snapshot: DigitalTwinSnapshot }) {
  const { throughput, avgCycleTimeMinutes, lineEfficiencyPercent } = snapshot;
  const deltaClass =
    throughput.unitsDelta > 0
      ? styles.kpiPositive
      : throughput.unitsDelta < 0
        ? styles.kpiNegative
        : styles.kpiNeutral;

  return (
    <div className={styles.kpiRow}>
      {/* Throughput */}
      <div className={styles.kpiCard}>
        <div className={styles.kpiHeader}>Line Throughput</div>
        <div className={styles.kpiBody}>
          <div className={styles.kpiValue}>
            {throughput.unitsToday}
            <span className={styles.kpiSuffix}>units/day</span>
          </div>
          <div className={styles.kpiSubtext}>
            <span className={deltaClass}>
              {throughput.unitsDelta > 0 ? (
                <ArrowUpRegular style={{ fontSize: 14 }} />
              ) : throughput.unitsDelta < 0 ? (
                <ArrowDownRegular style={{ fontSize: 14 }} />
              ) : null}
              {throughput.unitsDelta >= 0 ? '+' : ''}
              {throughput.unitsDelta} vs yesterday
            </span>
          </div>
        </div>
      </div>

      {/* Avg Cycle Time */}
      <div className={styles.kpiCard}>
        <div className={styles.kpiHeader}>Avg Cycle Time</div>
        <div className={styles.kpiBody}>
          <div className={styles.kpiValue}>
            {avgCycleTimeMinutes > 0 ? avgCycleTimeMinutes.toFixed(1) : '-'}
            <span className={styles.kpiSuffix}>min</span>
          </div>
          <div className={`${styles.kpiSubtext} ${styles.kpiNeutral}`}>
            End-to-end (Rolls &#x2192; Hydro)
          </div>
        </div>
      </div>

      {/* Line Efficiency */}
      <div className={styles.kpiCard}>
        <div className={styles.kpiHeader}>Line Efficiency</div>
        <div className={styles.kpiBody}>
          <div className={styles.kpiValue}>
            {lineEfficiencyPercent}
            <span className={styles.kpiSuffix}>%</span>
          </div>
          <div className={styles.efficiencyBar}>
            <div
              className={styles.efficiencyFill}
              style={{ width: `${Math.min(100, lineEfficiencyPercent)}%` }}
            />
          </div>
        </div>
      </div>
    </div>
  );
}

/* ===========================================================
   Station Detail Table
   =========================================================== */

function StationDetailTable({ stations }: { stations: StationStatus[] }) {
  return (
    <div className={styles.panel}>
      <div className={styles.panelHeader}>Station Detail</div>
      <div className={styles.panelBody}>
        <table className={styles.stationTable}>
          <thead>
            <tr>
              <th>Station</th>
              <th>Status</th>
              <th>Operator</th>
              <th>Units Today</th>
              <th>Avg Cycle Time</th>
              <th>FPY%</th>
            </tr>
          </thead>
          <tbody>
            {stations.map((s) => {
              const dotClass = STATUS_DOT_CLASSES[s.status.toLowerCase()] ?? styles.statusDotGray;
              return (
                <tr
                  key={s.workCenterId}
                  className={s.isBottleneck ? styles.bottleneckRow : undefined}
                >
                  <td style={{ fontWeight: 600 }}>{s.name}</td>
                  <td>
                    <span className={dotClass} />
                    {s.status}
                  </td>
                  <td>{s.currentOperator ?? '-'}</td>
                  <td>{s.unitsToday}</td>
                  <td>{formatCycleTime(s.avgCycleTimeMinutes)}</td>
                  <td>{s.firstPassYieldPercent != null ? `${s.firstPassYieldPercent}%` : '-'}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

/* ===========================================================
   Live Unit Tracker
   =========================================================== */

function UnitTracker({ snapshot }: { snapshot: DigitalTwinSnapshot }) {
  const { stations, unitTracker } = snapshot;
  const colCount = stations.length;

  return (
    <div className={styles.panel}>
      <div className={styles.panelHeader}>Live Unit Tracker</div>
      <div className={styles.panelBody}>
        {unitTracker.length === 0 ? (
          <div className={styles.emptyState}>No units tracked today.</div>
        ) : (
          <div
            className={styles.unitTrackerGrid}
            style={{ gridTemplateColumns: `repeat(${colCount}, 1fr)` }}
          >
            {/* Header */}
            <div className={styles.utHeader}>
              {stations.map((s) => (
                <div key={s.workCenterId} className={styles.utHeaderCell}>
                  {s.name}
                </div>
              ))}
            </div>

            {/* Unit rows */}
            {unitTracker.map((unit) => (
              <div key={unit.serialNumber} className={styles.utRow}>
                {stations.map((s) => (
                  <div key={s.workCenterId} className={styles.utCell}>
                    {s.sequence === unit.currentStationSequence ? (
                      <div>
                        <span
                          className={`${styles.unitChip} ${unit.isAssembly ? styles.unitChipAssembly : ''}`}
                        >
                          {unit.serialNumber}
                        </span>
                        <span className={styles.unitChipTime}>
                          {formatTime(unit.enteredCurrentStationAt)}
                        </span>
                      </div>
                    ) : null}
                  </div>
                ))}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
