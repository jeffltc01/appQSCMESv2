import { useState, useCallback } from 'react';
import {
  Button,
  Input,
  Select,
  Radio,
  RadioGroup,
  Spinner,
} from '@fluentui/react-components';
import {
  ChevronRightRegular,
  ChevronDownRegular,
  SearchRegular,
} from '@fluentui/react-icons';
import { AdminLayout } from './AdminLayout.tsx';
import { useAuth } from '../../auth/AuthContext.tsx';
import { siteApi, serialNumberApi } from '../../api/endpoints.ts';
import type { Plant, TraceabilityNode, ManufacturingEvent, SerialNumberLookup } from '../../types/domain.ts';
import styles from './SerialNumberLookupScreen.module.css';
import { useEffect } from 'react';

export function SerialNumberLookupScreen() {
  const { user } = useAuth();
  const canChangeSite = (user?.roleTier ?? 99) <= 2;

  const [sites, setSites] = useState<Plant[]>([]);
  const [siteId, setSiteId] = useState(user?.defaultSiteId ?? '');
  const [serial, setSerial] = useState('');
  const [showDetails, setShowDetails] = useState<'show' | 'hide'>('show');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState<SerialNumberLookup | null>(null);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);

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
    setSelectedNodeId(null);
    try {
      const result = await serialNumberApi.getLookup(trimmed);
      setData(result);
      const allIds = new Set<string>();
      const collectIds = (nodes: TraceabilityNode[]) => {
        for (const n of nodes) {
          allIds.add(n.id);
          if (n.children) collectIds(n.children);
        }
      };
      collectIds(result.treeNodes);
      setExpanded(allIds);
    } catch {
      setError('Serial number not found.');
    } finally {
      setLoading(false);
    }
  }, [serial]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleLookup();
  };

  const toggleExpand = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const filteredEvents = useCallback((): ManufacturingEvent[] => {
    if (!data) return [];
    return data.events;
  }, [data]);

  const formatTimestamp = (iso: string) => {
    try {
      return new Date(iso).toLocaleString([], {
        month: '2-digit',
        day: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return iso;
    }
  };

  function TreeNode({ node }: { node: TraceabilityNode }) {
    const hasChildren = node.children && node.children.length > 0;
    const isExpanded = expanded.has(node.id);
    const isSelected = selectedNodeId === node.id;

    return (
      <div>
        <div
          className={`${styles.treeNode} ${isSelected ? styles.treeNodeSelected : ''}`}
          onClick={() => setSelectedNodeId(node.id)}
        >
          <span
            className={styles.treeChevron}
            onClick={(e) => {
              e.stopPropagation();
              if (hasChildren) toggleExpand(node.id);
            }}
          >
            {hasChildren ? (
              isExpanded ? <ChevronDownRegular /> : <ChevronRightRegular />
            ) : null}
          </span>
          <span>{node.label}</span>
        </div>
        {hasChildren && isExpanded && (
          <div className={styles.treeChildren}>
            {node.children!.map((child) => (
              <TreeNode key={child.id} node={child} />
            ))}
          </div>
        )}
      </div>
    );
  }

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
        >
          Go
        </Button>

        <div className={styles.detailsToggle}>
          <span className={styles.fieldLabel}>Details</span>
          <RadioGroup
            layout="horizontal"
            value={showDetails}
            onChange={(_e, d) => setShowDetails(d.value as 'show' | 'hide')}
          >
            <Radio value="show" label="Show" />
            <Radio value="hide" label="Hide" />
          </RadioGroup>
        </div>
      </div>

      {loading && (
        <div className={styles.emptyTree}>
          <Spinner size="medium" label="Looking up..." />
        </div>
      )}

      {error && <div className={styles.errorMsg}>{error}</div>}

      {data && (
        <>
          <div className={styles.treeContainer}>
            {data.treeNodes.length === 0 ? (
              <div className={styles.emptyTree}>No traceability data found.</div>
            ) : (
              data.treeNodes.map((node) => (
                <TreeNode key={node.id} node={node} />
              ))
            )}
          </div>

          {showDetails === 'show' && (
            <table className={styles.eventsTable}>
              <thead>
                <tr>
                  <th>Date/Time</th>
                  <th>Workcenter</th>
                  <th>Type</th>
                  <th>Completed By</th>
                  <th>Asset</th>
                  <th>Inspection Result</th>
                </tr>
              </thead>
              <tbody>
                {filteredEvents().length === 0 ? (
                  <tr>
                    <td colSpan={6} style={{ textAlign: 'center', color: '#868e96', fontStyle: 'italic' }}>
                      No manufacturing events.
                    </td>
                  </tr>
                ) : (
                  filteredEvents().map((evt, i) => (
                    <tr key={i}>
                      <td>{formatTimestamp(evt.timestamp)}</td>
                      <td>{evt.workCenterName}</td>
                      <td>{evt.type}</td>
                      <td>{evt.completedBy}</td>
                      <td>{evt.assetName ?? '—'}</td>
                      <td>{evt.inspectionResult ?? '—'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          )}
        </>
      )}
    </AdminLayout>
  );
}
