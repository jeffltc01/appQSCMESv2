import { useState, useCallback, useEffect, useMemo, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  Button,
  Dialog,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogActions,
  Input,
  Select,
  Spinner,
} from '@fluentui/react-components';
import {
  SearchRegular,
  CheckmarkCircleFilled,
  DismissCircleFilled,
  SubtractCircleFilled,
  FlagFilled,
  FlagRegular,
  DismissRegular,
} from '@fluentui/react-icons';
import ReactFlow, { Background, Controls, MiniMap } from 'reactflow';
import 'reactflow/dist/style.css';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, serialNumberApi } from '../../api/endpoints.ts';
import type { Plant, TraceabilityNode, ManufacturingEvent, SerialNumberLookup, LogAnnotationBadge } from '../../types/domain.ts';
import { formatDateTime, formatShortDateTime } from '../../utils/dateFormat.ts';
import { NODE_TYPE_COLORS } from './SerialLookupNodeCard.tsx';
import { serialLookupNodeTypes, toReactFlowGraph } from './SerialLookupGraph.tsx';
import styles from './SerialNumberLookupScreen.module.css';

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


function AnnotationFlags({
  badges,
  onViewDetail,
}: {
  badges: LogAnnotationBadge[];
  onViewDetail: (badge: LogAnnotationBadge) => void;
}) {
  return (
    <div className={styles.annotCell}>
      {badges.length > 0
        ? badges.map((b, i) => (
            <button
              key={i}
              className={styles.flagBtn}
              onClick={() => onViewDetail(b)}
              title={`${b.typeName} — click to view details`}
              type="button"
            >
              <FlagFilled
                fontSize={18}
                className={styles.flagActive}
                style={{ color: b.color }}
              />
            </button>
          ))
        : <FlagRegular fontSize={18} className={styles.flagInactive} />}
    </div>
  );
}

function EventsPanel({ events }: { events: EventWithSource[] }) {
  const [detailBadge, setDetailBadge] = useState<LogAnnotationBadge | null>(null);

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
              <AnnotationFlags badges={item.event.annotations ?? []} onViewDetail={setDetailBadge} />
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

      <Dialog open={detailBadge !== null} onOpenChange={(_, data) => { if (!data.open) setDetailBadge(null); }}>
        <DialogSurface className={styles.detailSurface}>
          <DialogBody>
            <DialogTitle
              action={
                <Button
                  appearance="subtle"
                  aria-label="close"
                  icon={<DismissRegular />}
                  onClick={() => setDetailBadge(null)}
                />
              }
            >
              Annotation Details
            </DialogTitle>
            <DialogContent>
              {detailBadge && (
                <div className={styles.detailGrid}>
                  <div className={styles.detailLabel}>Type</div>
                  <div className={styles.detailValue}>
                    <span className={styles.detailTypeDot} style={{ background: detailBadge.color }} />
                    {detailBadge.typeName}
                  </div>

                  <div className={styles.detailLabel}>Status</div>
                  <div className={styles.detailValue}>
                    <span className={detailBadge.status === 'Open' ? styles.statusOpen : styles.statusResolved}>
                      {detailBadge.status}
                    </span>
                  </div>

                  <div className={styles.detailLabel}>Created</div>
                  <div className={styles.detailValue}>{formatShortDateTime(detailBadge.createdAt)}</div>

                  <div className={styles.detailLabel}>Initiated By</div>
                  <div className={styles.detailValue}>{detailBadge.initiatedByName || '—'}</div>

                  {detailBadge.notes && (
                    <>
                      <div className={styles.detailLabel}>Notes</div>
                      <div className={styles.detailValue}>{detailBadge.notes}</div>
                    </>
                  )}

                  {detailBadge.resolvedByName && (
                    <>
                      <div className={styles.detailLabel}>Resolved By</div>
                      <div className={styles.detailValue}>{detailBadge.resolvedByName}</div>
                    </>
                  )}

                  {detailBadge.resolvedNotes && (
                    <>
                      <div className={styles.detailLabel}>Resolution Notes</div>
                      <div className={styles.detailValue}>{detailBadge.resolvedNotes}</div>
                    </>
                  )}
                </div>
              )}
            </DialogContent>
            <DialogActions>
              <Button appearance="primary" onClick={() => setDetailBadge(null)}>Close</Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
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

  const graph = useMemo(() => (data ? toReactFlowGraph(data, { includeLineageChildren: true }) : null), [data]);
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
          ) : !graph || graph.nodes.length === 0 ? (
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
                  <div className={styles.graphSurface} data-testid="genealogy-flow">
                    <ReactFlow
                      nodes={graph.nodes}
                      edges={graph.edges}
                      nodeTypes={serialLookupNodeTypes}
                      fitView
                      fitViewOptions={{ padding: 0.2 }}
                      proOptions={{ hideAttribution: true }}
                      minZoom={0.3}
                      maxZoom={1.6}
                      nodesDraggable={false}
                      nodesConnectable={false}
                    >
                      <Background gap={20} size={1} />
                      <MiniMap pannable zoomable />
                      <Controls showInteractive={false} />
                    </ReactFlow>
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
