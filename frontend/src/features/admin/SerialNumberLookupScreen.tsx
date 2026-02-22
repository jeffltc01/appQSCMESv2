import { useState, useCallback, useEffect } from 'react';
import {
  Button,
  Input,
  Select,
  Spinner,
} from '@fluentui/react-components';
import {
  SearchRegular,
  ChevronDownRegular,
  ChevronUpRegular,
} from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, serialNumberApi } from '../../api/endpoints.ts';
import type { Plant, TraceabilityNode, ManufacturingEvent, SerialNumberLookup } from '../../types/domain.ts';
import { formatDateTime } from '../../utils/dateFormat.ts';
import styles from './SerialNumberLookupScreen.module.css';

const NODE_TYPE_COLORS: Record<string, { bg: string; label: string }> = {
  sellable:  { bg: '#28a745', label: 'Finished SN' },
  assembled: { bg: '#606ca3', label: 'Fitup (Alpha Code)' },
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
    <div className={styles.legend} data-testid="tree-legend">
      <strong>Diagram Key</strong>
      {items.map((item) => (
        <span key={item.label} className={styles.legendItem}>
          <span className={styles.legendDot} style={{ background: item.bg }} />
          {item.label}
        </span>
      ))}
    </div>
  );
}

function DetailPanel({ node, isOpen, isSmall }: { node: TraceabilityNode; isOpen: boolean; isSmall?: boolean }) {
  const events = node.events ?? [];
  const contentClass = [styles.detailContent, isSmall ? styles.detailContentSmall : ''].filter(Boolean).join(' ');

  return (
    <div className={`${styles.detailPanel} ${isOpen ? styles.detailPanelOpen : ''}`}>
      <div className={contentClass}>
        <div className={styles.detailGrid}>
          {node.productName && (
            <>
              <span className={styles.detailLabel}>Product</span>
              <span className={styles.detailValue}>{node.productName}</span>
            </>
          )}
          {node.tankSize != null && (
            <>
              <span className={styles.detailLabel}>Tank Size</span>
              <span className={styles.detailValue}>{node.tankSize} gal</span>
            </>
          )}
          {node.tankType && (
            <>
              <span className={styles.detailLabel}>Type</span>
              <span className={styles.detailValue}>{node.tankType}</span>
            </>
          )}
          {node.vendorName && (
            <>
              <span className={styles.detailLabel}>Vendor</span>
              <span className={styles.detailValue}>{node.vendorName}</span>
            </>
          )}
          {node.heatNumber && (
            <>
              <span className={styles.detailLabel}>Heat #</span>
              <span className={styles.detailValue}>{node.heatNumber}</span>
            </>
          )}
          {node.coilNumber && (
            <>
              <span className={styles.detailLabel}>Coil #</span>
              <span className={styles.detailValue}>{node.coilNumber}</span>
            </>
          )}
          {node.lotNumber && (
            <>
              <span className={styles.detailLabel}>Lot #</span>
              <span className={styles.detailValue}>{node.lotNumber}</span>
            </>
          )}
          {node.createdAt && (
            <>
              <span className={styles.detailLabel}>Created</span>
              <span className={styles.detailValue}>{formatDateTime(node.createdAt)}</span>
            </>
          )}
        </div>

        <div className={styles.eventsSection}>
          <div className={styles.eventsSectionTitle}>Manufacturing Events</div>
          {events.length === 0 ? (
            <div className={styles.noEventsText}>No manufacturing events.</div>
          ) : (
            <table className={styles.eventsTable} data-testid={`events-table-${node.id}`}>
              <thead>
                <tr>
                  <th>Date/Time</th>
                  <th>Workcenter</th>
                  <th>Type</th>
                  <th>Completed By</th>
                  <th>Asset</th>
                  <th>Result</th>
                </tr>
              </thead>
              <tbody>
                {events.map((evt: ManufacturingEvent, i: number) => (
                  <tr key={i}>
                    <td>{formatDateTime(evt.timestamp)}</td>
                    <td>{evt.workCenterName}</td>
                    <td>{evt.type}</td>
                    <td>{evt.completedBy}</td>
                    <td>{evt.assetName ?? '—'}</td>
                    <td>{evt.inspectionResult ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}

function HeroCard({
  node,
  isExpanded,
  onToggle,
  isSmall,
}: {
  node: TraceabilityNode;
  isExpanded: boolean;
  onToggle: () => void;
  isSmall?: boolean;
}) {
  const colorClass = getCardColorClass(node.nodeType);
  const badgeColor = NODE_TYPE_COLORS[node.nodeType]?.bg;
  const eventCount = (node.events ?? []).length;
  const cardClasses = [
    styles.heroCard,
    colorClass,
    isSmall ? styles.heroCardSmall : '',
    isExpanded ? styles.heroCardExpanded : '',
  ].filter(Boolean).join(' ');

  const badgeStyle = isSmall
    ? { background: badgeColor ? `${badgeColor}18` : undefined, color: badgeColor }
    : { background: badgeColor };

  return (
    <div>
      <div
        className={cardClasses}
        onClick={onToggle}
        data-testid={`hero-card-${node.id}`}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') onToggle(); }}
      >
        <div className={styles.cardTypeBadge} style={badgeStyle}>{getNodeTypeLabel(node.nodeType)}</div>
        <div className={styles.cardSerial}>{node.serial || node.label}</div>
        <div className={styles.cardInfo}>
          {node.tankSize != null && <span>{node.tankSize} gal</span>}
          {node.tankType && <span>{node.tankType}</span>}
          {node.heatNumber && !node.tankType && <span>Heat: {node.heatNumber}</span>}
        </div>
        <div className={`${styles.cardEventBadge} ${eventCount === 0 ? styles.noEvents : ''}`}>
          {eventCount > 0 ? `${eventCount} event${eventCount > 1 ? 's' : ''}` : 'No events'}
        </div>
        <div className={styles.cardExpandHint}>
          {isExpanded ? <ChevronUpRegular /> : <ChevronDownRegular />}
        </div>
      </div>
      <DetailPanel node={node} isOpen={isExpanded} isSmall={isSmall} />
    </div>
  );
}

export function SerialNumberLookupScreen() {
  const { user } = useAuth();
  const canChangeSite = (user?.roleTier ?? 99) <= 2;

  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(user?.defaultSiteId ?? '');
  const [serial, setSerial] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState<SerialNumberLookup | null>(null);
  const [expandedCardId, setExpandedCardId] = useState<string | null>(null);

  useEffect(() => {
    if (canChangeSite) {
      siteApi.getSites().then(setSites).catch(() => {});
    }
  }, [canChangeSite]);

  const handleLookup = useCallback(async () => {
    const trimmed = serial.trim();
    if (!trimmed) return;
    setLoading(true);
    setError('');
    setData(null);
    setExpandedCardId(null);
    try {
      const result = await serialNumberApi.getLookup(trimmed);
      setData(result);
    } catch {
      setError('Serial number not found.');
    } finally {
      setLoading(false);
    }
  }, [serial]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleLookup();
  };

  const toggleCard = (id: string) => {
    setExpandedCardId((prev) => (prev === id ? null : id));
  };

  const flowSteps = data ? flattenToFlow(data.treeNodes) : [];

  return (
    <AdminLayout title="Serial Number Lookup">
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
          onClick={handleLookup}
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
              <div className={styles.flowRow} data-testid="genealogy-flow">
                {flowSteps.map((step, i) => (
                  <div
                    key={step.node.id}
                    className={`${styles.flowColumn} ${i < flowSteps.length - 1 ? styles.flowColumnWithArrow : ''}`}
                  >
                    <HeroCard
                      node={step.node}
                      isExpanded={expandedCardId === step.node.id}
                      onToggle={() => toggleCard(step.node.id)}
                    />
                    {step.subComponents.length > 0 && (
                      <div className={styles.subComponentsArea}>
                        {step.subComponents.map((sub) => (
                          <div key={sub.id} className={styles.subItem}>
                            <div className={styles.subArrow}>
                              <div className={styles.subArrowLine} />
                            </div>
                            <HeroCard
                              node={sub}
                              isExpanded={expandedCardId === sub.id}
                              onToggle={() => toggleCard(sub.id)}
                              isSmall
                            />
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      )}
    </AdminLayout>
  );
}
