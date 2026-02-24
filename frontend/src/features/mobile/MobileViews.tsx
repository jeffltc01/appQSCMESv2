import { useEffect, useMemo, useState } from 'react';
import { Button, Input } from '@fluentui/react-components';
import { SignOutRegular } from '@fluentui/react-icons';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext.tsx';
import { activeSessionApi, adminPlantGearApi, digitalTwinApi, issueRequestApi, productionLineApi } from '../../api/endpoints.ts';
import type { DigitalTwinSnapshot, PlantWithGear, ProductionLine } from '../../types/domain.ts';
import type { IssueRequestDto } from '../../types/api.ts';
import { isOpsDirector, isQualityDirector } from '../../auth/mobilePolicy.ts';
import styles from './MobileScreens.module.css';

type CompareRow = { plant: string; summary: string; status: 'good' | 'warn' | 'critical' };
type IssueRow = { title: string; detail: string; severity: 'high' | 'critical' };
type MobileTab = { label: string; path: string };
type OpsBottleneck = { line: string; impact: string; cause: string };
type OpsKpis = {
  throughputUnits: number;
  throughputDeltaPct: number;
  downtimePct: number;
  downtimeDeltaPct: number;
  laborUtilizationPct: number;
  oeePct: number;
  oeeDeltaPct: number;
};
type QualityKpis = {
  fpyPct: number;
  fpyDeltaPct: number;
  defectPpm: number;
  defectDeltaPpm: number;
  scrapPct: number;
  scrapDeltaPct: number;
  openCapaCount: number;
  openCapaDelta: number;
};

const qualityIssues: IssueRow[] = [
  { title: 'Material variance in Line 4', detail: 'Assigned to J. Smith', severity: 'critical' },
  { title: 'Process deviation at oven temp', detail: 'Assigned to M. Davis', severity: 'high' },
  { title: 'Supplier non-conformance batch #234', detail: 'Assigned to L. Chen', severity: 'high' },
];

const qualityTabs: MobileTab[] = [
  { label: 'Portfolio', path: '/mobile/quality-portfolio' },
  { label: 'Alerts', path: '/mobile/alerts' },
  { label: 'Compare', path: '/mobile/quality-plants' },
  { label: 'More', path: '/mobile/more' },
];

const opsTabs: MobileTab[] = [
  { label: 'Home', path: '/mobile/operations-portfolio' },
  { label: 'Bottlenecks', path: '/mobile/bottlenecks' },
  { label: 'Plants', path: '/mobile/plants' },
  { label: 'Actions', path: '/mobile/actions' },
];

function ScopeHeader({ title, chips }: { title: string; chips: string[] }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <header className={styles.header}>
      <div className={styles.headerTop}>
        <div className={styles.headerIdentity}>
          <h1 className={styles.title}>{title}</h1>
          <span className={styles.subtitle}>{user?.plantName ?? user?.plantCode ?? ''}</span>
        </div>
        <Button
          appearance="subtle"
          icon={<SignOutRegular />}
          className={styles.logoutButton}
          onClick={() => {
            logout();
            navigate('/login');
          }}
        >
          Logout
        </Button>
      </div>
      <div className={styles.chipRow}>
        {chips.map((chip, index) => (
          <button key={chip} className={`${styles.chip} ${index === 0 ? styles.chipActive : ''}`}>{chip}</button>
        ))}
      </div>
    </header>
  );
}

function statusBadgeClass(status: CompareRow['status']) {
  if (status === 'critical') return styles.badgeCritical;
  if (status === 'warn') return styles.badgeWarn;
  return styles.badgeGood;
}

function toPct(value: number): number {
  return Math.round(value * 10) / 10;
}

function getCauseFromStatus(status: string): string {
  if (status === 'Down') return 'Maintenance';
  if (status === 'Slow') return 'Material';
  if (status === 'Idle') return 'Staffing';
  return 'Flow';
}

function getCompareStatus(oeePct: number, downPct: number): CompareRow['status'] {
  if (oeePct < 75 || downPct > 20) return 'critical';
  if (oeePct < 85 || downPct > 12) return 'warn';
  return 'good';
}

function getQualityCompareStatus(fpyPct: number): CompareRow['status'] {
  if (fpyPct < 90) return 'critical';
  if (fpyPct < 93) return 'warn';
  return 'good';
}

function formatRelativeSubmitted(iso: string): string {
  const ts = Date.parse(iso);
  if (!Number.isFinite(ts)) return 'Submitted recently';
  const mins = Math.max(1, Math.round((Date.now() - ts) / 60000));
  if (mins < 60) return `Submitted ${mins}m ago`;
  const hrs = Math.round(mins / 60);
  if (hrs < 24) return `Submitted ${hrs}h ago`;
  const days = Math.round(hrs / 24);
  return `Submitted ${days}d ago`;
}

function mapIssueToHotIssue(issue: IssueRequestDto): IssueRow {
  return {
    title: issue.title,
    detail: `${issue.submittedByName} - ${formatRelativeSubmitted(issue.submittedAt)}`,
    severity: issue.type === 0 ? 'critical' : 'high',
  };
}

function buildQualityKpis(fpyValues: number[], openCapaCount: number): QualityKpis {
  const fpyPct = fpyValues.length > 0
    ? toPct(fpyValues.reduce((sum, value) => sum + value, 0) / fpyValues.length)
    : 0;
  const defectPpm = Math.max(Math.round((100 - fpyPct) * 180), 0);
  const scrapPct = toPct((100 - fpyPct) / 2.1);
  return {
    fpyPct,
    fpyDeltaPct: toPct(fpyPct - 91.2),
    defectPpm,
    defectDeltaPpm: Math.max(Math.round(defectPpm - 1330), 0),
    scrapPct,
    scrapDeltaPct: toPct(Math.max(scrapPct - 3.3, 0)),
    openCapaCount,
    openCapaDelta: Math.max(openCapaCount - 7, 0),
  };
}

function buildOpsKpis(snapshots: DigitalTwinSnapshot[], totalSessions: number): OpsKpis {
  const throughputUnits = snapshots.reduce((sum, s) => sum + (s.throughput?.unitsToday ?? 0), 0);
  const throughputDeltaRaw = snapshots.reduce((sum, s) => sum + (s.throughput?.unitsDelta ?? 0), 0);
  const throughputDeltaPct = throughputUnits > 0 ? toPct((throughputDeltaRaw / throughputUnits) * 100) : 0;

  const stations = snapshots.flatMap((s) => s.stations ?? []);
  const downCount = stations.filter((st) => st.status === 'Down').length;
  const downtimePct = stations.length > 0 ? toPct((downCount / stations.length) * 100) : 0;

  const efficiencyValues = snapshots
    .map((s) => s.lineEfficiencyPercent ?? 0)
    .filter((v) => Number.isFinite(v) && v > 0);
  const oeePct = efficiencyValues.length > 0
    ? toPct(efficiencyValues.reduce((sum, value) => sum + value, 0) / efficiencyValues.length)
    : 0;

  const laborTarget = Math.max(stations.length * 2, 1);
  const laborUtilizationPct = toPct(Math.min((totalSessions / laborTarget) * 100, 100));

  return {
    throughputUnits,
    throughputDeltaPct,
    downtimePct,
    downtimeDeltaPct: toPct(Math.max(downtimePct - 10, 0)),
    laborUtilizationPct,
    oeePct,
    oeeDeltaPct: toPct(oeePct >= 1.5 ? 1.5 : oeePct),
  };
}

function buildBottlenecks(snapshots: DigitalTwinSnapshot[], lineNames: Map<string, string>): OpsBottleneck[] {
  const candidateStations = snapshots
    .flatMap((snapshot) => snapshot.stations ?? [])
    .filter((station) => station.isBottleneck || station.status === 'Down' || station.status === 'Slow')
    .sort((a, b) => (b.avgCycleTimeMinutes ?? 0) - (a.avgCycleTimeMinutes ?? 0));

  return candidateStations.slice(0, 6).map((station) => {
    const lineName = lineNames.get(station.workCenterId) ?? station.name;
    const impactMinutes = Math.max(Math.round(station.avgCycleTimeMinutes ?? 0), 1);
    return {
      line: lineName,
      impact: `${impactMinutes} min`,
      cause: getCauseFromStatus(station.status),
    };
  });
}

function useOperationsPortfolioData() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [kpis, setKpis] = useState<OpsKpis | null>(null);
  const [bottlenecks, setBottlenecks] = useState<OpsBottleneck[]>([]);
  const [compareRows, setCompareRows] = useState<CompareRow[]>([]);
  const [updatedLabel, setUpdatedLabel] = useState('Loading live data...');

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const plants = await adminPlantGearApi.getAll();
        const selectedPlants = plants.slice(0, 3);
        if (selectedPlants.length === 0) {
          throw new Error('No plants available');
        }

        const lineNames = new Map<string, string>();
        const plantRows = await Promise.all(selectedPlants.map(async (plant: PlantWithGear) => {
          const [lines, sessions] = await Promise.all([
            productionLineApi.getProductionLines(plant.plantId).catch(() => [] as ProductionLine[]),
            activeSessionApi.getBySite(plant.plantId).catch(() => []),
          ]);

          const lineSnapshots = await Promise.all(
            lines.slice(0, 4).map(async (line) => {
              try {
                const snapshot = await digitalTwinApi.getSnapshot(line.id, plant.plantId);
                for (const station of snapshot.stations ?? []) {
                  lineNames.set(station.workCenterId, line.name);
                }
                return snapshot;
              } catch {
                return null;
              }
            }),
          );

          const snapshots = lineSnapshots.filter((s): s is DigitalTwinSnapshot => s !== null);
          const stationCount = snapshots.reduce((sum, s) => sum + (s.stations?.length ?? 0), 0);
          const downCount = snapshots.reduce((sum, s) => sum + (s.stations?.filter((st) => st.status === 'Down').length ?? 0), 0);
          const downPct = stationCount > 0 ? toPct((downCount / stationCount) * 100) : 0;
          const efficiencyValues = snapshots
            .map((s) => s.lineEfficiencyPercent ?? 0)
            .filter((v) => Number.isFinite(v) && v > 0);
          const oeePct = efficiencyValues.length > 0
            ? toPct(efficiencyValues.reduce((sum, v) => sum + v, 0) / efficiencyValues.length)
            : 0;

          return {
            snapshots,
            sessionsCount: sessions.length,
            compare: {
              plant: plant.plantName,
              summary: `OEE ${oeePct}% / DT ${downPct}%`,
              status: getCompareStatus(oeePct, downPct),
            } as CompareRow,
          };
        }));

        const allSnapshots = plantRows.flatMap((r) => r.snapshots);
        const totalSessions = plantRows.reduce((sum, r) => sum + r.sessionsCount, 0);

        if (!cancelled) {
          setKpis(buildOpsKpis(allSnapshots, totalSessions));
          const dynamicBottlenecks = buildBottlenecks(allSnapshots, lineNames);
          setBottlenecks(dynamicBottlenecks);
          setCompareRows(plantRows.map((r) => r.compare));
          setUpdatedLabel(`Updated ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Unable to load operations portfolio');
          setKpis(null);
          setBottlenecks([]);
          setCompareRows([]);
          setUpdatedLabel('Live update unavailable');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  return { loading, error, kpis, bottlenecks, compareRows, updatedLabel };
}

function useQualityPortfolioData() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [kpis, setKpis] = useState<QualityKpis | null>(null);
  const [compareRows, setCompareRows] = useState<CompareRow[]>([]);
  const [hotIssues, setHotIssues] = useState<IssueRow[]>([]);
  const [updatedLabel, setUpdatedLabel] = useState('Loading live data...');

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        setLoading(true);
        setError(null);

        const [plants, pendingIssues] = await Promise.all([
          adminPlantGearApi.getAll(),
          issueRequestApi.getPending().catch(() => [] as IssueRequestDto[]),
        ]);

        const selectedPlants = plants.slice(0, 3);
        if (selectedPlants.length === 0) {
          throw new Error('No plants available');
        }

        const plantRows = await Promise.all(selectedPlants.map(async (plant: PlantWithGear) => {
          const lines = await productionLineApi.getProductionLines(plant.plantId).catch(() => [] as ProductionLine[]);
          const lineSnapshots = await Promise.all(
            lines.slice(0, 4).map(async (line) => {
              try {
                return await digitalTwinApi.getSnapshot(line.id, plant.plantId);
              } catch {
                return null;
              }
            }),
          );
          const snapshots = lineSnapshots.filter((s): s is DigitalTwinSnapshot => s !== null);
          const fpyValues = snapshots
            .flatMap((snapshot) => snapshot.stations ?? [])
            .map((station) => station.firstPassYieldPercent)
            .filter((value): value is number => typeof value === 'number' && Number.isFinite(value));
          const fpyPct = fpyValues.length > 0
            ? toPct(fpyValues.reduce((sum, value) => sum + value, 0) / fpyValues.length)
            : 0;
          return {
            compare: {
              plant: plant.plantName,
              summary: `FPY ${fpyPct}%`,
              status: getQualityCompareStatus(fpyPct),
            } as CompareRow,
            fpyValues,
          };
        }));

        const allFpyValues = plantRows.flatMap((row) => row.fpyValues);
        if (!cancelled) {
          setKpis(buildQualityKpis(allFpyValues, pendingIssues.length));
          setCompareRows(plantRows.map((row) => row.compare));
          setHotIssues(pendingIssues.slice(0, 3).map(mapIssueToHotIssue));
          setUpdatedLabel(`Updated ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Unable to load quality portfolio');
          setKpis(null);
          setCompareRows([]);
          setHotIssues([]);
          setUpdatedLabel('Live update unavailable');
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  return { loading, error, kpis, compareRows, hotIssues, updatedLabel };
}

function MobileTabBar({ tabs }: { tabs: MobileTab[] }) {
  const navigate = useNavigate();
  const location = useLocation();
  return (
    <nav className={styles.tabBar} aria-label="Mobile navigation">
      {tabs.map((tab) => (
        <button
          key={tab.path}
          className={`${styles.tabButton} ${location.pathname === tab.path ? styles.tabButtonActive : ''}`}
          onClick={() => navigate(tab.path)}
        >
          {tab.label}
        </button>
      ))}
    </nav>
  );
}

function MobileFrame({ title, chips, tabs, children }: { title: string; chips: string[]; tabs?: MobileTab[]; children: React.ReactNode }) {
  return (
    <div className={styles.shell}>
      <ScopeHeader title={title} chips={chips} />
      <main className={styles.content}>{children}</main>
      {tabs ? <MobileTabBar tabs={tabs} /> : null}
    </div>
  );
}

export function MobileSupervisorHubScreen() {
  const navigate = useNavigate();
  return (
    <MobileFrame title="Supervisor Hub" chips={['All Plants', 'Last 24h', 'All Shifts']} tabs={qualityTabs}>
      <h2 className={styles.sectionTitle}>Quick Actions</h2>
      <div className={styles.cardGrid}>
        <Button onClick={() => navigate('/mobile/alerts')}>Alerts & Exceptions</Button>
        <Button onClick={() => navigate('/mobile/approvals')}>Approvals</Button>
        <Button onClick={() => navigate('/mobile/whos-on-floor')}>Who&apos;s On The Floor</Button>
        <Button onClick={() => navigate('/mobile/lookup')}>Quick Lookup</Button>
      </div>
      <h2 className={styles.sectionTitle}>Live Status</h2>
      <section className={styles.list}>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Line 1</div><div className={styles.rowMeta}>Running</div></div><span className={styles.badgeGood}>ONLINE</span></div>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Line 2</div><div className={styles.rowMeta}>Downtime (Fault)</div></div><span className={styles.badgeCritical}>DOWN</span></div>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Line 3</div><div className={styles.rowMeta}>Starved (Material)</div></div><span className={styles.badgeWarn}>RISK</span></div>
      </section>
    </MobileFrame>
  );
}

export function MobileQualityDirectorScreen() {
  const { loading, error, kpis, compareRows, hotIssues, updatedLabel } = useQualityPortfolioData();

  return (
    <MobileFrame title="Quality Portfolio" chips={['All Plants', 'Last 24h', 'Shift A']} tabs={qualityTabs}>
      {error ? <div className={styles.emptyState}>Live quality data unavailable: {error}</div> : null}
      <h2 className={styles.sectionTitle}>Portfolio KPIs</h2>
      <section className={styles.cardGrid}>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>FPY</span><span className={styles.cardTarget}>Target: 94.0%</span><span className={styles.cardValue}>{kpis ? `${kpis.fpyPct}%` : '--'}</span><span className={styles.cardDeltaGood}>{kpis ? `${kpis.fpyDeltaPct >= 0 ? '+' : ''}${kpis.fpyDeltaPct}%` : (loading ? 'Loading...' : '--')}</span></article>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>Defect PPM</span><span className={styles.cardTarget}>Target: &lt;1000</span><span className={styles.cardValue}>{kpis?.defectPpm ?? '--'}</span><span className={styles.cardDeltaBad}>{kpis ? `+${kpis.defectDeltaPpm} PPM` : (loading ? 'Loading...' : '--')}</span></article>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>Scrap %</span><span className={styles.cardTarget}>Target: 2.5%</span><span className={styles.cardValue}>{kpis ? `${kpis.scrapPct}%` : '--'}</span><span className={styles.cardDeltaBad}>{kpis ? `+${kpis.scrapDeltaPct}%` : (loading ? 'Loading...' : '--')}</span></article>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>Open CAPA</span><span className={styles.cardTarget}>Critical: 7</span><span className={styles.cardValue}>{kpis?.openCapaCount ?? '--'}</span><span className={styles.cardDeltaBad}>{kpis ? `+${kpis.openCapaDelta}` : (loading ? 'Loading...' : '--')}</span></article>
      </section>
      <h2 className={styles.sectionTitle}>Cross-Plant Compare</h2>
      <section className={styles.list}>
        {compareRows.map((row, index) => (
          <div key={row.plant} className={styles.listRow}><div><div className={styles.rowTitle}>{index + 1}. {row.plant}</div><div className={styles.rowMeta}>{row.summary}</div></div><span className={statusBadgeClass(row.status)}>{row.status.toUpperCase()}</span></div>
        ))}
      </section>
      <h2 className={styles.sectionTitle}>Hot Issues</h2>
      <section className={styles.list}>
        {hotIssues.map((issue) => (
          <div key={issue.title} className={styles.listRow}><div><div className={styles.rowTitle}>{issue.title}</div><div className={styles.rowMeta}>{issue.detail}</div></div><span className={issue.severity === 'critical' ? styles.badgeCritical : styles.badgeHigh}>{issue.severity.toUpperCase()}</span></div>
        ))}
      </section>
      <div className={styles.actionRow}><Button appearance="primary">Assign Investigation</Button><Button>Approve Disposition</Button></div>
      <div className={styles.footerNote}>{updatedLabel}</div>
    </MobileFrame>
  );
}

export function MobileOpsDirectorScreen() {
  const { loading, error, kpis, bottlenecks, compareRows, updatedLabel } = useOperationsPortfolioData();

  return (
    <MobileFrame title="Operations Portfolio" chips={['3 Plants', 'Today', 'All Shifts']} tabs={opsTabs}>
      {error ? <div className={styles.emptyState}>Live operations data unavailable: {error}</div> : null}
      <h2 className={styles.sectionTitle}>Executive KPIs</h2>
      <section className={styles.cardGrid}>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>Throughput vs Plan</span><span className={styles.cardTarget}>Units</span><span className={styles.cardValue}>{kpis?.throughputUnits ?? '--'}</span><span className={styles.cardDeltaGood}>{kpis ? `${kpis.throughputDeltaPct >= 0 ? '+' : ''}${kpis.throughputDeltaPct}%` : (loading ? 'Loading...' : '--')}</span></article>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>Downtime %</span><span className={styles.cardTarget}>Digital twin status</span><span className={styles.cardValue}>{kpis ? `${kpis.downtimePct}%` : '--'}</span><span className={styles.cardDeltaBad}>{kpis ? `+${kpis.downtimeDeltaPct}%` : (loading ? 'Loading...' : '--')}</span></article>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>Labor Utilization</span><span className={styles.cardTarget}>Active sessions</span><span className={styles.cardValue}>{kpis ? `${kpis.laborUtilizationPct}%` : '--'}</span><span className={styles.cardDeltaGood}>{loading ? 'Loading...' : 'Live'}</span></article>
        <article className={styles.kpiCard}><span className={styles.cardLabel}>OEE Trend</span><span className={styles.cardTarget}>Line efficiency average</span><span className={styles.cardValue}>{kpis ? `${kpis.oeePct}%` : '--'}</span><span className={styles.cardDeltaGood}>{kpis ? `+${kpis.oeeDeltaPct}%` : (loading ? 'Loading...' : '--')}</span></article>
      </section>
      <h2 className={styles.sectionTitle}>Top Bottlenecks</h2>
      <section className={styles.list}>
        {bottlenecks.map((item) => (
          <div key={item.line} className={styles.listRow}><div><div className={styles.rowTitle}>{item.line}</div><div className={styles.rowMeta}>Impact: {item.impact} - Cause: {item.cause}</div></div><span className={styles.badgeHigh}>OPEN</span></div>
        ))}
      </section>
      <h2 className={styles.sectionTitle}>Across-Plant Compare</h2>
      <section className={styles.list}>
        {compareRows.map((row, index) => (
          <div key={row.plant} className={styles.listRow}><div><div className={styles.rowTitle}>{index + 1}. {row.plant}</div><div className={styles.rowMeta}>{row.summary}</div></div><span className={statusBadgeClass(row.status)}>{row.status.toUpperCase()}</span></div>
        ))}
      </section>
      <div className={styles.actionRow}><Button appearance="primary">Escalate</Button><Button>Message Plant Lead</Button></div>
      <div className={styles.footerNote}>{updatedLabel}</div>
    </MobileFrame>
  );
}

export function MobileAlertsScreen() {
  const rows = useMemo(() => [...qualityIssues, { title: 'Downtime spike in Line B', detail: 'Awaiting response from shift lead', severity: 'high' as const }], []);
  return (
    <MobileFrame title="Alerts & Exceptions" chips={['Priority', 'Last 8h', 'All Plants']} tabs={qualityTabs}>
      <section className={styles.list}>
        {rows.map((issue) => (
          <div key={issue.title} className={styles.listRow}><div><div className={styles.rowTitle}>{issue.title}</div><div className={styles.rowMeta}>{issue.detail}</div></div><span className={issue.severity === 'critical' ? styles.badgeCritical : styles.badgeHigh}>{issue.severity.toUpperCase()}</span></div>
        ))}
      </section>
    </MobileFrame>
  );
}

export function MobileApprovalsScreen() {
  return (
    <MobileFrame title="Approvals" chips={['Pending', 'All Plants', 'Today']} tabs={qualityTabs}>
      <section className={styles.list}>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Containment approval - Oven Temp</div><div className={styles.rowMeta}>Owner: M. Davis</div></div><span className={styles.badgeHigh}>PENDING</span></div>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Issue request #4521</div><div className={styles.rowMeta}>Owner: J. Smith</div></div><span className={styles.badgeWarn}>REVIEW</span></div>
      </section>
      <div className={styles.actionRow}><Button appearance="primary">Approve Selected</Button><Button>Reject</Button></div>
    </MobileFrame>
  );
}

export function MobileLookupScreen() {
  const [query, setQuery] = useState('');
  return (
    <MobileFrame title="Quick Lookup" chips={['Serial', 'Product', 'Vendor']} tabs={qualityTabs}>
      <Input value={query} onChange={(_, data) => setQuery(data.value)} placeholder="Scan or enter serial/product/vendor..." className={styles.lookupInput} />
      <section className={styles.list}>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Result preview</div><div className={styles.rowMeta}>{query ? `Searching for "${query}"` : 'Enter a value to query'}</div></div><span className={styles.badgeGood}>READY</span></div>
      </section>
    </MobileFrame>
  );
}

export function MobileWhosOnFloorScreen() {
  return (
    <MobileFrame title="Who's On The Floor" chips={['Current Shift', 'All Plants', 'Role']} tabs={qualityTabs}>
      <section className={styles.list}>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Line 1 - Final Assembly</div><div className={styles.rowMeta}>9 operators | 4 welders</div></div><span className={styles.badgeGood}>STAFFED</span></div>
        <div className={styles.listRow}><div><div className={styles.rowTitle}>Line 2 - Final Assembly</div><div className={styles.rowMeta}>6 operators | 2 welders</div></div><span className={styles.badgeWarn}>THIN</span></div>
      </section>
    </MobileFrame>
  );
}

export function MobileMoreScreen() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const qualityDirector = isQualityDirector(user);
  const opsDirector = isOpsDirector(user);

  const quickLinks = qualityDirector
    ? [
        { label: 'Quality Portfolio', path: '/mobile/quality-portfolio' },
        { label: 'Cross-Plant Compare', path: '/mobile/quality-plants' },
        { label: 'Alerts & Exceptions', path: '/mobile/alerts' },
        { label: 'Approvals', path: '/mobile/approvals' },
      ]
    : opsDirector
      ? [
          { label: 'Operations Portfolio', path: '/mobile/operations-portfolio' },
          { label: 'Top Bottlenecks', path: '/mobile/bottlenecks' },
          { label: 'Across-Plant Compare', path: '/mobile/plants' },
          { label: 'Quick Actions', path: '/mobile/actions' },
        ]
      : [];

  return (
    <MobileFrame title="More" chips={['Role Views', 'Shortcuts', 'Portfolio']} tabs={opsDirector ? opsTabs : qualityTabs}>
      <div className={styles.cardGrid}>
        {quickLinks.map((link) => (
          <Button key={link.path} onClick={() => navigate(link.path)}>{link.label}</Button>
        ))}
      </div>
      {quickLinks.length === 0 ? <div className={styles.emptyState}>No director shortcuts available for this role.</div> : null}
    </MobileFrame>
  );
}

export function MobileBottlenecksScreen() {
  const { bottlenecks, updatedLabel } = useOperationsPortfolioData();
  return (
    <MobileFrame title="Top Bottlenecks" chips={['Today', 'All Plants', 'Impact']} tabs={opsTabs}>
      <section className={styles.list}>
        {bottlenecks.map((item) => (
          <div key={item.line} className={styles.listRow}><div><div className={styles.rowTitle}>{item.line}</div><div className={styles.rowMeta}>Impact: {item.impact} - Cause: {item.cause}</div></div><span className={styles.badgeHigh}>OPEN</span></div>
        ))}
      </section>
      <div className={styles.footerNote}>{updatedLabel}</div>
    </MobileFrame>
  );
}

export function MobilePlantsScreen() {
  const { compareRows, updatedLabel } = useOperationsPortfolioData();
  return (
    <MobileFrame title="Across-Plant Compare" chips={['3 Plants', 'Today', 'OEE']} tabs={opsTabs}>
      <section className={styles.list}>
        {compareRows.map((row) => (
          <div key={row.plant} className={styles.listRow}><div><div className={styles.rowTitle}>{row.plant}</div><div className={styles.rowMeta}>{row.summary}</div></div><span className={statusBadgeClass(row.status)}>{row.status.toUpperCase()}</span></div>
        ))}
      </section>
      <div className={styles.footerNote}>{updatedLabel}</div>
    </MobileFrame>
  );
}

export function MobileQuickActionsScreen() {
  return (
    <MobileFrame title="Quick Actions" chips={['Escalations', 'Messaging', 'Approvals']} tabs={opsTabs}>
      <div className={styles.actionRow}><Button appearance="primary">Escalate Incident</Button><Button>Message Plant Lead</Button></div>
      <div className={styles.actionRow}><Button appearance="primary">Approve Override</Button><Button>Open Action Log</Button></div>
    </MobileFrame>
  );
}

export function MobileQualityPlantsScreen() {
  const { compareRows, updatedLabel } = useQualityPortfolioData();
  return (
    <MobileFrame title="Quality Cross-Plant Compare" chips={['All Plants', 'Last 24h', 'FPY']} tabs={qualityTabs}>
      <section className={styles.list}>
        {compareRows.map((row, index) => (
          <div key={row.plant} className={styles.listRow}><div><div className={styles.rowTitle}>{index + 1}. {row.plant}</div><div className={styles.rowMeta}>{row.summary}</div></div><span className={statusBadgeClass(row.status)}>{row.status.toUpperCase()}</span></div>
        ))}
      </section>
      <div className={styles.footerNote}>{updatedLabel}</div>
    </MobileFrame>
  );
}

export function MobileOperatorQuickActionsScreen() {
  return (
    <MobileFrame title="Operator Quick Actions" chips={['Current WC', 'Manual Mode', 'Status']}>
      <h2 className={styles.sectionTitle}>Quick Actions</h2>
      <div className={styles.cardGrid}>
        <Button appearance="primary">Start Job</Button>
        <Button>Pause Job</Button>
        <Button>Log Defect</Button>
        <Button>Request Team Lead</Button>
      </div>
      <div className={styles.emptyState}>
        Full kiosk operator control remains tablet-first. This phone view is intentionally limited to
        quick actions and status handoff.
      </div>
    </MobileFrame>
  );
}
