import { useState, useCallback, useEffect, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  Button,
  Input,
  Select,
  Spinner,
} from '@fluentui/react-components';
import {
  SearchRegular,
  CheckmarkCircleFilled,
  DismissCircleFilled,
  SubtractCircleFilled,
} from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, serialNumberApi } from '../../api/endpoints.ts';
import type { Plant, TraceabilityNode, ManufacturingEvent, SerialNumberLookup } from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './SerialNumberLookupScreen.module.css';

const NODE_TYPE_COLORS: Record<string, { bg: string; label: string }> = {
  sellable:  { bg: '#28a745', label: 'Finished SN' },
  assembled: { bg: '#606ca3', label: 'Fitup' },
  shell:     { bg: '#e41e2f', label: 'Shells' },
  plate:     { bg: '#ffc107', label: 'Plate' },
  leftHead:  { bg: '#ba68c8', label: 'Heads' },
  rightHead: { bg: '#ba68c8', label: 'Heads' },
  valve:     { bg: '#17a2b8', label: 'Valves' },
};

function getCardColorClass(nodeType: string): string {
  switch (nodeType) {
    case 'sellable': return styles.cardSellable;
    case 'assembled': return styles.cardAssembled;
    case 'shell': return styles.cardShell;
    case 'plate': return styles.cardPlate;
    case 'leftHead':
    case 'rightHead': return styles.cardHead;
    case 'valve': return styles.cardValve;
    default: return styles.cardDefault;
  }
}

function getNodeTypeLabel(nodeType: string): string {
  return NODE_TYPE_COLORS[nodeType]?.label ?? nodeType;
}

interface FlowStep {
  node: TraceabilityNode;
  subComponents: TraceabilityNode[];
}

function flattenToFlow(treeNodes: TraceabilityNode[]): FlowStep[] {
  if (treeNodes.length === 0) return [];
  const root = treeNodes[0];
  const steps: FlowStep[] = [];

  function walk(node: TraceabilityNode) {
    const mainTypes = new Set(['sellable', 'assembled', 'shell']);
    const children = node.children ?? [];
    const mainChildren = children.filter(c => mainTypes.has(c.nodeType));
    const subChildren = children.filter(c => !mainTypes.has(c.nodeType));

    if (mainChildren.length > 0) {
      for (const child of mainChildren) walk(child);
      steps.push({ node, subComponents: subChildren });
    } else {
      const ownSubs = children.filter(c => c.nodeType === 'plate' || c.nodeType === 'leftHead' || c.nodeType === 'rightHead' || c.nodeType === 'valve');
      steps.push({ node, subComponents: ownSubs });
    }
  }

  walk(root);
  return steps;
}

interface EventWithSource {
  event: ManufacturingEvent;
  nodeSerial: string;
  nodeType: string;
}

function collectAllEvents(treeNodes: TraceabilityNode[]): EventWithSource[] {
  const results: EventWithSource[] = [];

  function walk(node: TraceabilityNode) {
    for (const evt of node.events ?? []) {
      results.push({ event: evt, nodeSerial: node.serial || node.label, nodeType: node.nodeType });
    }
    for (const child of node.children ?? []) walk(child);
  }

  for (const root of treeNodes) walk(root);
  results.sort((a, b) => new Date(b.event.timestamp).getTime() - new Date(a.event.timestamp).getTime());
  return results;
}

function Legend() {
  const shown = new Set<string>();
  const items: { bg: string; label: string }[] = [];
  for (const [key, val] of Object.entries(NODE_TYPE_COLORS)) {
    if (key === 'rightHead') continue;
    if (shown.has(val.label)) continue;
    shown.add(val.label);
    items.push(val);
  }

  return (
    <div className={styles.legendRow}>
      <div className={styles.legend} data-testid="tree-legend">
        <strong>Diagram Key</strong>
        {items.map((item) => (
          <span key={item.label} className={styles.legendItem}>
            <span className={styles.legendDot} style={{ background: item.bg }} />
            {item.label}
          </span>
        ))}
      </div>
      <div className={`${styles.legend} ${styles.legendGate}`} data-testid="gate-legend">
        <strong>Gate Check</strong>
        <span className={styles.legendItem}>
          <CheckmarkCircleFilled className={styles.gatePass} />
          Passed
        </span>
        <span className={styles.legendItem}>
          <DismissCircleFilled className={styles.gateFail} />
          Rejected
        </span>
        <span className={styles.legendItem}>
          <SubtractCircleFilled className={styles.gateNone} />
          No Record
        </span>
      </div>
    </div>
  );
}

type GateStatus = 'pass' | 'fail' | 'none';

function deriveGateStatus(node: TraceabilityNode): GateStatus | null {
  const { nodeType } = node;
  if (nodeType !== 'shell' && nodeType !== 'assembled' && nodeType !== 'sellable') return null;

  const events = node.events ?? [];

  const matches = (e: ManufacturingEvent, ...keywords: string[]) => {
    const s = `${e.type} ${e.workCenterName}`.toLowerCase();
    return keywords.some(k => s.includes(k));
  };

  let gateEvents: ManufacturingEvent[];
  if (nodeType === 'shell') {
    gateEvents = events.filter(e =>
      e.inspectionResult != null && !matches(e, 'queue') &&
      (matches(e, 'rt', 'realtime', 'real-time') ||
       (matches(e, 'x-ray', 'xray') && !matches(e, 'spot'))));
  } else if (nodeType === 'assembled') {
    gateEvents = events.filter(e =>
      e.inspectionResult != null && matches(e, 'spot'));
  } else {
    gateEvents = events.filter(e =>
      e.inspectionResult != null && matches(e, 'hydro'));
  }

  if (gateEvents.length === 0) return 'none';

  const hasReject = gateEvents.some(e => {
    const r = e.inspectionResult!.toLowerCase();
    return r.includes('reject') || r.includes('fail') || r === 'nogo';
  });
  return hasReject ? 'fail' : 'pass';
}

function formatCardTitle(node: TraceabilityNode): string {
  const serial = node.serial || node.label;
  if (node.nodeType === 'assembled' && node.childSerials && node.childSerials.length > 0) {
    return `${serial} (${node.childSerials.join(', ')})`;
  }
  return serial;
}

function HeroCard({
  node,
  isSmall,
}: {
  node: TraceabilityNode;
  isSmall?: boolean;
}) {
  const colorClass = getCardColorClass(node.nodeType);
  const badgeColor = NODE_TYPE_COLORS[node.nodeType]?.bg;
  const gateStatus = deriveGateStatus(node);
  const defects = node.defectCount ?? 0;
  const annotations = node.annotationCount ?? 0;

  const cardClasses = [
    styles.heroCard,
    colorClass,
    isSmall ? styles.heroCardSmall : '',
  ].filter(Boolean).join(' ');

  return (
    <div
      className={cardClasses}
      data-testid={`hero-card-${node.id}`}
    >
      {gateStatus != null && (
        <span className={styles.gateIcon} data-testid={`gate-${node.id}`}>
          {gateStatus === 'pass'
            ? <CheckmarkCircleFilled className={styles.gatePass} />
            : gateStatus === 'fail'
            ? <DismissCircleFilled className={styles.gateFail} />
            : <SubtractCircleFilled className={styles.gateNone} />}
        </span>
      )}
      <div className={styles.cardTypeBadge} style={{ background: badgeColor ? `${badgeColor}18` : undefined, color: badgeColor }}>
        {getNodeTypeLabel(node.nodeType)}
      </div>
      <div className={styles.cardSerial} title={formatCardTitle(node)}>{formatCardTitle(node)}</div>
      <div className={styles.cardInfo}>
        {node.tankSize != null && <span title={`${node.tankSize} gal`}>{node.tankSize} gal</span>}
        {node.heatNumber && <span title={`Heat: ${node.heatNumber}`}>Heat: {node.heatNumber}</span>}
      </div>
      <div className={styles.cardFooter}>
        <div className={styles.cardStats}>
          <span className={styles.statItem} title="Defects">
            <span className={styles.statLabel}>Defects</span>
            <span className={defects > 0 ? styles.statBad : styles.statGood}>{defects}</span>
          </span>
          <span className={styles.statItem} title="Annotations">
            <span className={styles.statLabel}>Notes</span>
            <span className={styles.statNeutral}>{annotations}</span>
          </span>
        </div>
      </div>
    </div>
  );
}

function EventsPanel({ events }: { events: EventWithSource[] }) {
  return (
    <div className={styles.eventsPane} data-testid="events-panel">
      <div className={styles.eventsPaneTitle}>Manufacturing Events</div>
      {events.length === 0 ? (
        <div className={styles.noEventsText}>No manufacturing events recorded.</div>
      ) : (
        events.map((item, i) => {
          const badgeColor = NODE_TYPE_COLORS[item.nodeType]?.bg ?? '#6c757d';
          return (
            <div key={i} className={styles.eventRow}>
              <span className={styles.eventSerialBadge} style={{ background: `${badgeColor}18`, color: badgeColor }}>
                {item.nodeSerial}
              </span>
              <div className={styles.eventDetails}>
                <div className={styles.eventPrimary}>
                  {item.event.workCenterName} — {item.event.type}
                </div>
                <div className={styles.eventSecondary}>
                  {formatDateTime(item.event.timestamp)}
                  {' · '}{item.event.completedBy}
                  {item.event.assetName && <> · {item.event.assetName}</>}
                  {item.event.inspectionResult && <> · Result: {item.event.inspectionResult}</>}
                </div>
              </div>
            </div>
          );
        })
      )}
    </div>
  );
}

export function SerialNumberLookupScreen() {
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const canChangeSite = (user?.roleTier ?? 99) <= 2;
  const autoLoaded = useRef(false);
  const cameFromSellable = searchParams.get('from') === 'sellable-status';

  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(user?.defaultSiteId ?? '');
  const [serial, setSerial] = useState(searchParams.get('serial') ?? '');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState<SerialNumberLookup | null>(null);

  useEffect(() => {
    if (canChangeSite) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [canChangeSite]);

  const handleLookup = useCallback(async (overrideSerial?: string) => {
    const trimmed = (overrideSerial ?? serial).trim();
    if (!trimmed) return;
    setLoading(true);
    setError('');
    setData(null);
    try {
      const result = await serialNumberApi.getLookup(trimmed);
      setData(result);
    } catch {
      setError('Serial number not found.');
    } finally {
      setLoading(false);
    }
  }, [serial]);

  useEffect(() => {
    const qsSerial = searchParams.get('serial');
    if (qsSerial && !autoLoaded.current) {
      autoLoaded.current = true;
      handleLookup(qsSerial);
    }
  }, [searchParams, handleLookup]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleLookup();
  };

  const flowSteps = data ? flattenToFlow(data.treeNodes) : [];
  const allEvents = data ? collectAllEvents(data.treeNodes) : [];

  return (
    <AdminLayout
      title="Serial Number Lookup"
      backLabel={cameFromSellable ? 'Back' : undefined}
      onBack={cameFromSellable ? () => navigate(-1) : undefined}
    >
      <div className={styles.controls}>
        {canChangeSite && (
          <div className={styles.fieldGroup}>
            <label className={styles.fieldLabel}>Site</label>
            <Select
              value={siteId}
              onChange={(_e, d) => setSiteId(d.value)}
            >
              {sites.map((s) => (
                <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
              ))}
              {sites.length === 0 && siteId && (
                <option value={siteId}>{user?.plantName ?? siteId}</option>
              )}
            </Select>
          </div>
        )}

        <div className={`${styles.fieldGroup} ${styles.serialInput}`}>
          <label className={styles.fieldLabel}>Serial Number</label>
          <Input
            value={serial}
            onChange={(_e, d) => setSerial(d.value)}
            onKeyDown={handleKeyDown}
            placeholder="Enter serial number..."
            size="medium"
          />
        </div>

        <Button
          appearance="primary"
          icon={<SearchRegular />}
          onClick={() => handleLookup()}
          disabled={loading || !serial.trim()}
          data-testid="lookup-go-btn"
        >
          Go
        </Button>
      </div>

      {error && <div className={styles.errorMsg}>{error}</div>}

      {(loading || data) && (
        <div className={styles.genealogyContainer}>
          {loading ? (
            <div className={styles.emptyState}>
              <Spinner size="medium" label="Looking up..." />
            </div>
          ) : flowSteps.length === 0 ? (
            <>
              <Legend />
              <div className={styles.emptyState}>No traceability data found.</div>
            </>
          ) : (
            <>
              <div className={styles.sectionTitle}>Production Genealogy</div>
              <Legend />
              <div className={styles.splitLayout}>
                <div className={styles.diagramPane}>
                  <div className={styles.flowRow} data-testid="genealogy-flow">
                    {flowSteps.map((step, i) => {
                      const isLastShell =
                        step.node.nodeType === 'shell' &&
                        i < flowSteps.length - 1 &&
                        flowSteps[i + 1].node.nodeType !== 'shell';
                      return (
                      <div
                        key={step.node.id}
                        className={`${styles.flowColumn} ${i < flowSteps.length - 1 ? styles.flowColumnWithArrow : ''}`}
                        data-node-type={step.node.nodeType}
                        {...(isLastShell ? { 'data-last-shell': '' } : {})}
                      >
                        <HeroCard node={step.node} />
                        {step.subComponents.length > 0 && (
                          <div className={styles.subComponentsArea}>
                            {step.subComponents.map((sub) => (
                              <div key={sub.id} className={styles.subItem}>
                                <div className={styles.subArrow}>
                                  <div className={styles.subArrowLine} />
                                </div>
                                <HeroCard node={sub} isSmall />
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                      );
                    })}
                  </div>
                </div>
                <EventsPanel events={allEvents} />
              </div>
            </>
          )}
        </div>
      )}
    </AdminLayout>
  );
}
