import { useState, useCallback, useMemo, useEffect } from 'react';
import { Button, Spinner, Checkbox } from '@fluentui/react-components';
import { spotXrayApi } from '../../api/endpoints';
import { useAuth } from '../../auth/AuthContext';
import type { SpotXrayLaneQueues, SpotXrayLane, SpotXrayQueueTank, SpotXrayIncrementSummary } from '../../types/domain';
import styles from './SpotXrayScreen.module.css';

const MAX_INCREMENT_SIZE: Record<number, number> = {
  120: 8, 250: 6, 320: 6, 500: 5, 1000: 4, 1450: 4, 1990: 4,
};
const LANE_REFRESH_INTERVAL_MS = 15_000;

interface Props {
  workCenterId: string;
  productionLineId: string;
  operatorId: string;
  onIncrementsCreated: (ids: SpotXrayIncrementSummary[]) => void;
  onOpenDraft: (draft: SpotXrayIncrementSummary) => void;
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message) {
    return error.message;
  }
  if (
    typeof error === 'object' &&
    error !== null &&
    'message' in error &&
    typeof (error as { message?: unknown }).message === 'string'
  ) {
    return (error as { message: string }).message;
  }
  return fallback;
}

export function SpotXrayCreateView({ workCenterId, productionLineId, operatorId, onIncrementsCreated, onOpenDraft }: Props) {
  const { user } = useAuth();
  const siteId = user?.defaultSiteId ?? '';
  const [mode, setMode] = useState<'create' | 'drafts'>('create');
  const [lanes, setLanes] = useState<SpotXrayLaneQueues | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selections, setSelections] = useState<Record<string, Set<number>>>({});
  const [creating, setCreating] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [drafts, setDrafts] = useState<SpotXrayIncrementSummary[]>([]);
  const [draftsLoading, setDraftsLoading] = useState(false);

  const fetchLanes = useCallback(async (options?: { showLoading?: boolean; resetSelections?: boolean }) => {
    const showLoading = options?.showLoading ?? true;
    const resetSelections = options?.resetSelections ?? false;
    if (!siteId) return;
    try {
      if (showLoading) {
        setLoading(true);
      }
      setError('');
      const data = await spotXrayApi.getLaneQueues(siteId);
      setLanes(data);
      setLastUpdated(new Date());
      setSelections(prev => {
        const next: Record<string, Set<number>> = {};
        for (const lane of data.lanes) {
          if (resetSelections) {
            next[lane.laneName] = new Set();
            continue;
          }

          const previousSelection = prev[lane.laneName] ?? new Set<number>();
          const validPositions = new Set(lane.tanks.map(t => t.position));
          next[lane.laneName] = new Set(
            Array.from(previousSelection).filter(position => validPositions.has(position)),
          );
        }
        return next;
      });
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'Failed to load lane queues'));
    } finally {
      if (showLoading) {
        setLoading(false);
      }
    }
  }, [siteId]);

  useEffect(() => {
    fetchLanes({ showLoading: true, resetSelections: true });
  }, [fetchLanes]);

  useEffect(() => {
    if (!siteId) return undefined;
    if (mode !== 'create') return undefined;
    const intervalId = window.setInterval(() => {
      void fetchLanes({ showLoading: false, resetSelections: false });
    }, LANE_REFRESH_INTERVAL_MS);

    return () => window.clearInterval(intervalId);
  }, [siteId, mode, fetchLanes]);

  const fetchDrafts = useCallback(async () => {
    if (!siteId) return;
    try {
      setDraftsLoading(true);
      setError('');
      const data = await spotXrayApi.getDraftIncrements(siteId);
      setDrafts(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'Failed to load drafts'));
    } finally {
      setDraftsLoading(false);
    }
  }, [siteId]);

  useEffect(() => {
    if (mode === 'drafts') {
      void fetchDrafts();
    }
  }, [mode, fetchDrafts]);

  const toggleTank = useCallback((laneName: string, position: number) => {
    setSelections(prev => {
      const current = new Set(prev[laneName] ?? []);
      if (current.has(position)) {
        current.delete(position);
      } else {
        current.add(position);
      }
      return { ...prev, [laneName]: current };
    });
  }, []);

  const validationErrors = useMemo(() => {
    if (!lanes) return {};
    const errors: Record<string, string> = {};
    for (const lane of lanes.lanes) {
      const sel = selections[lane.laneName];
      if (!sel || sel.size === 0) continue;
      const err = validateLaneSelection(lane, sel);
      if (err) errors[lane.laneName] = err;
    }
    return errors;
  }, [lanes, selections]);

  const anySelected = Object.values(selections).some(s => s.size > 0);
  const hasErrors = Object.keys(validationErrors).length > 0;

  const handleCreate = async () => {
    if (!anySelected || hasErrors) return;
    try {
      setCreating(true);
      setError('');
      const laneSelections = Object.entries(selections)
        .filter(([, s]) => s.size > 0)
        .map(([laneName, s]) => ({
          laneName,
          selectedPositions: Array.from(s).sort((a, b) => a - b),
        }));
      const result = await spotXrayApi.createIncrements({
        workCenterId,
        productionLineId,
        operatorId,
        siteId,
        laneSelections,
      });
      onIncrementsCreated(result.increments);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'Failed to create increments'));
    } finally {
      setCreating(false);
    }
  };

  if (loading) {
    return (
      <div className={styles.container} style={{ alignItems: 'center', justifyContent: 'center' }}>
        <Spinner size="large" label="Loading lane queues..." />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.createHeader}>
        <div className={styles.createHeaderTitle}>
          <h2>Spot Xray Increments</h2>
          <div className={styles.createToggleGroup}>
            <Button
              appearance={mode === 'create' ? 'primary' : 'secondary'}
              size="small"
              onClick={() => setMode('create')}
            >
              Create
            </Button>
            <Button
              appearance={mode === 'drafts' ? 'primary' : 'secondary'}
              size="small"
              onClick={() => setMode('drafts')}
            >
              Drafts
            </Button>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {mode === 'create' && (
            <span style={{ color: '#868686', fontSize: 12 }}>
              Auto-refresh every 15s{lastUpdated ? ` • Last updated ${lastUpdated.toLocaleTimeString()}` : ''}
            </span>
          )}
          {error && <span style={{ color: '#dc3545', fontSize: 13 }}>{error}</span>}
          {mode === 'create' && (
            <Button
              appearance="primary"
              size="large"
              disabled={!anySelected || hasErrors || creating}
              onClick={handleCreate}
            >
              {creating ? 'Creating...' : 'Create Increment'}
            </Button>
          )}
        </div>
      </div>
      {mode === 'create' ? (
        <div className={styles.lanesContainer}>
          {lanes?.lanes.map(lane => (
            <LaneColumn
              key={lane.laneName}
              lane={lane}
              selected={selections[lane.laneName] ?? new Set()}
              error={validationErrors[lane.laneName]}
              onToggle={(pos) => toggleTank(lane.laneName, pos)}
            />
          ))}
          {(!lanes || lanes.lanes.length === 0) && (
            <div style={{ padding: 20, color: '#868686' }}>No tanks available in any lane.</div>
          )}
        </div>
      ) : (
        <DraftList drafts={drafts} loading={draftsLoading} onOpenDraft={onOpenDraft} />
      )}
    </div>
  );
}

function DraftList({ drafts, loading, onOpenDraft }: {
  drafts: SpotXrayIncrementSummary[];
  loading: boolean;
  onOpenDraft: (draft: SpotXrayIncrementSummary) => void;
}) {
  if (loading) {
    return (
      <div className={styles.draftsContainer}>
        <Spinner size="large" label="Loading drafts..." />
      </div>
    );
  }

  if (drafts.length === 0) {
    return (
      <div className={styles.draftsContainer}>
        <div className={styles.draftsEmpty}>No drafts available.</div>
      </div>
    );
  }

  return (
    <div className={styles.draftsContainer}>
      <div className={styles.draftsTableHeader}>
        <span>Increment</span>
        <span>Lane</span>
        <span>Tank Size</span>
        <span>Status</span>
        <span />
      </div>
      {drafts.map(draft => (
        <div key={draft.id} className={styles.draftRow}>
          <span>{draft.incrementNo}</span>
          <span>{draft.laneNo}</span>
          <span>{draft.tankSize ? `${draft.tankSize} gal` : '-'}</span>
          <span>{draft.overallStatus}</span>
          <Button appearance="primary" size="small" onClick={() => onOpenDraft(draft)}>
            Open
          </Button>
        </div>
      ))}
    </div>
  );
}

function LaneColumn({ lane, selected, error, onToggle }: {
  lane: SpotXrayLane;
  selected: Set<number>;
  error?: string;
  onToggle: (pos: number) => void;
}) {
  const selectedCount = selected.size;
  const firstSelectedTank = lane.tanks.find(t => selected.has(t.position));
  const tankSize = firstSelectedTank?.tankSize;
  const maxSize = tankSize ? MAX_INCREMENT_SIZE[tankSize] ?? 8 : null;

  return (
    <div className={styles.laneColumn}>
      <div className={styles.laneHeader}>
        <span>{lane.laneName}</span>
        {lane.draftCount > 0 && (
          <span className={styles.draftBadge}>{lane.draftCount} draft</span>
        )}
      </div>
      <div className={styles.laneBody}>
        <table className={styles.laneTable}>
          <thead>
            <tr>
              <th />
              <th>Pos</th>
              <th>Tank</th>
              <th>Size</th>
              <th>Round Seam Date/Time</th>
              <th>Seam Welders</th>
            </tr>
          </thead>
          <tbody>
            {lane.tanks.map(tank => (
              <TankRow
                key={tank.position}
                tank={tank}
                isSelected={selected.has(tank.position)}
                onToggle={() => onToggle(tank.position)}
              />
            ))}
          </tbody>
        </table>
        {lane.tanks.length === 0 && (
          <div style={{ padding: 12, color: '#868686', fontSize: 13, textAlign: 'center' }}>
            No tanks available
          </div>
        )}
      </div>
      <div className={`${styles.selectionInfo} ${error ? styles.selectionError : ''}`}>
        {error
          ? error
          : selectedCount > 0
            ? `${selectedCount} selected${maxSize ? ` (max ${maxSize})` : ''}`
            : 'Select tanks for this lane'}
      </div>
    </div>
  );
}

function TankRow({ tank, isSelected, onToggle }: {
  tank: SpotXrayQueueTank;
  isSelected: boolean;
  onToggle: () => void;
}) {
  const showBreak = tank.sizeChanged || tank.welderChanged;
  const alphaWithShells = tank.shellSerials.length > 0
    ? `${tank.alphaCode} (${tank.shellSerials.join(', ')})`
    : tank.alphaCode;
  const roundSeamDateTime = tank.roundSeamWeldedAtUtc
    ? new Date(tank.roundSeamWeldedAtUtc).toLocaleString()
    : '-';
  const seamWelders = tank.seamWelders?.trim() || '-';
  return (
    <tr
      className={`${styles.tankRow} ${isSelected ? styles.tankRowSelected : ''} ${showBreak ? styles.tankRowBreak : ''}`}
      onClick={onToggle}
    >
      <td>
        <Checkbox
          checked={isSelected}
          onChange={(e) => { e.stopPropagation(); onToggle(); }}
        />
      </td>
      <td className={styles.tankPosition}>{tank.position}</td>
      <td>
        <div className={styles.tankInfo}>
          <span className={styles.tankAlpha}>{alphaWithShells}</span>
          <span className={styles.tankMeta}>{tank.weldType}</span>
        </div>
      </td>
      <td><span className={styles.sizeBadge}>{tank.tankSize}</span></td>
      <td className={styles.tankMeta}>{roundSeamDateTime}</td>
      <td className={styles.tankMeta}>{seamWelders}</td>
    </tr>
  );
}

function validateLaneSelection(lane: SpotXrayLane, selected: Set<number>): string | undefined {
  if (selected.size === 0) return undefined;

  const positions = Array.from(selected).sort((a, b) => a - b);

  // Sequential check
  for (let i = 1; i < positions.length; i++) {
    if (positions[i] !== positions[i - 1] + 1) {
      return 'Selections must be sequential';
    }
  }

  const tanks = lane.tanks.filter(t => selected.has(t.position));

  // Same tank size
  const firstSize = tanks[0].tankSize;
  if (tanks.some(t => t.tankSize !== firstSize)) {
    return 'All tanks must be the same size';
  }

  // Same welders
  const firstWelders = JSON.stringify(tanks[0].welderIds);
  if (tanks.some(t => JSON.stringify(t.welderIds) !== firstWelders)) {
    return 'All tanks must have the same welders';
  }

  // Max size
  const maxSize = MAX_INCREMENT_SIZE[firstSize] ?? 8;
  if (tanks.length > maxSize) {
    return `Max ${maxSize} tanks for ${firstSize} gal`;
  }

  return undefined;
}
